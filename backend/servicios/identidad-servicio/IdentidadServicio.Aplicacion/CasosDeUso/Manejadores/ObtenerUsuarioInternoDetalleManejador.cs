using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerUsuarioInternoDetalleManejador
    : IRequestHandler<ObtenerUsuarioInternoDetalleConsulta, PerfilUsuarioDto?>
{
    private readonly IRepositorioIdentidad _repositorio;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;

    public ObtenerUsuarioInternoDetalleManejador(
        IRepositorioIdentidad repositorio,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo)
    {
        _repositorio = repositorio;
        _fabricaMapeo = fabricaMapeo;
    }

    public async Task<PerfilUsuarioDto?> Handle(
        ObtenerUsuarioInternoDetalleConsulta consulta, CancellationToken cancelacion)
    {
        // El puerto ya garantiza que no devuelva Participantes para HU08;
        // si el id corresponde a un Participante, llega null y el controlador
        // responde 404.
        var usuario = await _repositorio.ObtenerUsuarioInternoPorIdAsync(consulta.Id, cancelacion);
        if (usuario is null) return null;

        return _fabricaMapeo.Mapear(usuario);
    }
}
