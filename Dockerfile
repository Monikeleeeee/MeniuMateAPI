# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy solution and project files
COPY *.sln .  
COPY *.csproj .  
RUN dotnet restore

# Copy all source code and publish
COPY . .  
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .  
ENTRYPOINT ["dotnet", "MeniuMate_API.dll"]
