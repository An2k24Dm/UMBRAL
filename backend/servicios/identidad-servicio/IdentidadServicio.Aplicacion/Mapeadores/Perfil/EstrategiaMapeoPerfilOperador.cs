using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Mapeadores.Perfil;

public sealed class EstrategiaMapeoPerfilOperador : IEstrategiaMapeoPerfilUsuario
{
    public bool PuedeMapear(Usuario usuario) => usuario is Operador;

    public PerfilUsuarioDto Mapear(Usuario usuario)
    {
        var operador = (Operador)usuario;
        var dto = new PerfilOperadorDto
        {
            CodigoOperador = operador.CodigoOperador
        };
        BaseEstrategiaMapeoPerfil.RellenarComunes(usuario, dto);
        return dto;
    }
}
