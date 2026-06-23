using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Entidades;

public sealed class Equipo
{
    private readonly List<Participante> _participantes = new();

    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public NombreEquipo Nombre { get; private set; } = null!;
    public Guid LiderParticipanteId { get; private set; }
    // Puntaje se mantiene como int en HU40 (siempre nace en 0). Se evaluará
    // promoverlo a Value Object en historias futuras de scoring/ranking y
    // penalizaciones, donde tendrá invariantes propias y trazabilidad.
    public int Puntaje { get; private set; }
    public TipoEquipo Tipo { get; private set; }
    public ContrasenaEquipoHash? ContrasenaHash { get; private set; }
    public int CapacidadMaxima { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public IReadOnlyList<Participante> Participantes => _participantes.AsReadOnly();

    private Equipo() { }

    internal static Equipo CrearConLider(
        Guid equipoId, Guid sesionId, NombreEquipo nombre,
        TipoEquipo tipo, ContrasenaEquipoHash? contrasenaHash,
        int capacidadMaxima, Participante lider, DateTime fechaCreacionUtc)
    {
        if (nombre is null)
            throw new EquipoInvalidoExcepcion("El nombre del equipo es obligatorio.");
        if (lider is null)
            throw new EquipoInvalidoExcepcion("El líder del equipo es obligatorio.");
        if (lider.EquipoId != equipoId)
            throw new EquipoInvalidoExcepcion(
                "El líder debe pertenecer al equipo que se está creando.");
        if (capacidadMaxima < PoliticaCapacidadSesion.MinimoParticipantesPorEquipo)
            throw new EquipoInvalidoExcepcion(
                "La capacidad del equipo debe ser al menos " +
                $"{PoliticaCapacidadSesion.MinimoParticipantesPorEquipo}.");

        ValidarConsistenciaTipoContrasena(tipo, contrasenaHash);

        var equipo = new Equipo
        {
            Id = equipoId,
            SesionId = sesionId,
            Nombre = nombre,
            LiderParticipanteId = lider.Id,
            Puntaje = 0,
            Tipo = tipo,
            ContrasenaHash = tipo == TipoEquipo.Privado ? contrasenaHash : null,
            CapacidadMaxima = capacidadMaxima,
            FechaCreacion = fechaCreacionUtc
        };
        equipo._participantes.Add(lider);
        return equipo;
    }

    internal void AgregarParticipante(Participante participante)
    {
        if (participante is null)
            throw new EquipoInvalidoExcepcion("El participante es obligatorio.");
        if (participante.EquipoId != Id)
            throw new EquipoInvalidoExcepcion(
                "El participante no pertenece a este equipo.");
        if (EstaLleno())
            throw new EquipoInvalidoExcepcion(
                "El equipo alcanzó el máximo de participantes permitido.");
        if (_participantes.Any(p => p.ParticipanteIdentidadId == participante.ParticipanteIdentidadId))
            throw new ParticipacionInvalidaExcepcion(
                "El participante ya forma parte de este equipo.");

        _participantes.Add(participante);
    }

    public bool ContieneParticipanteIdentidadId(Guid participanteIdentidadId)
        => _participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId);

    public bool EstaLleno() => _participantes.Count >= CapacidadMaxima;

    public void SumarPuntaje(int puntos)
    {
        if (puntos < 0)
            throw new EquipoInvalidoExcepcion("El puntaje a sumar no puede ser negativo.");
        Puntaje += puntos;
    }

    public static Equipo Rehidratar(
        Guid id, Guid sesionId, string nombre,
        Guid liderParticipanteId, int puntaje,
        TipoEquipo tipo, string? contrasenaHash, int capacidadMaxima,
        DateTime fechaCreacion,
        IEnumerable<Participante>? integrantes = null)
    {
        var equipo = new Equipo
        {
            Id = id,
            SesionId = sesionId,
            Nombre = NombreEquipo.Crear(nombre),
            LiderParticipanteId = liderParticipanteId,
            Puntaje = puntaje,
            Tipo = tipo,
            ContrasenaHash = tipo == TipoEquipo.Privado && !string.IsNullOrWhiteSpace(contrasenaHash)
                ? ContrasenaEquipoHash.Crear(contrasenaHash)
                : null,
            CapacidadMaxima = capacidadMaxima,
            FechaCreacion = fechaCreacion
        };
        if (integrantes is not null) equipo._participantes.AddRange(integrantes);
        return equipo;
    }

    private static void ValidarConsistenciaTipoContrasena(
        TipoEquipo tipo, ContrasenaEquipoHash? contrasenaHash)
    {
        if (tipo == TipoEquipo.Privado && contrasenaHash is null)
            throw new EquipoInvalidoExcepcion(
                "Un equipo privado debe tener una contraseña configurada.");
    }
}
