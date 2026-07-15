using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.IngresarSesionPorCodigo;

public sealed record IngresarSesionPorCodigoComando(
    IngresarSesionDto Datos) : IRequest<IngresarSesionRespuestaDto>;
