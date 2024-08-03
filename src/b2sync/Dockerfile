FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/b2sync/b2sync.csproj", "src/b2sync/"]
COPY ["src/Client/Backblaze.Client.csproj", "src/Client/"]
RUN dotnet restore "src/b2sync/b2sync.csproj"
COPY . .
WORKDIR "/src/src/b2sync"
RUN rm .env
RUN dotnet build "b2sync.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "b2sync.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "b2sync.dll"]
