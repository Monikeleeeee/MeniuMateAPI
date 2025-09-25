# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /App

# Copy everything
COPY MeniuMateAPI/. ./

# Restore as distinct layers
RUN dotnet restore

# Build and publish a release
RUN dotnet publish -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /App

# Copy published output from build stage
COPY --from=build-env /App/out ./

# Set globalization environment
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

# Install ICU for globalization support (Debian-based)
RUN apt-get update && apt-get install -y icu-devtools && rm -rf /var/lib/apt/lists/*

# Set entrypoint
ENTRYPOINT ["dotnet", "MeniuMateAPI.dll"]
