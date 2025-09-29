# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution and project files
COPY *.sln ./
COPY *.csproj ./
RUN dotnet restore

# Copy all source and publish
COPY . ./
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Make ASP.NET Core listen on port 80
ENV ASPNETCORE_URLS=http://+:80

COPY --from=build /app .
ENTRYPOINT ["dotnet", "MeniuMate_API.dll"]
