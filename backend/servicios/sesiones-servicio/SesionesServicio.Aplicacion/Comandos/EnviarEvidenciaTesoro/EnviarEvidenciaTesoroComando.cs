using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.EnviarEvidenciaTesoro;

public sealed record EnviarEvidenciaTesoroComando(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid BusquedaId,
    string CodigoEscaneado) : IRequest<EvidenciaTesoroRespuestaDto>;
