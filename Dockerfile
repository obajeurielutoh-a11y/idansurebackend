# Use the official .NET 8 SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SubscriptionSystem/SubscriptionSystem.sln", "SubscriptionSystem/"]
COPY ["SubscriptionSystem/SubscriptionSystem.csproj", "SubscriptionSystem/"]
COPY ["SubscriptionSystem.API/SubscriptionSystem.API.csproj", "SubscriptionSystem.API/"]
COPY ["SubscriptionSystem.Application/SubscriptionSystem.Application.csproj", "SubscriptionSystem.Application/"]
COPY ["SubscriptionSystem.Domain/SubscriptionSystem.Domain.csproj", "SubscriptionSystem.Domain/"]
COPY ["SubscriptionSystem.Infrastructure/SubscriptionSystem.Infrastructure.csproj", "SubscriptionSystem.Infrastructure/"]
COPY ["SubscriptionSystem.Shared/SubscriptionSystem.Shared.csproj", "SubscriptionSystem.Shared/"]
COPY ["SubscriptionSystem.Tests/SubscriptionSystem.Tests.csproj", "SubscriptionSystem.Tests/"]

RUN dotnet restore "SubscriptionSystem/SubscriptionSystem.sln"

COPY . .

RUN dotnet publish "SubscriptionSystem/SubscriptionSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 and set the entry point
EXPOSE 8080
ENTRYPOINT ["dotnet", "SubscriptionSystem.dll"]
