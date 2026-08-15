FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/credit_system/CreditSystem.Api/CreditSystem.Api.csproj", "src/credit_system/CreditSystem.Api/"]
RUN dotnet restore "src/credit_system/CreditSystem.Api/CreditSystem.Api.csproj"
COPY . .
WORKDIR "/src/src/credit_system/CreditSystem.Api"
RUN dotnet build "CreditSystem.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CreditSystem.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CreditSystem.Api.dll"]
