namespace Propyka.Api.Common;

/// <summary>
/// A business rule was broken — "you cannot publish a listing with no images".
/// The global handler turns this into a 400 with the message shown to the user,
/// so services can enforce rules without knowing anything about HTTP.
///
/// Only throw this with messages that are safe to show a stranger. Anything
/// else should be a plain exception, which becomes a generic 500.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

/// <summary>
/// The thing asked for does not exist, or the caller is not allowed to see that
/// it exists. Deliberately the same exception for both: telling a stranger
/// "this exists but is not yours" leaks information.
/// </summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message = "The requested resource was not found.")
        : base(message)
    {
    }
}
