using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.AgregarEtapa;

public sealed record AgregarEtapaComando(Guid MisionId, AgregarEtapaDto Dto) : IRequest<Guid>;
