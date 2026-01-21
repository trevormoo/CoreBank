# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY CoreBank.sln .
COPY src/CoreBank.Domain/CoreBank.Domain.csproj src/CoreBank.Domain/
COPY src/CoreBank.Application/CoreBank.Application.csproj src/CoreBank.Application/
COPY src/CoreBank.Infrastructure/CoreBank.Infrastructure.csproj src/CoreBank.Infrastructure/
COPY src/CoreBank.Api/CoreBank.Api.csproj src/CoreBank.Api/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build the application
WORKDIR /src/src/CoreBank.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Copy published output
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "CoreBank.Api.dll"]
