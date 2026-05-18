using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerPerfilActualManejador
    : IRequestHandler<ObtenerPerfilActualConsulta, PerfilUsuarioDto>
{
    private readonly IRepositorioIdentidad _repositorio;

    public ObtenerPerfilActualManejador(IRepositorioIdentidad repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<PerfilUsuarioDto> Handle(
        ObtenerPerfilActualConsulta consulta, CancellationToken cancelacion)
    {
        var usuario = await _repositorio.ObtenerPorIdKeycloakAsync(consulta.IdKeycloak, cancelacion)
                      ?? throw new DatosUsuarioInvalidosExcepcion("Usuario no registrado.");

        return DtoMapeador.APerfilUsuario(usuario);
    }
}
