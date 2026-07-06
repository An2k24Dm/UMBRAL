FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PartidasServicio.Dominio/PartidasServicio.Dominio.csproj                    PartidasServicio.Dominio/
COPY PartidasServicio.Commons/PartidasServicio.Commons.csproj                    PartidasServicio.Commons/
COPY PartidasServicio.Aplicacion/PartidasServicio.Aplicacion.csproj              PartidasServicio.Aplicacion/
COPY PartidasServicio.Infraestructura/PartidasServicio.Infraestructura.csproj    PartidasServicio.Infraestructura/
COPY PartidasServicio.Presentacion/PartidasServicio.Presentacion.csproj          PartidasServicio.Presentacion/

RUN dotnet restore PartidasServicio.Presentacion/PartidasServicio.Presentacion.csproj

COPY PartidasServicio.Dominio/          PartidasServicio.Dominio/
COPY PartidasServicio.Commons/          PartidasServicio.Commons/
COPY PartidasServicio.Aplicacion/       PartidasServicio.Aplicacion/
COPY PartidasServicio.Infraestructura/  PartidasServicio.Infraestructura/
COPY PartidasServicio.Presentacion/     PartidasServicio.Presentacion/

RUN dotnet publish PartidasServicio.Presentacion/PartidasServicio.Presentacion.csproj \
    -c Release -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PartidasServicio.Presentacion.dll"]
