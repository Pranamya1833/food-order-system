# Stage 1: Build & Publish using the .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker layer caching for NuGet restore
COPY FoodOrderSystem.slnx ./
COPY FoodOrder.Domain/FoodOrder.Domain.csproj FoodOrder.Domain/
COPY FoodOrder.Services/FoodOrder.Services.csproj FoodOrder.Services/
COPY FoodOrder.Data/FoodOrder.Data.csproj FoodOrder.Data/
COPY FoodOrder.Api/FoodOrder.Api.csproj FoodOrder.Api/

# Restore dependencies
RUN dotnet restore FoodOrder.Api/FoodOrder.Api.csproj

# Copy remaining source code and publish the API in Release mode
COPY FoodOrder.Domain/ FoodOrder.Domain/
COPY FoodOrder.Services/ FoodOrder.Services/
COPY FoodOrder.Data/ FoodOrder.Data/
COPY FoodOrder.Api/ FoodOrder.Api/

WORKDIR /src/FoodOrder.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Minimal ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose standard ASP.NET Core port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FoodOrder.Api.dll"]