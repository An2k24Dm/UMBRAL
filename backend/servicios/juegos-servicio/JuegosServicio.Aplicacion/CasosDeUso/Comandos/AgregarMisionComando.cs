using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record AgregarMisionComando(Guid BusquedaId, Guid EtapaId, AgregarMisionDto Dto) : IRequest<Guid>;
