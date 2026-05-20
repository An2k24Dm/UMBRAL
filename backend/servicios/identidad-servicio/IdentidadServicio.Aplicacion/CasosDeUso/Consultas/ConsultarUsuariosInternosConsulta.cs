using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Consultas;

// HU08 — consulta paginada de cuentas internas (Operador / Administrador).
// El valor de "Rol" admitido en el filtro es "Todos", "Operador" o
// "Administrador"; el manejador se encarga de traducirlo al puerto.
public sealed record ConsultarUsuariosInternosConsulta(
    int Pagina,
    int TamanioPagina,
    string? Rol,
    string? OrdenEstado) : IRequest<ResultadoPaginadoDto<UsuarioInternoListadoDto>>;
