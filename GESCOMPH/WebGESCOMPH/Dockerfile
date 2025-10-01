# Etapa base (con Playwright)
FROM mcr.microsoft.com/playwright/dotnet:v1.54.0-jammy AS base
WORKDIR /app
EXPOSE 8080

# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar proyectos para aprovechar cache de restore
COPY Business/Business.csproj Business/
COPY Entity/Entity.csproj Entity/
COPY Data/Data.csproj Data/
COPY Utilities/Utilities.csproj Utilities/
COPY Templates/Templates.csproj Templates/
COPY WebGESCOMPH/WebGESCOMPH.csproj WebGESCOMPH/
COPY GESCOMPH.sln .

RUN dotnet restore "WebGESCOMPH/WebGESCOMPH.csproj"

# Copiar todo el código fuente
COPY . .

# Build
WORKDIR /src/WebGESCOMPH
RUN dotnet build "WebGESCOMPH.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publicación
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebGESCOMPH.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Imagen final (runtime con Playwright)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebGESCOMPH.dll"]
