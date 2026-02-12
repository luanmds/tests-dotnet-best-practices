# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY PointsWallet.sln ./

# Copy project files
COPY src/PointsWallet.Api/PointsWallet.Api.csproj src/PointsWallet.Api/
COPY src/PointsWallet.Domain/PointsWallet.Domain.csproj src/PointsWallet.Domain/
COPY src/PointsWallet.Infrastructure/PointsWallet.Infrastructure.csproj src/PointsWallet.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY src/ src/

# Build the application
WORKDIR /src/src/PointsWallet.Api
RUN dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1000 appuser \
    && adduser --system --uid 1000 --gid 1000 appuser

# Copy published files
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "PointsWallet.Api.dll"]
