using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU07: detalle/perfil de un Participante seleccionado desde la lista.
// Reutiliza la fábrica de estrategias de mapeo de perfil (HU06) para no
// duplicar el armado del DTO base. La fábrica resuelve EstrategiaMapeoPerfil
// Participante porque el repositorio sólo devuelve Participantes (los
// usuarios internos se filtran ahí).
public sealed class ObtenerParticipanteDetalleManejador
    : IRequestHandler<ObtenerParticipanteDetalleConsulta, PerfilParticipanteDto>
{
    private readonly IRepositorioIdentidad _repositorio;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;

    public ObtenerParticipanteDetalleManejador(
        IRepositorioIdentidad repositorio,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo)
    {
        _repositorio = repositorio;
        _fabricaMapeo = fabricaMapeo;
    }

    public async Task<PerfilParticipanteDto> Handle(
        ObtenerParticipanteDetalleConsulta consulta, CancellationToken cancelacion)
    {
        var participante = await _repositorio.ObtenerParticipantePorIdAsync(consulta.Id, cancelacion)
                           ?? throw new DatosUsuarioInvalidosExcepcion("Participante no encontrado.");

        return (PerfilParticipanteDto)_fabricaMapeo.Mapear(participante);
    }
}
