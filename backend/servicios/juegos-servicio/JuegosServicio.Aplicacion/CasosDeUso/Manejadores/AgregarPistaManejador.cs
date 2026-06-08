using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class AgregarPistaManejador : IRequestHandler<AgregarPistaComando, Guid>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IValidador<AgregarPistaComando> _validador;

    public AgregarPistaManejador(
        IRepositorioBusquedas repositorio,
        IRepositorioMisiones repositorioMisiones,
        IValidador<AgregarPistaComando> validador)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _validador = validador;
    }

    public async Task<Guid> Handle(AgregarPistaComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (await _repositorioMisiones.EsContenidoUsadoEnMisionActivaAsync(
                TipoModoDeJuego.BusquedaTesoro, comando.BusquedaId, cancelacion))
            throw new ContenidoUsadoEnMisionActivaExcepcion();

        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        var pista = busqueda.AgregarPista(comando.Dto.Contenido);

        await _repositorio.AgregarPistaAsync(pista, cancelacion);

        return pista.Id;
    }
}
