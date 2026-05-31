using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record AgregarPistaComando(Guid BusquedaId, Guid EtapaId, AgregarPistaDto Dto) : IRequest<Guid>;
