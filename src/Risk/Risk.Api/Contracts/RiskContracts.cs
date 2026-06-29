using AutoMapper;
using DigiTrade.Common.Mapping;
using Risk.Application.Models;

namespace Risk.Api.Contracts;

public sealed record OpenMarginAccountInput(
    string AccountId,
    string CurrencyCode,
    decimal TotalMargin);

public sealed record AdjustMarginInput(decimal Amount);

public sealed record MarginAccountDto(
    Guid MarginAccountId,
    string AccountId,
    string CurrencyCode,
    decimal TotalMargin,
    decimal ReservedMargin,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt) : IAutoMap<MarginAccountDetailsModel, MarginAccountDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<MarginAccountDetailsModel, MarginAccountDto>();
    }
}