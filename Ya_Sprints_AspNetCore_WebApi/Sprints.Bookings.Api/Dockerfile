# ============================================================
# Bookings API
# Build context: корень решения (где docker-compose.yml)
# Запуск: docker build -f Sprints.Bookings.Api/Dockerfile -t sprints-bookings-api .
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

# Bookings
COPY ["Sprints.Bookings.Domain/Sprints.Bookings.Domain.csproj",                 "Sprints.Bookings.Domain/"]
COPY ["Sprints.Bookings.Application/Sprints.Bookings.Application.csproj",       "Sprints.Bookings.Application/"]
COPY ["Sprints.Bookings.Infrastructure/Sprints.Bookings.Infrastructure.csproj", "Sprints.Bookings.Infrastructure/"]
COPY ["Sprints.Bookings.Api/Sprints.Bookings.Api.csproj",                       "Sprints.Bookings.Api/"]

RUN dotnet restore "Sprints.Bookings.Api/Sprints.Bookings.Api.csproj"

COPY . .
WORKDIR "/src/Sprints.Bookings.Api"
RUN dotnet build "Sprints.Bookings.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ---------- Publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sprints.Bookings.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# ---------- Final ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sprints.Bookings.Api.dll"]