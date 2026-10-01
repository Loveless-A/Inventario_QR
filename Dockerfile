# syntax=docker/dockerfile:1

# ---------- Etapa 1: compilacion ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Primero solo el .csproj para que la restauracion de NuGet quede cacheada
COPY Inventario-QR/Inventario-QR.csproj Inventario-QR/
RUN dotnet restore Inventario-QR/Inventario-QR.csproj

# Luego el resto del codigo fuente (wwwroot, Pages, Migrations, etc.)
COPY Inventario-QR/ Inventario-QR/
RUN dotnet publish Inventario-QR/Inventario-QR.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Etapa 2: ejecucion ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl se usa en el healthcheck del contenedor
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Usuario sin privilegios
RUN groupadd --system --gid 1001 inventario \
    && useradd --system --uid 1001 --gid inventario --create-home inventario

# Copia con propietario correcto desde la etapa de compilacion
COPY --from=build --chown=inventario:inventario /app/publish ./

# Directorio persistente para las claves de Data Protection (cookies de sesion)
RUN mkdir -p /var/lib/inventario-qr/keys \
    && chown -R inventario:inventario /var/lib/inventario-qr
VOLUME ["/var/lib/inventario-qr"]

USER inventario

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD curl -fsS http://127.0.0.1:8080/Login || exit 1

ENTRYPOINT ["dotnet", "Inventario-QR.dll"]
