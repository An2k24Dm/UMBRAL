using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record AgregarMisionComando(Guid BusquedaId, AgregarMisionDto Dto) : IRequest<Guid>;
