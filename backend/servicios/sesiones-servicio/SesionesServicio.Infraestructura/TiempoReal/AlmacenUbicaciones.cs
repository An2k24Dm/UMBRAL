using System.Collections.Concurrent;

namespace SesionesServicio.Infraestructura.TiempoReal;

public sealed record UbicacionRegistro(
    Guid ParticipanteIdentidadId,
    string Nombre,
    Guid? EquipoId,
    double Latitud,
    double Longitud,
    DateTime FechaUtc);

public interface IAlmacenUbicaciones
{
    void Actualizar(Guid sesionId, Guid participanteId, string nombre, Guid? equipoId, double latitud, double longitud);
    IReadOnlyList<UbicacionRegistro> ObtenerPorSesion(Guid sesionId);
    void Eliminar(Guid sesionId, Guid participanteId);
}

public sealed class AlmacenUbicaciones : IAlmacenUbicaciones
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, UbicacionRegistro>> _datos = new();

    public void Actualizar(Guid sesionId, Guid participanteId, string nombre, Guid? equipoId, double latitud, double longitud)
    {
        var sesion = _datos.GetOrAdd(sesionId, _ => new ConcurrentDictionary<Guid, UbicacionRegistro>());
        sesion[participanteId] = new UbicacionRegistro(participanteId, nombre, equipoId, latitud, longitud, DateTime.UtcNow);
    }

    public IReadOnlyList<UbicacionRegistro> ObtenerPorSesion(Guid sesionId)
        => _datos.TryGetValue(sesionId, out var sesion)
            ? sesion.Values.ToList()
            : [];

    public void Eliminar(Guid sesionId, Guid participanteId)
    {
        if (_datos.TryGetValue(sesionId, out var sesion))
            sesion.TryRemove(participanteId, out _);
    }
}
