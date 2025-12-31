# ================================
# BUILD
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia tudo
COPY . .

# Entra na pasta da API
WORKDIR /src/CasaResolve.Api

# Restaura e publica
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# ================================
# RUNTIME
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out .

# 🚫 NÃO definir ASPNETCORE_URLS
# 🚫 NÃO definir PORT
# Railway injeta tudo automaticamente

ENTRYPOINT ["dotnet", "CasaResolve.Api.dll"]

