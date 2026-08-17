using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Propyka.Api.Data;
using Propyka.Api.Authorization;
using Propyka.Api.Identity;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<PropykaIdentityDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PropykaDatabase")));

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager<PropykaSignInManager>()
    .AddEntityFrameworkStores<PropykaIdentityDbContext>();

builder.Services.AddScoped<IAuthorizationHandler, ActiveUserHandler>();

builder.Services.AddAuthorization(options =>
{
    var activeUser = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new ActiveUserRequirement())
        .Build();

    options.AddPolicy("ActiveUser", activeUser);

    // Every plain [Authorize] now also requires a non-deleted account.
    options.DefaultPolicy = activeUser;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("PropykaFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("PropykaFrontend");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapIdentityApi<ApplicationUser>();

app.Run();
