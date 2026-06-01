using MediatR;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.CasosDeUso.Consultas;

// HU34 — Listado con filtros opcionales por TipoJuego y Estado. La
// regla de visibilidad por rol se aplica en el manejador (no en el
// controlador) para mantener una sola fuente de verdad.
public sealed record ListarSesionesConsulta(
    TipoJuego? TipoJuego,
    EstadoSesion? Estado) : IRequest<List<SesionListadoDto>>;
