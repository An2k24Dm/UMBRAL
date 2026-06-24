using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Consultas.ObtenerSesionPorId;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public class ObtenerSesionPorIdManejadorPruebas
{
    private static readonly DateTime AhoraUtc =
        new(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid OperadorId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid SesionId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ParticipanteIdentidadId =
        Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public async Task DetalleIndividual_DevuelveParticipantesConDatosBasicosDeIdentidad()
    {
        var participante = Participante.CrearParaSesionIndividual(
            SesionId, ParticipanteIdentidadId, AhoraUtc.AddMinutes(5));
        var sesion = SesionIndividual.Rehidratar(
            SesionId, "Individual", "Demo", EstadoSesion.EnPreparacion,
            AhoraUtc.AddHours(1), "ABC123", OperadorId, AhoraUtc,
            null, null, 10, participantes: new[] { participante });

        var repositorio = new Mock<IRepositorioSesiones>();
        var usuario = new Mock<IUsuarioActual>();
        var identidad = new Mock<IClienteIdentidadParticipantes>();

        repositorio.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        usuario.Setup(u => u.EstaAutenticado()).Returns(true);
        usuario.Setup(u => u.ObtenerId()).Returns(OperadorId);
        usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
            .Returns<string[]>(roles => roles.Contains("Operador"));
        identidad.Setup(c => c.ObtenerParticipantesPorIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ParticipanteIdentidadResumenDto>
            {
                [ParticipanteIdentidadId] = new()
                {
                    Id = ParticipanteIdentidadId,
                    Alias = "luna",
                    Nombre = "Luna",
                    Apellido = "Rivas"
                }
            });

        var fabrica = new FabricaMapeadorDetalleSesion(
            new IMapeadorDetalleSesion[]
            {
                new MapeadorDetalleSesionIndividual(),
                new MapeadorDetalleSesionGrupal()
            });
        var manejador = new ObtenerSesionPorIdManejador(
            repositorio.Object, usuario.Object, fabrica, identidad.Object);

        var dto = await manejador.Handle(
            new ObtenerSesionPorIdConsulta(SesionId), CancellationToken.None);

        dto.Should().NotBeNull();
        var participanteDto = dto!.ParticipantesIndividuales.Should().ContainSingle().Subject;
        participanteDto.ParticipanteSesionId.Should().Be(participante.Id);
        participanteDto.ParticipanteIdentidadId.Should().Be(ParticipanteIdentidadId);
        participanteDto.Alias.Should().Be("luna");
        participanteDto.Nombre.Should().Be("Luna");
        participanteDto.Apellido.Should().Be("Rivas");
        participanteDto.Puntaje.Should().Be(participante.Puntaje);
        participanteDto.FechaUnion.Should().Be(participante.FechaUnionSesion);

        typeof(SesionesServicio.Commons.Dtos.ParticipanteSesionDto)
            .GetProperties()
            .Select(p => p.Name.ToLowerInvariant())
            .Should()
            .NotContain(n =>
                n.Contains("correo") ||
                n.Contains("telefono") ||
                n.Contains("direcci") ||
                n.Contains("nacimiento") ||
                n.Contains("keycloak") ||
                n.Contains("contrasena") ||
                n.Contains("hash"));
        identidad.Verify(c => c.ObtenerParticipantesPorIdsAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { ParticipanteIdentidadId })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
