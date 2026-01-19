# Usa la imagen oficial de ASP.NET para ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Usa el SDK de .NET 8 para compilar el código
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FarmaciaSantaRita.csproj", "."]
RUN dotnet restore "FarmaciaSantaRita.csproj"
COPY . .
RUN dotnet build "FarmaciaSantaRita.csproj" -c Release -o /app/build

# Publica la aplicación
FROM build AS publish
RUN dotnet publish "FarmaciaSantaRita.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Capa final: copia los archivos publicados y define cómo arrancar
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FarmaciaSantaRita.dll"]