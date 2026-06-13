using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

// Coordinador de mapeo dominio ↔ persistencia. Arma la parte común del
// SesionModelo/agregado y delega la parte específica del tipo en la
// estrategia compatible (seleccionada por el discriminador `tipo_sesion`).
// Sin switch/if por tipo concreto.
public sealed class MapeadorSesionesPersistencia
{
    private readonly IEnumerable<IMapeadorPersistenciaSesion> _mapeadores;

    public MapeadorSesionesPersistencia(IEnumerable<IMapeadorPersistenciaSesion> mapeadores)
    {
        _mapeadores = mapeadores;
    }

    public SesionModelo HaciaModelo(Sesion sesion)
    {
        var modelo = new SesionModelo
        {
            Id = sesion.Id,
            TipoSesion = sesion.TipoSesion,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Estado = sesion.Estado,
            FechaProgramada = sesion.FechaProgramada,
            CodigoAcceso = sesion.CodigoAcceso,
            OperadorCreadorId = sesion.OperadorCreadorId,
            FechaCreacion = sesion.FechaCreacion,
            FechaInicioUtc = sesion.FechaInicioUtc,
            FechaFinalizacionUtc = sesion.FechaFinalizacionUtc,
            Misiones = sesion.Misiones.Select(m => new SesionMisionModelo
            {
                Id = m.Id,
                SesionId = m.SesionId,
                MisionId = m.MisionId,
                Orden = m.Orden
            }).ToList()
        };

        Seleccionar(sesion.TipoSesion).CompletarModelo(sesion, modelo);
        return modelo;
    }

    public Sesion HaciaDominio(SesionModelo modelo)
    {
        var misiones = modelo.Misiones
            .Select(m => SesionMision.Rehidratar(m.Id, m.SesionId, m.MisionId, m.Orden))
            .ToList();

        return Seleccionar(modelo.TipoSesion).HaciaDominio(modelo, misiones);
    }

    private IMapeadorPersistenciaSesion Seleccionar(string tipoSesion)
        => _mapeadores.FirstOrDefault(m => m.Soporta(tipoSesion))
           ?? throw new SesionInvalidaExcepcion(
               $"No existe un mapeador de persistencia para el tipo de sesión '{tipoSesion}'.");
}
