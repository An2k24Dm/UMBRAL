using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

// HU33 — Consulta usada por sesiones-servicio para resolver un contenido
// (Trivia o BúsquedaTesoro) por tipo + id antes de crear una sesión.
public sealed record ObtenerContenidoActivoConsulta(string TipoJuego, Guid ContenidoId)
    : IRequest<ContenidoJuegoActivoDto?>;
