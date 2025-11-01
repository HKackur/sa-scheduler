# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Kopiera csproj och återställ
COPY ["SchedulerMVP/SchedulerMVP.csproj", "SchedulerMVP/"]
RUN dotnet restore "SchedulerMVP/SchedulerMVP.csproj"

# Kopiera resten av projektet och bygg
COPY . .
WORKDIR "/src/SchedulerMVP"
RUN dotnet publish "SchedulerMVP.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponera port 8080
EXPOSE 8080

# Fly.io kommer förvänta sig att appen lyssnar på 0.0.0.0:8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "SchedulerMVP.dll"]
