using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.AgregarPista;

public sealed record AgregarPistaComando(Guid BusquedaId, AgregarPistaDto Dto) : IRequest<Guid>;
