namespace DigiTrade.SharedKernel.Abstractions;

public interface IVersionedEntity
{
    int Version { get; }
}