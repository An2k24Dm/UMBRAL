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
    private readonly IRepositorioIdentidad _repositorio;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;

    public ObtenerPerfilActualManejador(
        IRepositorioIdentidad repositorio,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo)
    {
        _repositorio = repositorio;
        _fabricaMapeo = fabricaMapeo;
    }

    public async Task<PerfilUsuarioDto> Handle(
        ObtenerPerfilActualConsulta consulta, CancellationToken cancelacion)
    {
        var usuario = await _repositorio.ObtenerPorIdKeycloakAsync(consulta.IdKeycloak, cancelacion)
                      ?? throw new DatosUsuarioInvalidosExcepcion("Usuario no registrado.");

        // La fábrica devuelve la instancia derivada apropiada
        // (PerfilAdministradorDto / PerfilOperadorDto / PerfilParticipanteDto).
        return _fabricaMapeo.Mapear(usuario);
    }
}
