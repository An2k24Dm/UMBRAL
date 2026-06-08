using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record AgregarEtapaComando(Guid MisionId, AgregarEtapaDto Dto) : IRequest<Guid>;
