using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.CrearBusquedaTesoro;

public sealed class CrearBusquedaTesoroManejador : IRequestHandler<CrearBusquedaTesoroComando, Guid>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IProveedorFechaHora _reloj;
    private readonly IValidador<CrearBusquedaTesoroComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public CrearBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IProveedorFechaHora reloj,
        IValidador<CrearBusquedaTesoroComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _reloj = reloj;
        _validador = validador;
        _registroLogs = registroLogs;
    }

    public async Task<Guid> Handle(CrearBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (await _repositorio.ExisteBusquedaConNombreAsync(comando.Dto.Nombre, cancelacion))
            throw new ExcepcionDominio($"Ya existe una búsqueda del tesoro con el nombre '{comando.Dto.Nombre}'.");

        var busqueda = BusquedaTesoro.Crear(
            comando.Dto.Nombre,
            comando.Dto.Descripcion,
            comando.CreadorId,
            _reloj.ObtenerFechaHoraUtc(),
            Tiempo.CrearParaBusqueda(comando.Dto.Tiempo),
            Puntaje.CrearParaBusqueda(comando.Dto.Puntaje));

        await _repositorio.CrearBusquedaTesoroAsync(busqueda, cancelacion);

        _registroLogs.Informacion(
            evento: "BusquedaTesoroCreada",
            descripcion: "Usuario creó una búsqueda del tesoro correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["BusquedaId"] = busqueda.Id,
                ["Nombre"] = busqueda.Nombre,
                ["CreadorId"] = busqueda.CreadorId
            });

        return busqueda.Id;
    }
}
