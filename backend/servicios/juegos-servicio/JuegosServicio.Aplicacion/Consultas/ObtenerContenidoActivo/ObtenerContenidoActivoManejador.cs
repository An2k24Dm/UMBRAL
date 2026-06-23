using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Enums;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerContenidoActivo;

// HU33 — Devuelve el contenido (Trivia o BúsquedaTesoro) por id si
// existe, junto con su estado. sesiones-servicio diferencia entre "no
// encontrado" e "inactivo" leyendo EstaActivo.
public sealed class ObtenerContenidoActivoManejador
    : IRequestHandler<ObtenerContenidoActivoConsulta, ContenidoJuegoActivoDto?>
{
    private readonly IRepositorioJuegos _repositorioJuegos;
    private readonly IRepositorioBusquedas _repositorioBusquedas;

    public ObtenerContenidoActivoManejador(
        IRepositorioJuegos repositorioJuegos,
        IRepositorioBusquedas repositorioBusquedas)
    {
        _repositorioJuegos = repositorioJuegos;
        _repositorioBusquedas = repositorioBusquedas;
    }

    public async Task<ContenidoJuegoActivoDto?> Handle(
        ObtenerContenidoActivoConsulta consulta, CancellationToken cancelacion)
    {
        if (string.Equals(consulta.TipoJuego, "Trivia", StringComparison.OrdinalIgnoreCase))
        {
            var trivia = await _repositorioJuegos.ObtenerTriviaPorIdAsync(
                consulta.ContenidoId, cancelacion);
            if (trivia is null) return null;

            return new ContenidoJuegoActivoDto
            {
                Id = trivia.Id,
                Nombre = trivia.Nombre,
                TipoJuego = "Trivia",
                Estado = trivia.Estado.ToString(),
                EstaActivo = trivia.Estado == EstadoTrivia.Activa
            };
        }

        if (string.Equals(consulta.TipoJuego, "BusquedaTesoro", StringComparison.OrdinalIgnoreCase))
        {
            var busqueda = await _repositorioBusquedas.ObtenerBusquedaPorIdAsync(
                consulta.ContenidoId, cancelacion);
            if (busqueda is null) return null;

            return new ContenidoJuegoActivoDto
            {
                Id = busqueda.Id,
                Nombre = busqueda.Nombre,
                TipoJuego = "BusquedaTesoro",
                Estado = busqueda.Estado.ToString(),
                EstaActivo = busqueda.Estado == EstadoBusqueda.Activa
            };
        }

        return null;
    }
}
