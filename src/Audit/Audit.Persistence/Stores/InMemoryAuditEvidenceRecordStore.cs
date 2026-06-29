using System.Collections.Concurrent;
using Audit.Domain.Evidence;

namespace Audit.Persistence.Stores;

public sealed class InMemoryAuditEvidenceRecordStore : IAuditEvidenceRecordStore
{
    private readonly ConcurrentDictionary<Guid, AuditEvidenceRecord> evidenceById = new();
    private readonly ConcurrentDictionary<Guid, Guid> recordIdsByEventId = new();

    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(recordIdsByEventId.ContainsKey(eventId));
    }

    public Task<bool> AppendIfNotExistsAsync(AuditEvidenceRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var added = recordIdsByEventId.TryAdd(record.EventId, record.Id);
        if (!added)
        {
            return Task.FromResult(false);
        }

        evidenceById[record.Id] = record;
        return Task.FromResult(true);
    }
}
