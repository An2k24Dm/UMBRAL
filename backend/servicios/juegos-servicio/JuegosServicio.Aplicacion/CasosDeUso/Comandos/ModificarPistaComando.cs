using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ModificarPistaComando(Guid BusquedaId, Guid PistaId, ModificarPistaDto Dto) : IRequest;
