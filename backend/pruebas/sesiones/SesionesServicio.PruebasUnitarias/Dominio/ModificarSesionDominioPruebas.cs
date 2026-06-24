using System;
using System.Linq;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Métodos de dominio para modificar una sesión (HU38): guardas de estado,
// reglas de reducción de capacidad y reemplazo de misiones.
public class ModificarSesionDominioPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Mision = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static SesionIndividual NuevaIndividual(int maxParticipantes = 10)
        => SesionIndividual.Crear(
            "Original", "Demo", AhoraUtc.AddHours(2), "ABC123", Operador, AhoraUtc, maxParticipantes);

    private static SesionGrupal NuevaGrupal(int maxEquipos = 5, int maxPorEquipo = 2)
        => SesionGrupal.Crear(
            "Original", "Demo", AhoraUtc.AddHours(2), "ABC123", Operador, AhoraUtc,
            maxEquipos, maxPorEquipo);

    private static SesionIndividual IndividualEnEstado(EstadoSesion estado)
        => SesionIndividual.Rehidratar(
            Guid.NewGuid(), "Original", "Demo", estado,
            AhoraUtc.AddHours(2), "ABC123", Operador, AhoraUtc, null, null, 10);

    // --- ModificarDatosBasicos ---

    [Fact]
    public void ModificarDatosBasicos_ActualizaCampos()
    {
        var sesion = NuevaIndividual();

        sesion.ModificarDatosBasicos("Nuevo nombre", "Nueva descripción", AhoraUtc.AddHours(4), AhoraUtc);

        sesion.Nombre.Should().Be("Nuevo nombre");
        sesion.Descripcion.Should().Be("Nueva descripción");
        sesion.FechaProgramada.Should().Be(AhoraUtc.AddHours(4));
    }

    [Fact]
    public void ModificarDatosBasicos_NoCambiaCodigoNiEstadoNiOperador()
    {
        var sesion = NuevaIndividual();

        sesion.ModificarDatosBasicos("X", "Y", AhoraUtc.AddHours(4), AhoraUtc);

        sesion.CodigoAcceso.Should().Be("ABC123");
        sesion.Estado.Should().Be(EstadoSesion.Programada);
        sesion.OperadorCreadorId.Should().Be(Operador);
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    public void ModificarDatosBasicos_NoProgramada_Lanza(EstadoSesion estado)
    {
        var sesion = IndividualEnEstado(estado);
        Action accion = () => sesion.ModificarDatosBasicos("X", "Y", AhoraUtc.AddHours(4), AhoraUtc);
        accion.Should().Throw<SesionNoModificableExcepcion>();
    }

    [Fact]
    public void ModificarDatosBasicos_FechaPasada_Lanza()
    {
        var sesion = NuevaIndividual();
        Action accion = () => sesion.ModificarDatosBasicos("X", "Y", AhoraUtc.AddHours(-1), AhoraUtc);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void ModificarDatosBasicos_NombreVacio_Lanza()
    {
        var sesion = NuevaIndividual();
        Action accion = () => sesion.ModificarDatosBasicos("  ", "Y", AhoraUtc.AddHours(4), AhoraUtc);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    // --- ReemplazarMisiones ---

    [Fact]
    public void ReemplazarMisiones_ActualizaLista()
    {
        var sesion = NuevaIndividual();
        var nuevas = new[] { Guid.NewGuid(), Guid.NewGuid() };

        sesion.ReemplazarMisiones(nuevas);

        sesion.Misiones.OrderBy(m => m.Orden).Select(m => m.MisionId).Should().Equal(nuevas);
    }

    [Fact]
    public void ReemplazarMisiones_Vacia_Lanza()
    {
        var sesion = NuevaIndividual();
        Action accion = () => sesion.ReemplazarMisiones(Array.Empty<Guid>());
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void ReemplazarMisiones_NoProgramada_Lanza()
    {
        var sesion = IndividualEnEstado(EstadoSesion.Activa);
        Action accion = () => sesion.ReemplazarMisiones(new[] { Mision });
        accion.Should().Throw<SesionNoModificableExcepcion>();
    }

    // --- Capacidad Individual ---

    [Fact]
    public void IndividualModificarCapacidad_Actualiza()
    {
        var sesion = NuevaIndividual(maxParticipantes: 5);
        sesion.ModificarCapacidad(12);
        sesion.MaximoParticipantes.Should().Be(12);
    }

    [Fact]
    public void IndividualModificarCapacidad_MenorAUno_Lanza()
    {
        var sesion = NuevaIndividual();
        Action accion = () => sesion.ModificarCapacidad(0);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void IndividualModificarCapacidad_PorDebajoDeParticipantesActuales_Lanza()
    {
        var sesionId = Guid.NewGuid();
        var participantes = Enumerable.Range(0, 3)
            .Select(_ => Participante.CrearParaSesionIndividual(
                sesionId, Guid.NewGuid(), AhoraUtc))
            .ToList();
        var sesion = SesionIndividual.Rehidratar(
            sesionId, "Original", "Demo", EstadoSesion.Programada,
            AhoraUtc.AddHours(2), "ABC123", Operador, AhoraUtc,
            null, null, 5, participantes: participantes);

        Action accion = () => sesion.ModificarCapacidad(2);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("No se puede establecer una capacidad menor a la cantidad de participantes actuales.");
    }

    // --- Capacidad Grupal ---

    [Fact]
    public void GrupalModificarCapacidad_Actualiza()
    {
        var sesion = NuevaGrupal();
        sesion.ModificarCapacidad(8, 4);
        sesion.MaximoEquipos.Should().Be(8);
        sesion.MaximoParticipantesPorEquipo.Should().Be(4);
    }

    [Fact]
    public void GrupalModificarCapacidad_EquiposMenorAUno_Lanza()
    {
        var sesion = NuevaGrupal();
        Action accion = () => sesion.ModificarCapacidad(0, 2);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void GrupalModificarCapacidad_PorEquipoMenorADos_Lanza()
    {
        var sesion = NuevaGrupal();
        Action accion = () => sesion.ModificarCapacidad(5, 1);
        accion.Should().Throw<SesionInvalidaExcepcion>();
    }

    [Fact]
    public void GrupalModificarCapacidad_PorDebajoDeEquiposActuales_Lanza()
    {
        var sesion = NuevaGrupal(maxEquipos: 5, maxPorEquipo: 2);
        sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.CrearEquipo("Azul", Guid.NewGuid(), AhoraUtc, AhoraUtc);

        Action accion = () => sesion.ModificarCapacidad(1, 2);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("No se puede establecer una capacidad menor a la cantidad de equipos actuales.");
    }

    [Fact]
    public void GrupalModificarCapacidad_PorEquipoPorDebajoDeIntegrantes_Lanza()
    {
        var sesion = NuevaGrupal(maxEquipos: 5, maxPorEquipo: 3);
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc); // 1 integrante (líder)
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc); // 2
        sesion.AgregarParticipanteAEquipo(equipo.Id, Guid.NewGuid(), AhoraUtc, AhoraUtc); // 3 integrantes

        // El equipo tiene 3 integrantes; bajar la capacidad por equipo a 2
        // (válido por mínimo) debe fallar por los integrantes ya existentes.
        Action accion = () => sesion.ModificarCapacidad(5, 2);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("No se puede establecer una capacidad por equipo menor a la cantidad de integrantes actuales.");
    }

    // --- TieneInscritos ---

    [Fact]
    public void TieneInscritos_Individual()
    {
        var sesion = NuevaIndividual();
        sesion.TieneInscritos.Should().BeFalse();
        sesion.Preparar();
        sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc);
        sesion.TieneInscritos.Should().BeTrue();
    }

    [Fact]
    public void TieneInscritos_Grupal()
    {
        var sesion = NuevaGrupal();
        sesion.TieneInscritos.Should().BeFalse();
        sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        sesion.TieneInscritos.Should().BeTrue();
    }
}
