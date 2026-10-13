using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Propyka.Api.Common;
using Propyka.Api.Common.Storage;
using Propyka.Api.Modules.Identity;
using Propyka.Api.Modules.Identity.Domain;
using Propyka.Api.Modules.Listings;
using Propyka.Api.Persistence;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddDbContext<PropykaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PropykaDatabase")));

// Each module registers its own services. Adding a business area is one line
// here plus one new folder — no other change to this file.
builder.Services.AddIdentityModule();
builder.Services.AddListingsModule();

builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));

// Singleton is safe: LocalFileStorage holds only configuration and a path.
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Behind a hosting platform's proxy (Azure App Service, Render, Railway) the app
// receives plain HTTP from the proxy's address. Without these headers the HTTPS
// redirect loops, and every visitor shares one rate-limit bucket.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // The default allow-list only trusts loopback, which would ignore a remote
    // platform proxy. Only enable Proxy:TrustForwardedHeaders when nothing can
    // reach the app except through that proxy, or clients can spoof their IP.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PropykaDbContext>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("PropykaFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Anonymous writes — registration and enquiries. Partitioned by IP because
    // there is no user to partition by.
    options.AddPolicy(RateLimitPolicies.PublicWrite, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Authenticated uploads, partitioned by user so one person filling their
    // disk quota cannot slow anybody else down.
    options.AddPolicy(RateLimitPolicies.Upload, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Bearer",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your access token."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Must be first. Later steps (HTTPS redirect, rate limiting, the client address
// in logs) need the scheme and IP the client actually used, not the proxy's.
if (app.Configuration.GetValue<bool>("Proxy:TrustForwardedHeaders"))
{
    app.UseForwardedHeaders();
}

// ─── Pipeline ─────────────────────────────────────────────────────────────────
// Order matters here more than anywhere else in the application. Each entry is
// placed where it is for a reason:

// First, so it can catch exceptions thrown by everything below it.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Before CORS and auth: uploaded images are public files, and making every
// image request run the auth stack is pointless work.
app.UseStaticFiles();

// Must run before authentication so that a rejected pre-flight still carries
// the CORS headers the browser needs to read the error.
app.UseCors("PropykaFrontend");

// Before authorization: rate limiting a request is cheaper than authorizing it.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapIdentityApi<ApplicationUser>();
app.MapHealthChecks("/health");

// Brings the schema up to date before the first request is served. Idempotent,
// so running it on every start is safe. Set Database:ApplyMigrationsOnStartup to
// false when several instances share a database and migrations run as their own step.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<PropykaDbContext>();
    await db.Database.MigrateAsync();
}

// Seeding runs after Build() but before the first request is served.
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();