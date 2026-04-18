FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# APP_UID=1654 is the non-root user pre-created in the Microsoft .NET base image.
# Declaring it here makes the value explicit and keeps the Dockerfile self-contained.
ARG APP_UID=1654
# Create directories as root and transfer ownership before dropping privileges.
# The chown ensures the app user can write into bind-mounted volumes at runtime.
RUN mkdir -p /app/data /app/data/keys /app/attachments \
    && chown -R ${APP_UID}:${APP_UID} /app
USER ${APP_UID}
WORKDIR /app
EXPOSE 1003

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
