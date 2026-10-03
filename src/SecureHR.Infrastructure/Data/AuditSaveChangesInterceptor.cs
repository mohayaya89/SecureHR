using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using SecureHR.Application.Interfaces.Services;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Enums;
using System.Text.Json;

namespace SecureHR.Infrastructure.Data
{
    /// <summary>
    /// Writes an <see cref="AuditLog"/> row for every create, update and delete of an audited
    /// entity. The audit rows are saved in the same transaction as the change, so a change is
    /// never committed without its audit record.
    ///
    /// Generated keys are only known after the first save, so the flow is:
    ///   SavingChanges - capture the changes and begin a transaction (if none is active)
    ///   SavedChanges  - add audit rows with the real keys, save them, commit
    ///   SaveChangesFailed - roll back
    ///
    /// Registered as Scoped (one instance per DbContext / request).
    /// </summary>
    public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
    {
        private const int MaxChangesLength = 4000;
        private const string SystemUser = "system";

        private static readonly HashSet<Type> AuditedTypes =
            [typeof(Employee), typeof(Department), typeof(PayrollRecord), typeof(User)];

        // Values of these properties are never written to the audit log; only the fact they changed.
        private static readonly HashSet<string> SensitiveProperties =
            [nameof(User.PasswordHash)];

        private List<PendingAudit>? _pending;
        private IDbContextTransaction? _ownedTransaction;
        private bool _writingAuditRows;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (!_writingAuditRows && eventData.Context is { } context)
            {
                _pending = Capture(context);
                if (_pending.Count > 0 && context.Database.CurrentTransaction is null)
                    _ownedTransaction = await context.Database.BeginTransactionAsync(ct);
            }

            return await base.SavingChangesAsync(eventData, result, ct);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
        {
            if (_writingAuditRows || eventData.Context is not { } context)
                return await base.SavedChangesAsync(eventData, result, ct);

            try
            {
                if (_pending is { Count: > 0 })
                {
                    context.Set<AuditLog>().AddRange(_pending.Select(p => p.ToAuditLog()));
                    _writingAuditRows = true;
                    await context.SaveChangesAsync(ct);
                }

                if (_ownedTransaction is not null)
                    await _ownedTransaction.CommitAsync(ct);
            }
            catch
            {
                if (_ownedTransaction is not null)
                    await _ownedTransaction.RollbackAsync(CancellationToken.None);
                throw;
            }
            finally
            {
                await ResetAsync();
            }

            return await base.SavedChangesAsync(eventData, result, ct);
        }

        public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
        {
            if (!_writingAuditRows)
            {
                if (_ownedTransaction is not null)
                    await _ownedTransaction.RollbackAsync(CancellationToken.None);
                await ResetAsync();
            }

            await base.SaveChangesFailedAsync(eventData, ct);
        }

        // The application only uses the async API; fail loudly rather than skip auditing.
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is { } context && Capture(context).Count > 0)
                throw new InvalidOperationException("Audited entities must be saved with SaveChangesAsync.");

            return base.SavingChanges(eventData, result);
        }

        private async Task ResetAsync()
        {
            _pending = null;
            _writingAuditRows = false;
            if (_ownedTransaction is not null)
            {
                await _ownedTransaction.DisposeAsync();
                _ownedTransaction = null;
            }
        }

        private List<PendingAudit> Capture(DbContext context)
        {
            var pending = new List<PendingAudit>();
            var timestamp = DateTime.UtcNow;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (!AuditedTypes.Contains(entry.Entity.GetType()))
                    continue;

                var action = entry.State switch
                {
                    EntityState.Added => AuditAction.Create,
                    EntityState.Modified when IsDeactivation(entry) => AuditAction.Delete,
                    EntityState.Modified => AuditAction.Update,
                    EntityState.Deleted => AuditAction.Delete,
                    _ => (AuditAction?)null
                };

                if (action is null)
                    continue;

                var changes = DescribeChanges(entry);
                if (entry.State == EntityState.Modified && changes.Count == 0)
                    continue;

                pending.Add(new PendingAudit(
                    entry,
                    action.Value,
                    Serialize(changes),
                    currentUser.UserId ?? SystemUser,
                    currentUser.Username ?? SystemUser,
                    timestamp));
            }

            return pending;
        }

        // Soft delete: IsActive switched from true to false
        private static bool IsDeactivation(EntityEntry entry)
        {
            var isActive = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsActive");
            return isActive is { IsModified: true, OriginalValue: true, CurrentValue: false };
        }

        private static Dictionary<string, object?> DescribeChanges(EntityEntry entry)
        {
            var changes = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                    continue;

                var name = property.Metadata.Name;
                var sensitive = SensitiveProperties.Contains(name);

                switch (entry.State)
                {
                    case EntityState.Added:
                        changes[name] = sensitive ? Redact(property.CurrentValue) : property.CurrentValue;
                        break;

                    case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                        changes[name] = sensitive
                            ? "(changed)"
                            : new { from = property.OriginalValue, to = property.CurrentValue };
                        break;
                }
            }

            return changes;
        }

        private static string? Redact(object? value) =>
            value is string s && s.Length == 0 ? "" : "(set)";

        private static string? Serialize(Dictionary<string, object?> changes)
        {
            if (changes.Count == 0)
                return null;

            var json = JsonSerializer.Serialize(changes);
            return json.Length <= MaxChangesLength ? json : json[..(MaxChangesLength - 3)] + "...";
        }

        private sealed record PendingAudit(
            EntityEntry Entry,
            AuditAction Action,
            string? ChangesJson,
            string UserId,
            string Username,
            DateTime Timestamp)
        {
            public AuditLog ToAuditLog() => new()
            {
                EntityName = Entry.Metadata.ClrType.Name,
                // Read after the save so generated keys are populated
                EntityId = Entry.Property("Id").CurrentValue is int id ? id : 0,
                Action = Action,
                ChangesJson = ChangesJson,
                UserId = UserId,
                Username = Username,
                Timestamp = Timestamp
            };
        }
    }
}
