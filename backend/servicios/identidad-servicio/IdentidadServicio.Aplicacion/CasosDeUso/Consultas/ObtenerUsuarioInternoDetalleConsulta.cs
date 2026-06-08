using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Consultas;

// HU08 — perfil completo de un usuario interno (Operador / Administrador).
// Reutiliza el árbol PerfilUsuarioDto / PerfilOperadorDto / PerfilAdministradorDto.
public sealed record ObtenerUsuarioInternoDetalleConsulta(Guid Id)
    : IRequest<PerfilUsuarioDto?>;
