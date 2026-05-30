using MediatR;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.CasosDeUso.Consultas;

// HU33 — Consulta usada por juegos-servicio antes de desactivar un
// contenido. Devuelve true si existe al menos una sesión en estado
// vigente (Programada, EnPreparacion, Activa, Pausada) asociada al
// contenido (TipoJuego + ContenidoJuegoId) indicado.
public sealed record ExisteSesionVigentePorContenidoConsulta(
    TipoJuego TipoJuego,
    Guid ContenidoJuegoId) : IRequest<ExisteSesionVigenteRespuestaDto>;
