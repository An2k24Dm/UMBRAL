using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Mapeadores.Perfil;

public sealed class EstrategiaMapeoPerfilParticipante : IEstrategiaMapeoPerfilUsuario
{
    public bool PuedeMapear(Usuario usuario) => usuario is Participante;

    public PerfilUsuarioDto Mapear(Usuario usuario)
    {
        var participante = (Participante)usuario;
        var dto = new PerfilParticipanteDto
        {
            Alias = participante.Alias
        };
        BaseEstrategiaMapeoPerfil.RellenarComunes(usuario, dto);
        return dto;
    }
}
