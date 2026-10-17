# ============================================================
# Events API
# Build context: Ya_Sprints_AspNetCore_WebApi/src/
# Запуск: docker build -f Services/Events/Sprints.Events.Api/Dockerfile -t sprints-events-api .
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

# Contracts
COPY ["Contracts/Sprints.Contracts/Sprints.Contracts.csproj", "Contracts/Sprints.Contracts/"]

# Events
COPY ["Services/Events/Sprints.Events.Domain/Sprints.Events.Domain.csproj",           "Services/Events/Sprints.Events.Domain/"]
COPY ["Services/Events/Sprints.Events.Application/Sprints.Events.Application.csproj", "Services/Events/Sprints.Events.Application/"]
COPY ["Services/Events/Sprints.Events.Infrastructure/Sprints.Events.Infrastructure.csproj", "Services/Events/Sprints.Events.Infrastructure/"]
COPY ["Services/Events/Sprints.Events.Api/Sprints.Events.Api.csproj",                 "Services/Events/Sprints.Events.Api/"]

RUN dotnet restore "Services/Events/Sprints.Events.Api/Sprints.Events.Api.csproj"

COPY . .
WORKDIR "/src/Services/Events/Sprints.Events.Api"
RUN dotnet build "Sprints.Events.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore

# ---------- Publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sprints.Events.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# ---------- Final ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sprints.Events.Api.dll"]