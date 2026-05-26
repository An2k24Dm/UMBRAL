using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record AgregarEtapaComando(Guid BusquedaId, AgregarEtapaDto Dto) : IRequest<Guid>;
