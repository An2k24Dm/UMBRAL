using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class AgregarMisionManejador : IRequestHandler<AgregarMisionComando, Guid>
{
    private readonly IRepositorioBusquedas _repositorio;

    public AgregarMisionManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Guid> Handle(AgregarMisionComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        var tipo = (TipoMision)comando.Dto.Tipo;
        var mision = busqueda.AgregarMisionAEtapa(
            comando.EtapaId,
            comando.Dto.Titulo,
            comando.Dto.Descripcion,
            tipo,
            comando.Dto.PistaClave);

        await _repositorio.AgregarMisionAsync(comando.EtapaId, mision, cancelacion);

        return mision.Id;
    }
}
