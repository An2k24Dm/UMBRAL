using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.Aplicacion.Comandos.CrearBusquedaTesoro;

public sealed class CrearBusquedaTesoroManejador : IRequestHandler<CrearBusquedaTesoroComando, Guid>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IProveedorFechaHora _reloj;
    private readonly IValidador<CrearBusquedaTesoroComando> _validador;
    private readonly ILogger<CrearBusquedaTesoroManejador> _registro;

    public CrearBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IProveedorFechaHora reloj,
        IValidador<CrearBusquedaTesoroComando> validador,
        ILogger<CrearBusquedaTesoroManejador> registro)
    {
        _repositorio = repositorio;
        _reloj = reloj;
        _validador = validador;
        _registro = registro;
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
            comando.Dto.Tiempo,
            comando.Dto.Puntaje);

        await _repositorio.CrearBusquedaTesoroAsync(busqueda, cancelacion);

        _registro.LogInformation(
            "Búsqueda del tesoro '{Nombre}' (ID: {Id}) creada por el operador {CreadorId}.",
            busqueda.Nombre, busqueda.Id, busqueda.CreadorId);

        return busqueda.Id;
    }
}
