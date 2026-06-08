FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) Copiar solo los .csproj para aprovechar caché de restore.
COPY JuegosServicio.Dominio/JuegosServicio.Dominio.csproj          JuegosServicio.Dominio/
COPY JuegosServicio.Commons/JuegosServicio.Commons.csproj          JuegosServicio.Commons/
COPY JuegosServicio.Aplicacion/JuegosServicio.Aplicacion.csproj    JuegosServicio.Aplicacion/
COPY JuegosServicio.Infraestructura/JuegosServicio.Infraestructura.csproj  JuegosServicio.Infraestructura/
COPY JuegosServicio.Presentacion/JuegosServicio.Presentacion.csproj                  JuegosServicio.Presentacion/

RUN dotnet restore JuegosServicio.Presentacion/JuegosServicio.Presentacion.csproj

# 2) Copiar el resto del código y publicar.
COPY JuegosServicio.Dominio/          JuegosServicio.Dominio/
COPY JuegosServicio.Commons/          JuegosServicio.Commons/
COPY JuegosServicio.Aplicacion/       JuegosServicio.Aplicacion/
COPY JuegosServicio.Infraestructura/  JuegosServicio.Infraestructura/
COPY JuegosServicio.Presentacion/              JuegosServicio.Presentacion/

RUN dotnet publish JuegosServicio.Presentacion/JuegosServicio.Presentacion.csproj \
    -c Release -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "JuegosServicio.Presentacion.dll"]
