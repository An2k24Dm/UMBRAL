FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY RankingServicio.Dominio/RankingServicio.Dominio.csproj          RankingServicio.Dominio/
COPY RankingServicio.Commons/RankingServicio.Commons.csproj          RankingServicio.Commons/
COPY RankingServicio.Aplicacion/RankingServicio.Aplicacion.csproj    RankingServicio.Aplicacion/
COPY RankingServicio.Infraestructura/RankingServicio.Infraestructura.csproj  RankingServicio.Infraestructura/
COPY RankingServicio.Presentacion/RankingServicio.Presentacion.csproj        RankingServicio.Presentacion/

RUN dotnet restore RankingServicio.Presentacion/RankingServicio.Presentacion.csproj

COPY RankingServicio.Dominio/          RankingServicio.Dominio/
COPY RankingServicio.Commons/          RankingServicio.Commons/
COPY RankingServicio.Aplicacion/       RankingServicio.Aplicacion/
COPY RankingServicio.Infraestructura/  RankingServicio.Infraestructura/
COPY RankingServicio.Presentacion/     RankingServicio.Presentacion/

RUN dotnet publish RankingServicio.Presentacion/RankingServicio.Presentacion.csproj \
    -c Release -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RankingServicio.Presentacion.dll"]
