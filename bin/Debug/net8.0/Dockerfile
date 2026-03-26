# ================================================
# Etapa 1: Base (Runtime) - Para ejecutar la app
# ================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# ================================================
# Etapa 2: Build - Para compilar
# ================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el csproj primero (mejora caché de Docker)
COPY ["FarmaciaSantaRita.csproj", "./"]
RUN dotnet restore "FarmaciaSantaRita.csproj"

# Copiar el resto del código fuente
COPY . .
WORKDIR "/src"
RUN dotnet build "FarmaciaSantaRita.csproj" -c Release -o /app/build

# ================================================
# Etapa 3: Publish - Preparar para producción
# ================================================
FROM build AS publish
RUN dotnet publish "FarmaciaSantaRita.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ================================================
# Etapa 4: Final - Imagen ligera para Railway
# ================================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FarmaciaSantaRita.dll"]