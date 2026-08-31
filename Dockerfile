# syntax=docker/dockerfile:1

# ---- Build stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy just the project files first so `dotnet restore` is cached as its own layer --
# it only re-runs when a .csproj actually changes, not on every source edit.
COPY RealEstate.Api/RealEstate.Api.csproj RealEstate.Api/
COPY RealEstate.Application/RealEstate.Application.csproj RealEstate.Application/
COPY RealEstate.Core/RealEstate.Core.csproj RealEstate.Core/
COPY RealEstate.Infrastructure/RealEstate.Infrastructure.csproj RealEstate.Infrastructure/
RUN dotnet restore RealEstate.Api/RealEstate.Api.csproj

COPY RealEstate.Api/ RealEstate.Api/
COPY RealEstate.Application/ RealEstate.Application/
COPY RealEstate.Core/ RealEstate.Core/
COPY RealEstate.Infrastructure/ RealEstate.Infrastructure/

RUN dotnet publish RealEstate.Api/RealEstate.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:UseAppHost=false

# ---- Runtime stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is needed for the HEALTHCHECK below -- the aspnet base image doesn't ship it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RealEstate.Api.dll"]
