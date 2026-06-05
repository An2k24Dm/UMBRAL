FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) Copiar solo los .csproj para aprovechar caché de restore.
COPY IdentidadServicio.Dominio/IdentidadServicio.Dominio.csproj          IdentidadServicio.Dominio/
COPY IdentidadServicio.Commons/IdentidadServicio.Commons.csproj          IdentidadServicio.Commons/
COPY IdentidadServicio.Aplicacion/IdentidadServicio.Aplicacion.csproj    IdentidadServicio.Aplicacion/
COPY IdentidadServicio.Infraestructura/IdentidadServicio.Infraestructura.csproj  IdentidadServicio.Infraestructura/
COPY IdentidadServicio.Presentacion/IdentidadServicio.Presentacion.csproj                  IdentidadServicio.Presentacion/

RUN dotnet restore IdentidadServicio.Presentacion/IdentidadServicio.Presentacion.csproj

# 2) Copiar el resto del código y publicar.
COPY IdentidadServicio.Dominio/          IdentidadServicio.Dominio/
COPY IdentidadServicio.Commons/          IdentidadServicio.Commons/
COPY IdentidadServicio.Aplicacion/       IdentidadServicio.Aplicacion/
COPY IdentidadServicio.Infraestructura/  IdentidadServicio.Infraestructura/
COPY IdentidadServicio.Presentacion/              IdentidadServicio.Presentacion/

RUN dotnet publish IdentidadServicio.Presentacion/IdentidadServicio.Presentacion.csproj \
    -c Release -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "IdentidadServicio.Presentacion.dll"]
