FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RagApp.Domain/RagApp.Domain.csproj RagApp.Domain/
COPY RagApp.Application/RagApp.Application.csproj RagApp.Application/
COPY RagApp.Infrastructure/RagApp.Infrastructure.csproj RagApp.Infrastructure/
COPY RagApp.WebApi/RagApp.WebApi.csproj RagApp.WebApi/

RUN dotnet restore RagApp.WebApi/RagApp.WebApi.csproj

COPY . .
RUN dotnet publish RagApp.WebApi/RagApp.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RagApp.WebApi.dll"]