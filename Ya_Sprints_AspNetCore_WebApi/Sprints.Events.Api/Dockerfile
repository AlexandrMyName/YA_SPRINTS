FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Собственные библиотеки (у вас в корне)
COPY ["Concurrency_Lib/Concurrency_Lib.csproj",                                 "Concurrency_Lib/"]
COPY ["RefactoringNetCompile_Lib/RefactoringNetCompile_Lib.csproj",             "RefactoringNetCompile_Lib/"]
COPY ["dataBase_autoMigration_Lib/dataBase_autoMigration_Lib.csproj",           "dataBase_autoMigration_Lib/"]
COPY ["queryBuilder_Lib/queryBuilder_Lib.csproj",                               "queryBuilder_Lib/"]
COPY ["reflectionPropertyAccessor_Lib/reflectionPropertyAccessor_Lib.csproj",   "reflectionPropertyAccessor_Lib/"]

# Общие контракты
COPY ["Sprints.Contracts/Sprints.Contracts.csproj", "Sprints.Contracts/"]

# Events
COPY ["Sprints.Events.Domain/Sprints.Events.Domain.csproj",                 "Sprints.Events.Domain/"]
COPY ["Sprints.Events.Application/Sprints.Events.Application.csproj",       "Sprints.Events.Application/"]
COPY ["Sprints.Events.Infrastructure/Sprints.Events.Infrastructure.csproj", "Sprints.Events.Infrastructure/"]
COPY ["Sprints.Events.Api/Sprints.Events.Api.csproj",                       "Sprints.Events.Api/"]

RUN dotnet restore "Sprints.Events.Api/Sprints.Events.Api.csproj"

COPY . .
WORKDIR "/src/Sprints.Events.Api"
RUN dotnet build "Sprints.Events.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sprints.Events.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sprints.Events.Api.dll"]