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
    public PuntajeSesion Puntaje { get; private set; } = null!;
    public int PuntosPenalizados { get; private set; }
    public DateTime? SnapshotRankingUtc { get; private set; }
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
            Puntaje = PuntajeSesion.Cero(),
            SnapshotRankingUtc = null,
            Tipo = tipo,
            ContrasenaHash = tipo == TipoEquipo.Privado ? contrasenaHash : null,
            CapacidadMaxima = capacidadMaxima,
            FechaCreacion = fechaCreacionUtc
        };
        equipo._participantes.Add(lider);
        equipo.RecalcularPuntajeDesdeParticipantes();
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
        RecalcularPuntajeDesdeParticipantes();
    }

    internal void ModificarDatos(
        NombreEquipo nuevoNombre,
        TipoEquipo nuevoTipo,
        ContrasenaEquipoHash? nuevaContrasenaHash,
        bool actualizarContrasena)
    {
        if (nuevoNombre is null)
            throw new EquipoInvalidoExcepcion("El nombre del equipo es obligatorio.");

        if (nuevoTipo == TipoEquipo.Publico)
        {
            // Un equipo público nunca conserva contraseña.
            Nombre = nuevoNombre;
            Tipo = TipoEquipo.Publico;
            ContrasenaHash = null;
            return;
        }

        if (actualizarContrasena)
        {
            if (nuevaContrasenaHash is null)
                throw new EquipoInvalidoExcepcion(
                    "Un equipo privado debe tener una contraseña configurada.");
            ContrasenaHash = nuevaContrasenaHash;
        }
        else if (ContrasenaHash is null)
        {
            throw new EquipoInvalidoExcepcion(
                "Un equipo privado debe tener una contraseña configurada.");
        }

        Nombre = nuevoNombre;
        Tipo = TipoEquipo.Privado;
    }

    internal Participante ExpulsarParticipante(
        Guid participanteSesionId, bool permitirExpulsarLider)
    {
        var participante = _participantes.FirstOrDefault(p => p.Id == participanteSesionId)
            ?? throw new ParticipanteNoEncontradoExcepcion(
                "El participante indicado no pertenece a este equipo.");

        if (participante.Id == LiderParticipanteId)
        {
            if (!permitirExpulsarLider)
                throw new EquipoInvalidoExcepcion(
                    "No puedes expulsar al líder del equipo.");

            var siguiente = _participantes
                .Where(p => p.Id != LiderParticipanteId)
                .OrderBy(p => p.FechaUnionEquipo ?? DateTime.MaxValue)
                .FirstOrDefault()
                ?? throw new EquipoInvalidoExcepcion(
                    "No se puede expulsar al único integrante del equipo. " +
                    "Expulsa el equipo completo.");

            LiderParticipanteId = siguiente.Id;
        }

        _participantes.Remove(participante);
        RecalcularPuntajeDesdeParticipantes();
        return participante;
    }

    // HU48 — Abandono voluntario. Se busca por identidad (el actor es el
    // propio participante). Si abandona el líder y quedan integrantes, el
    // liderazgo pasa al más antiguo; si era el único, el equipo queda vacío y
    // la sesión lo elimina (nunca queda un equipo con integrantes sin líder).
    internal Participante AbandonarParticipante(Guid participanteIdentidadId)
    {
        var participante = _participantes.FirstOrDefault(
                p => p.ParticipanteIdentidadId == participanteIdentidadId)
            ?? throw new ParticipanteNoEncontradoExcepcion(
                "El participante no pertenece a este equipo.");

        if (participante.Id == LiderParticipanteId)
        {
            var siguiente = _participantes
                .Where(p => p.Id != LiderParticipanteId)
                .OrderBy(p => p.FechaUnionEquipo ?? DateTime.MaxValue)
                .FirstOrDefault();

            if (siguiente is not null)
                LiderParticipanteId = siguiente.Id;
        }

        _participantes.Remove(participante);
        RecalcularPuntajeDesdeParticipantes();
        return participante;
    }

    public bool ContieneParticipanteIdentidadId(Guid participanteIdentidadId)
        => _participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId);


    public bool EsLider(Guid participanteIdentidadId)
    {
        var lider = _participantes.FirstOrDefault(p => p.Id == LiderParticipanteId);
        return lider is not null && lider.ParticipanteIdentidadId == participanteIdentidadId;
    }

    public bool EstaLleno() => _participantes.Count >= CapacidadMaxima;

    // El puntaje del equipo es derivado: siempre es la suma de los puntajes
    // de sus integrantes. Por eso no existe un SumarPuntaje directo sobre el
    // equipo; los puntos se suman al participante y el total se recalcula.
    public void SumarPuntajeAParticipante(Guid participanteSesionId, PuntajeSesion puntos)
    {
        var participante = _participantes.FirstOrDefault(p => p.Id == participanteSesionId)
            ?? throw new ParticipanteNoEncontradoExcepcion(
                "El participante indicado no pertenece a este equipo.");

        participante.SumarPuntaje(puntos);
        RecalcularPuntajeDesdeParticipantes();
    }

    public void SumarPuntajeAParticipante(Guid participanteSesionId, int puntos)
        => SumarPuntajeAParticipante(participanteSesionId, PuntajeSesion.Crear(puntos));

    public void EstablecerPuntajeSnapshotParticipante(
        Guid participanteSesionId,
        int puntajeParticipante,
        int? puntajeEquipo)
    {
        var participante = _participantes.FirstOrDefault(p => p.Id == participanteSesionId)
            ?? throw new ParticipanteNoEncontradoExcepcion(
                "El participante indicado no pertenece a este equipo.");

        participante.EstablecerPuntajeSnapshot(puntajeParticipante);
        Puntaje = puntajeEquipo.HasValue
            ? PuntajeSesion.DesdePersistencia(puntajeEquipo.Value)
            : _participantes.Aggregate(PuntajeSesion.Cero(), (total, p) => total.Sumar(p.Puntaje));
    }

    public bool EstablecerPuntajeSnapshotParticipante(
        Guid participanteSesionId,
        int puntajeParticipante,
        int? puntajeEquipo,
        DateTime calculadoEnUtc)
    {
        var participante = _participantes.FirstOrDefault(p => p.Id == participanteSesionId)
            ?? throw new ParticipanteNoEncontradoExcepcion(
                "El participante indicado no pertenece a este equipo.");

        var participanteActualizado = participante.EstablecerPuntajeSnapshot(
            puntajeParticipante, calculadoEnUtc);
        var equipoActualizado = !SnapshotRankingUtc.HasValue
            || calculadoEnUtc > SnapshotRankingUtc.Value;

        if (equipoActualizado)
        {
            Puntaje = puntajeEquipo.HasValue
                ? PuntajeSesion.DesdePersistencia(puntajeEquipo.Value)
                : _participantes.Aggregate(PuntajeSesion.Cero(), (total, p) => total.Sumar(p.Puntaje));
            SnapshotRankingUtc = calculadoEnUtc;
        }

        return participanteActualizado || equipoActualizado;
    }

    public bool EstablecerPenalizacionSnapshot(
        int puntosPenalizados, int puntajeEquipo, DateTime calculadoEnUtc)
    {
        if (SnapshotRankingUtc.HasValue && calculadoEnUtc <= SnapshotRankingUtc.Value)
            return false;

        PuntosPenalizados = puntosPenalizados;
        Puntaje = PuntajeSesion.DesdePersistencia(puntajeEquipo);
        SnapshotRankingUtc = calculadoEnUtc;
        return true;
    }

    private void RecalcularPuntajeDesdeParticipantes()
    {
        var total = PuntajeSesion.Cero();
        foreach (var participante in _participantes)
            total = total.Sumar(participante.Puntaje);
        Puntaje = total;
    }

    public static Equipo Rehidratar(
        Guid id, Guid sesionId, string nombre,
        Guid liderParticipanteId, int puntaje,
        TipoEquipo tipo, string? contrasenaHash, int capacidadMaxima,
        DateTime fechaCreacion,
        IEnumerable<Participante>? integrantes = null,
        DateTime? snapshotRankingUtc = null,
        int puntosPenalizados = 0)
    {
        var equipo = new Equipo
        {
            Id = id,
            SesionId = sesionId,
            Nombre = NombreEquipo.Crear(nombre),
            LiderParticipanteId = liderParticipanteId,
            Puntaje = PuntajeSesion.DesdePersistencia(puntaje),
            PuntosPenalizados = puntosPenalizados,
            SnapshotRankingUtc = snapshotRankingUtc,
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
