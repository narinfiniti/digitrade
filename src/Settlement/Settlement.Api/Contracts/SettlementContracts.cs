using AutoMapper;
using DigiTrade.Common.Mapping;
using Settlement.Application.Models;
using Settlement.Domain.Settlements;

namespace Settlement.Api.Contracts;

public sealed record InitiateSettlementInput(
    Guid TradeId,
    string AccountId,
    string CurrencyCode,
    decimal NetAmount);

public sealed record FailSettlementInput(string FailureReason);

public sealed record SettlementDto(
    Guid SettlementId,
    Guid TradeId,
    string AccountId,
    string CurrencyCode,
    decimal NetAmount,
    SettlementStatus Status,
    DateTimeOffset InitiatedAtUtc,
    DateTimeOffset? FinalizedAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt) : IAutoMap<SettlementDetailsModel, SettlementDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<SettlementDetailsModel, SettlementDto>();
    }
}