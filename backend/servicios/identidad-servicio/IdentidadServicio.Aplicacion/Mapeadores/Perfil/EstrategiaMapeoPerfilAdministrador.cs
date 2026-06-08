using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Mapeadores.Perfil;

public sealed class EstrategiaMapeoPerfilAdministrador : IEstrategiaMapeoPerfilUsuario
{
    public bool PuedeMapear(Usuario usuario) => usuario is Administrador;

    public PerfilUsuarioDto Mapear(Usuario usuario)
    {
        var administrador = (Administrador)usuario;
        var dto = new PerfilAdministradorDto
        {
            CodigoAdministrador = administrador.CodigoAdministrador
        };
        BaseEstrategiaMapeoPerfil.RellenarComunes(usuario, dto);
        return dto;
    }
}
