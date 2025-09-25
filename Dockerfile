# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY *.sln .
COPY MeniuMate_API/*.csproj ./MeniuMate_API/
RUN dotnet restore

COPY MeniuMate_API/. ./MeniuMate_API/
WORKDIR /source/MeniuMate_API
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MeniuMate_API.dll"]
