using FluentAssertions;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

namespace IdentidadServicio.PruebasUnitarias.Fabricas;

public class FabricaEstrategiaMapeoPerfilUsuarioPruebas
{
    // Construye la fábrica con TODAS las estrategias reales para verificar el
    // ruteo correcto entre tipos derivados de Usuario.
    private static FabricaEstrategiaMapeoPerfilUsuario CrearFabricaCompleta()
    {
        return new FabricaEstrategiaMapeoPerfilUsuario(new IEstrategiaMapeoPerfilUsuario[]
        {
            new EstrategiaMapeoPerfilAdministrador(),
            new EstrategiaMapeoPerfilOperador(),
            new EstrategiaMapeoPerfilParticipante()
        });
    }

    [Fact]
    public void Mapear_ConOperador_DevuelvePerfilOperadorDto()
    {
        var resultado = CrearFabricaCompleta().Mapear(UsuariosDePrueba.NuevoOperador());
        resultado.Should().BeOfType<PerfilOperadorDto>();
    }

    [Fact]
    public void Mapear_ConAdministrador_DevuelvePerfilAdministradorDto()
    {
        var resultado = CrearFabricaCompleta().Mapear(UsuariosDePrueba.NuevoAdministrador());
        resultado.Should().BeOfType<PerfilAdministradorDto>();
    }

    [Fact]
    public void Mapear_ConParticipante_DevuelvePerfilParticipanteDto()
    {
        var resultado = CrearFabricaCompleta().Mapear(UsuariosDePrueba.NuevoParticipante());
        resultado.Should().BeOfType<PerfilParticipanteDto>();
    }

    [Fact]
    public void Mapear_SinEstrategiaCompatible_LanzaRolNoValido()
    {
        // Fábrica sin estrategias: ninguna PuedeMapear el usuario recibido.
        var fabrica = new FabricaEstrategiaMapeoPerfilUsuario(
            Array.Empty<IEstrategiaMapeoPerfilUsuario>());

        Action accion = () => fabrica.Mapear(UsuariosDePrueba.NuevoOperador());

        accion.Should().Throw<RolNoValidoExcepcion>();
    }

    [Fact]
    public void Mapear_ConUsuarioDeTipoDesconocido_LanzaRolNoValido()
    {
        // Usuario derivado fuera de los tres tipos conocidos: ninguna estrategia
        // existente puede mapearlo.
        var fabrica = CrearFabricaCompleta();
        var usuarioDesconocido = new UsuarioDesconocidoDePrueba();

        Action accion = () => fabrica.Mapear(usuarioDesconocido);

        accion.Should().Throw<RolNoValidoExcepcion>();
    }

    // Subclase de Usuario que no coincide con Administrador, Operador ni
    // Participante; sirve para validar la ruta de error de la fábrica.
    private sealed class UsuarioDesconocidoDePrueba : Usuario
    {
        public UsuarioDesconocidoDePrueba()
            : base(
                id: Guid.NewGuid(),
                nombreUsuario: NombreUsuario.Crear("desconocido01"),
                correo: Correo.Crear("desconocido@umbral.com"),
                rol: RolUsuario.Administrador,
                estado: EstadoUsuario.Activo,
                fechaRegistro: UsuariosDePrueba.FechaRegistro,
                nombrePersona: NombrePersona.Crear("Des", "Conocido"),
                datosContacto: DatosContacto.Crear(
                    UsuariosDePrueba.Direccion, UsuariosDePrueba.Telefono),
                sexo: SexoPersona.Indefinido,
                fechaNacimiento: UsuariosDePrueba.FechaNacimiento)
        {
        }
    }
}
