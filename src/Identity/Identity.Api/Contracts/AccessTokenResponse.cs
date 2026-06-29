using AutoMapper;
using DigiTrade.Common.Mapping;
using DigiTrade.Security.Contracts;

namespace Identity.Api.Contracts;

public sealed record AccessTokenDto(string AccessToken, DateTimeOffset ExpiresAtUtc, string TokenType)
  : IAutoMap<IssuedAccessToken, AccessTokenDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<IssuedAccessToken, AccessTokenDto>();
    }
}