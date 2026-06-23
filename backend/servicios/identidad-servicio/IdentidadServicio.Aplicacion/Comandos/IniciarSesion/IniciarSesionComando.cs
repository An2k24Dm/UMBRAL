using IdentidadServicio.Aplicacion.Enums;
using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.IniciarSesion;

public sealed record IniciarSesionComando(
    string NombreUsuario,
    string Contrasena,
    OrigenInicioSesion Origen) : IRequest<ResultadoInicioSesionDto>;
