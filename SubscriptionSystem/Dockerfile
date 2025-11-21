# -------------------------
# 1) Build stage
# -------------------------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy solution file
# COPY SubscriptionSystem.sln ./
COPY SubscriptionSystem ./
# Copy each project separately for caching
# COPY SubscriptionSystem.API/*.csproj SubscriptionSystem.API/
# COPY SubscriptionSystem.Application/*.csproj SubscriptionSystem.Application/
# COPY SubscriptionSystem.Infrastructure/*.csproj SubscriptionSystem.Infrastructure/
# COPY SubscriptionSystem.Domain/*.csproj SubscriptionSystem.Domain/
# COPY SubscriptionSystem.Shared/*.csproj SubscriptionSystem.Shared/

# Restore via solution
RUN dotnet restore SubscriptionSystem.sln

# Copy the rest of the source code
COPY . .

# Publish the API project
RUN dotnet publish SubscriptionSystem.API/SubscriptionSystem.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# -------------------------
# 2) Runtime stage
# -------------------------
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SubscriptionSystem.API.dll"]
