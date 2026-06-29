using AutoMapper;
using DigiTrade.Common.Mapping;
using Trade.Application.Models;
using Trade.Domain.Trades;

namespace Trade.Api.Contracts;

public sealed record OpenTradeInput(
    string AccountId,
    string InstrumentId,
    TradeDirection Direction,
    decimal Quantity,
    decimal OpenPrice);

public sealed record CloseTradeInput(decimal ClosePrice);

public sealed record TradeDto(
    Guid TradeId,
    string AccountId,
    string InstrumentId,
    TradeDirection Direction,
    TradeStatus Status,
    decimal Quantity,
    decimal OpenPrice,
    DateTimeOffset OpenedAtUtc,
    decimal? ClosePrice,
    DateTimeOffset? ClosedAtUtc,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt) : IAutoMap<TradeDetailsModel, TradeDto>
    {
      public void CreateMap(Profile profile)
      {
          profile.CreateMap<TradeDetailsModel, TradeDto>();
      }
    };