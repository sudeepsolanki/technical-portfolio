FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/ProductCatalog.API/ProductCatalog.API.csproj", "src/ProductCatalog.API/"]
COPY ["src/ProductCatalog.Application/ProductCatalog.Application.csproj", "src/ProductCatalog.Application/"]
COPY ["src/ProductCatalog.Domain/ProductCatalog.Domain.csproj", "src/ProductCatalog.Domain/"]
COPY ["src/ProductCatalog.Infrastructure/ProductCatalog.Infrastructure.csproj", "src/ProductCatalog.Infrastructure/"]
RUN dotnet restore "src/ProductCatalog.API/ProductCatalog.API.csproj"

COPY . .
WORKDIR "/src/src/ProductCatalog.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "ProductCatalog.API.dll"]
