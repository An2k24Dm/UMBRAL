using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

namespace IdentidadServicio.PruebasUnitarias.Dtos;

// HU07: el mapeo Participante -> ParticipanteListadoDto vive dentro del
// manejador. Estas pruebas verifican que ese mapeo llena exactamente los
// campos esperados y que NO incluye códigos de usuarios internos.
public class ParticipanteListadoDtoPruebas
{
    private static ParticipanteListadoDto MapearViaManejador(Participante participante)
    {
        var repositorio = new Mock<IRepositorioParticipantes>();
        repositorio
            .Setup(r => r.ConsultarAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { participante });
        repositorio
            .Setup(r => r.ContarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manejador = new ConsultarParticipantesManejador(repositorio.Object);
        var resultado = manejador
            .Handle(new ConsultarParticipantesConsulta(1, 10, null), CancellationToken.None)
            .GetAwaiter().GetResult();
        return resultado.Elementos.Single();
    }

    [Fact]
    public void Mapeo_LlenaCorrectamenteLosCamposDelDto()
    {
        var participante = UsuariosDePrueba.NuevoParticipante(
            nombreUsuario: "participante01",
            nombre: "Pablo",
            apellido: "Participante",
            alias: "sombra01");

        var dto = MapearViaManejador(participante);

        dto.Alias.Should().Be("sombra01");
        dto.NombreUsuario.Should().Be("participante01");
        dto.Nombre.Should().Be("Pablo");
        dto.Apellido.Should().Be("Participante");
        dto.Estado.Should().Be("Activo");
        dto.Sexo.Should().Be("Masculino");
    }

    [Fact]
    public void Dto_NoExponeCodigoOperadorNiCodigoAdministrador()
    {
        var propiedades = typeof(ParticipanteListadoDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToArray();

        propiedades.Should().NotContain("CodigoOperador");
        propiedades.Should().NotContain("CodigoAdministrador");
    }
}
