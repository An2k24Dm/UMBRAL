using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record IniciarSesionComando(
    string NombreUsuario,
    string Contrasena,
    OrigenInicioSesion Origen) : IRequest<ResultadoInicioSesionDto>;
