ARG DOTNET_SDK_TAG=9.0
ARG DOTNET_ASPNET_TAG=9.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_TAG} AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Order/Order.Api/Order.Api.csproj
RUN dotnet publish src/Order/Order.Api/Order.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_ASPNET_TAG}
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "Order.Api.dll"]
