namespace SecureHR.Application.Interfaces.Services
{
    /// <summary>
    /// The user making the current request. Both values are null outside a request
    /// (e.g. startup seeding), which the audit trail records as "system".
    /// </summary>
    public interface ICurrentUser
    {
        string? UserId { get; }
        string? Username { get; }
    }
}
