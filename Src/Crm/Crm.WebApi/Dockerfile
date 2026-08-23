FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/crm/Crm.WebApi/Crm.WebApi.csproj", "src/crm/Crm.WebApi/"]
COPY ["src/crm/Crm.Application/Crm.Application.csproj", "src/crm/Crm.Application/"]
COPY ["src/crm/Crm.Domain/Crm.Domain.csproj", "src/crm/Crm.Domain/"]
COPY ["src/crm/Crm.Infrastructure/Crm.Infrastructure.csproj", "src/crm/Crm.Infrastructure/"]
COPY ["src/shared/SharedKernel/SharedKernel.csproj", "src/shared/SharedKernel/"]
COPY ["src/shared/SmartCore.Telemetry/SmartCore.Telemetry.csproj", "src/shared/SmartCore.Telemetry/"]
RUN dotnet restore "src/crm/Crm.WebApi/Crm.WebApi.csproj"
COPY . .
WORKDIR "/src/src/crm/Crm.WebApi"
RUN dotnet build "Crm.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Crm.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Crm.WebApi.dll"]
