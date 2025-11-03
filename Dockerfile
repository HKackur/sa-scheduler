# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Disable telemetry and set environment to production
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    ASPNETCORE_ENVIRONMENT=Production

# Copy csproj and restore dependencies
COPY ["SchedulerMVP/SchedulerMVP.csproj", "SchedulerMVP/"]
RUN dotnet restore "SchedulerMVP/SchedulerMVP.csproj"

# Copy rest of the project and build
COPY . .
WORKDIR "/src/SchedulerMVP"
RUN dotnet publish "SchedulerMVP.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Environment configuration
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Expose Fly.io port
EXPOSE 8080

# Healthcheck for Fly.io
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "SchedulerMVP.dll"]
