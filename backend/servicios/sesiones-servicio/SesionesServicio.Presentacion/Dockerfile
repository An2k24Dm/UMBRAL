FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) Copiar sólo los .csproj para aprovechar caché de restore.
COPY SesionesServicio.Dominio/SesionesServicio.Dominio.csproj                  SesionesServicio.Dominio/
COPY SesionesServicio.Commons/SesionesServicio.Commons.csproj                  SesionesServicio.Commons/
COPY SesionesServicio.Aplicacion/SesionesServicio.Aplicacion.csproj            SesionesServicio.Aplicacion/
COPY SesionesServicio.Infraestructura/SesionesServicio.Infraestructura.csproj  SesionesServicio.Infraestructura/
COPY SesionesServicio.Presentacion/SesionesServicio.Presentacion.csproj                          SesionesServicio.Presentacion/

RUN dotnet restore SesionesServicio.Presentacion/SesionesServicio.Presentacion.csproj

# 2) Copiar el resto del código y publicar.
COPY SesionesServicio.Dominio/          SesionesServicio.Dominio/
COPY SesionesServicio.Commons/          SesionesServicio.Commons/
COPY SesionesServicio.Aplicacion/       SesionesServicio.Aplicacion/
COPY SesionesServicio.Infraestructura/  SesionesServicio.Infraestructura/
COPY SesionesServicio.Presentacion/              SesionesServicio.Presentacion/

RUN dotnet publish SesionesServicio.Presentacion/SesionesServicio.Presentacion.csproj \
    -c Release -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SesionesServicio.Presentacion.dll"]
