# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY PointsWallet.sln ./

# Copy project files
COPY src/PointsWallet.Worker/PointsWallet.Worker.csproj src/PointsWallet.Worker/
COPY src/PointsWallet.Contracts/PointsWallet.Contracts.csproj src/PointsWallet.Contracts/
COPY src/PointsWallet.Domain/PointsWallet.Domain.csproj src/PointsWallet.Domain/
COPY src/PointsWallet.Infrastructure/PointsWallet.Infrastructure.csproj src/PointsWallet.Infrastructure/

# Restore dependencies
RUN dotnet restore src/PointsWallet.Worker/PointsWallet.Worker.csproj

# Copy all source files
COPY src/ src/

# Build the application
WORKDIR /src/src/PointsWallet.Worker
RUN dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
WORKDIR /src
RUN dotnet publish src/PointsWallet.Worker/PointsWallet.Worker.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV DOTNET_ENVIRONMENT=Development

# Create non-root user
RUN addgroup --system --gid 1000 appuser \
    && adduser --system --uid 1000 --gid 1000 appuser

# Copy published files
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

USER appuser

ENTRYPOINT ["dotnet", "PointsWallet.Worker.dll"]
