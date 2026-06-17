using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerUsuarioInternoDetalleManejador
    : IRequestHandler<ObtenerUsuarioInternoDetalleConsulta, PerfilUsuarioDto?>
{
    private readonly IRepositorioUsuariosLectura _repositorio;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;

    public ObtenerUsuarioInternoDetalleManejador(
        IRepositorioUsuariosLectura repositorio,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo)
    {
        _repositorio = repositorio;
        _fabricaMapeo = fabricaMapeo;
    }

    public async Task<PerfilUsuarioDto?> Handle(
        ObtenerUsuarioInternoDetalleConsulta consulta, CancellationToken cancelacion)
    {

        var usuario = await _repositorio.ObtenerUsuarioInternoPorIdAsync(consulta.Id, cancelacion);
        if (usuario is null) return null;

        return _fabricaMapeo.Mapear(usuario);
    }
}
