# ============================================================
# Users API
# Build context: Ya_Sprints_AspNetCore_WebApi/src/
# Запуск: docker build -f Services/Users/Sprints.Users.Api/Dockerfile -t sprints-users-api .
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

# Users
COPY ["Services/Users/Sprints.Users.Domain/Sprints.Users.Domain.csproj",           "Services/Users/Sprints.Users.Domain/"]
COPY ["Services/Users/Sprints.Users.Application/Sprints.Users.Application.csproj", "Services/Users/Sprints.Users.Application/"]
COPY ["Services/Users/Sprints.Users.Infrastructure/Sprints.Users.Infrastructure.csproj", "Services/Users/Sprints.Users.Infrastructure/"]
COPY ["Services/Users/Sprints.Users.Api/Sprints.Users.Api.csproj",                 "Services/Users/Sprints.Users.Api/"]

RUN dotnet restore "Services/Users/Sprints.Users.Api/Sprints.Users.Api.csproj"

COPY . .
WORKDIR "/src/Services/Users/Sprints.Users.Api"
RUN dotnet build "Sprints.Users.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore

# ---------- Publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sprints.Users.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# ---------- Final ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sprints.Users.Api.dll"]