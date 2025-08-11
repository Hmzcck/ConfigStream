# Use the official .NET 8 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/ConfigStream.Mvc.Web/ConfigStream.Mvc.Web.csproj", "ConfigStream.Mvc.Web/"]
COPY ["src/ConfigStream.Core/ConfigStream.Core.csproj", "ConfigStream.Core/"]
COPY ["src/ConfigStream.MongoDb/ConfigStream.MongoDb.csproj", "ConfigStream.MongoDb/"]

# Restore dependencies
RUN dotnet restore "ConfigStream.Mvc.Web/ConfigStream.Mvc.Web.csproj"

# Copy all source code
COPY src/ .

# Build the application
RUN dotnet build "ConfigStream.Mvc.Web/ConfigStream.Mvc.Web.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ConfigStream.Mvc.Web/ConfigStream.Mvc.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "ConfigStream.Mvc.Web.dll"]