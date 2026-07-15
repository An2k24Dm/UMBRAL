using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarPista;

public sealed record ModificarPistaComando(Guid BusquedaId, Guid PistaId, ModificarPistaDto Dto) : IRequest;
