# ============================================================
# Users API
# Build context: корень решения (где docker-compose.yml)
# Запуск: docker build -f Sprints.Users.Api/Dockerfile -t sprints-users-api .
# ============================================================

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Собственные библиотеки (в корне решения)
COPY ["Concurrency_Lib/Concurrency_Lib.csproj",                                 "Concurrency_Lib/"]
COPY ["RefactoringNetCompile_Lib/RefactoringNetCompile_Lib.csproj",             "RefactoringNetCompile_Lib/"]
COPY ["dataBase_autoMigration_Lib/dataBase_autoMigration_Lib.csproj",           "dataBase_autoMigration_Lib/"]
COPY ["queryBuilder_Lib/queryBuilder_Lib.csproj",                               "queryBuilder_Lib/"]
COPY ["reflectionPropertyAccessor_Lib/reflectionPropertyAccessor_Lib.csproj",   "reflectionPropertyAccessor_Lib/"]

# Общий контракт
COPY ["Sprints.Contracts/Sprints.Contracts.csproj", "Sprints.Contracts/"]

# Users
COPY ["Sprints.Users.Domain/Sprints.Users.Domain.csproj",                 "Sprints.Users.Domain/"]
COPY ["Sprints.Users.Application/Sprints.Users.Application.csproj",       "Sprints.Users.Application/"]
COPY ["Sprints.Users.Infrastructure/Sprints.Users.Infrastructure.csproj", "Sprints.Users.Infrastructure/"]
COPY ["Sprints.Users.Api/Sprints.Users.Api.csproj",                       "Sprints.Users.Api/"]

RUN dotnet restore "Sprints.Users.Api/Sprints.Users.Api.csproj"

COPY . .
WORKDIR "/src/Sprints.Users.Api"
RUN dotnet build "Sprints.Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ---------- Publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sprints.Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# ---------- Final ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sprints.Users.Api.dll"]