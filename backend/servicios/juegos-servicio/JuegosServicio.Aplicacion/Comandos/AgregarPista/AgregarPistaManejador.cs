using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.AgregarPista;

public sealed class AgregarPistaManejador : IRequestHandler<AgregarPistaComando, Guid>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IValidador<AgregarPistaComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public AgregarPistaManejador(
        IRepositorioBusquedas repositorio,
        IRepositorioMisiones repositorioMisiones,
        IValidador<AgregarPistaComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _validador = validador;
        _registroLogs = registroLogs;
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

        _registroLogs.Informacion(
            evento: "PistaAgregada",
            descripcion: "Usuario agregó una pista correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["BusquedaId"] = comando.BusquedaId,
                ["PistaId"] = pista.Id
            });

        return pista.Id;
    }
}
