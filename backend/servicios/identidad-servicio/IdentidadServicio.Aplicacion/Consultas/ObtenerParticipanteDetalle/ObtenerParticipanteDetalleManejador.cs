using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.Consultas.ObtenerParticipanteDetalle;

public sealed class ObtenerParticipanteDetalleManejador
    : IRequestHandler<ObtenerParticipanteDetalleConsulta, PerfilParticipanteDto>
{
    private readonly IRepositorioParticipantes _repositorio;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;

    public ObtenerParticipanteDetalleManejador(
        IRepositorioParticipantes repositorio,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo)
    {
        _repositorio = repositorio;
        _fabricaMapeo = fabricaMapeo;
    }

    public async Task<PerfilParticipanteDto> Handle(
        ObtenerParticipanteDetalleConsulta consulta, CancellationToken cancelacion)
    {
        var participante = await _repositorio.ObtenerPorIdAsync(consulta.Id, cancelacion)
                           ?? throw new DatosUsuarioInvalidosExcepcion("Participante no encontrado.");

        return (PerfilParticipanteDto)_fabricaMapeo.Mapear(participante);
    }
}
