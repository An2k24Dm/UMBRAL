using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ModificarEtapaComando(Guid BusquedaId, Guid EtapaId, ModificarEtapaDto Dto)
    : IRequest;
