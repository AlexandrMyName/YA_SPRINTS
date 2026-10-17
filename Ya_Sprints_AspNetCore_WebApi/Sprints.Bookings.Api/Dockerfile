# ============================================================
# Bookings API
# Build context: Ya_Sprints_AspNetCore_WebApi/src/
# Запуск: docker build -f Services/Bookings/Sprints.Bookings.Api/Dockerfile -t sprints-bookings-api .
# ============================================================

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Contracts (общий)
COPY ["Contracts/Sprints.Contracts/Sprints.Contracts.csproj", "Contracts/Sprints.Contracts/"]

# Bookings
COPY ["Services/Bookings/Sprints.Bookings.Domain/Sprints.Bookings.Domain.csproj",           "Services/Bookings/Sprints.Bookings.Domain/"]
COPY ["Services/Bookings/Sprints.Bookings.Application/Sprints.Bookings.Application.csproj", "Services/Bookings/Sprints.Bookings.Application/"]
COPY ["Services/Bookings/Sprints.Bookings.Infrastructure/Sprints.Bookings.Infrastructure.csproj", "Services/Bookings/Sprints.Bookings.Infrastructure/"]
COPY ["Services/Bookings/Sprints.Bookings.Api/Sprints.Bookings.Api.csproj",                 "Services/Bookings/Sprints.Bookings.Api/"]

RUN dotnet restore "Services/Bookings/Sprints.Bookings.Api/Sprints.Bookings.Api.csproj"

COPY . .
WORKDIR "/src/Services/Bookings/Sprints.Bookings.Api"
RUN dotnet build "Sprints.Bookings.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore

# ---------- Publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sprints.Bookings.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# ---------- Final ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sprints.Bookings.Api.dll"]