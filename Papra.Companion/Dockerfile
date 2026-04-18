FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 1003
# Pre-create persistent directories so volume mounts are owned by the app user
RUN mkdir -p /app/data /app/data/keys /app/attachments

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Papra.Companion/Papra.Companion.csproj", "Papra.Companion/"]
COPY ["Papra.Companion.Data/Papra.Companion.Data.csproj", "Papra.Companion.Data/"]
RUN dotnet restore "./Papra.Companion/Papra.Companion.csproj"
COPY . .
WORKDIR "/src/Papra.Companion"
RUN dotnet build "./Papra.Companion.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Papra.Companion.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Papra.Companion.dll"]
