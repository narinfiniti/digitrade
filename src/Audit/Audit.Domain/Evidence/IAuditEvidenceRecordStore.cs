namespace Audit.Domain.Evidence;

public interface IAuditEvidenceRecordStore
{
    Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<bool> AppendIfNotExistsAsync(AuditEvidenceRecord record, CancellationToken cancellationToken = default);
}
