# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy entire repo (respects .dockerignore)
COPY . .

# Restore and publish
RUN dotnet restore SchedulerMVP/SchedulerMVP.csproj \
    && dotnet publish SchedulerMVP/SchedulerMVP.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# App ports
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# Copy published output
COPY --from=build /app/publish .

# Default connection string comes from environment
# ConnectionStrings__DefaultConnection is read by Program.cs
ENTRYPOINT ["dotnet", "SchedulerMVP.dll"]
