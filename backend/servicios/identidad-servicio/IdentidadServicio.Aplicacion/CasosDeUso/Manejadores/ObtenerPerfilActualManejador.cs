using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerPerfilActualManejador
    : IRequestHandler<ObtenerPerfilActualConsulta, PerfilUsuarioDto>
{
    private readonly IRepositorioUsuariosLectura _repositorioLectura;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;

    public ObtenerPerfilActualManejador(
        IRepositorioUsuariosLectura repositorioLectura,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo)
    {
        _repositorioLectura = repositorioLectura;
        _fabricaMapeo = fabricaMapeo;
    }

    public async Task<PerfilUsuarioDto> Handle(
        ObtenerPerfilActualConsulta consulta, CancellationToken cancelacion)
    {
        var usuario = await _repositorioLectura.ObtenerPorIdKeycloakAsync(consulta.IdKeycloak, cancelacion)
                      ?? throw new DatosUsuarioInvalidosExcepcion("Usuario no registrado.");

        return _fabricaMapeo.Mapear(usuario);
    }
}
