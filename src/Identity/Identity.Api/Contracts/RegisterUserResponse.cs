using AutoMapper;
using DigiTrade.Common.Mapping;
using Identity.Application.Models;

namespace Identity.Api.Contracts;

public sealed record RegisterUserDto(Guid UserId, string UserName, string Email, DateTimeOffset CreatedAtUtc)
 : IAutoMap<RegisteredUserResultModel, RegisterUserDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<RegisteredUserResultModel, RegisterUserDto>();
    }
}