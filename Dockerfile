FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore ResolveAi.Api/ResolveAi.Api.csproj
RUN dotnet publish ResolveAi.Api/ResolveAi.Api.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "ResolveAi.Api.dll"]
