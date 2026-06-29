namespace DigiTrade.SharedKernel.Abstractions;

public interface IEntity<out TId>
    where TId : notnull
{
    TId Id { get; }
}