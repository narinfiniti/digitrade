namespace DigiTrade.SharedKernel.Abstractions;

public interface ITrackTimestamps
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset UpdatedAt { get; }
}