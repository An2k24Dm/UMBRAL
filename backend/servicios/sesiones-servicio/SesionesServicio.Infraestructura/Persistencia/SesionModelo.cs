using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Infraestructura.Persistencia;

// Modelo de persistencia: refleja la fila de la tabla "Sesion" en
// PostgreSQL. Se mantiene separado del agregado de dominio para no
// acoplar las reglas de negocio a EF Core; el mapeo entre uno y otro
// vive en SesionesMapeador / ConfiguracionMapsterSesiones.
//
// La columna creada_por_rol NO existe a propósito: el rol del creador
// es información de identidad y se consulta en línea a
// identidad-servicio cuando la regla de visibilidad lo necesita.
public sealed class SesionModelo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoJuego TipoJuego { get; set; }
    public Guid ContenidoJuegoId { get; set; }
    public ModoSesion Modo { get; set; }
    public EstadoSesion Estado { get; set; }
    public DateTime FechaProgramada { get; set; }
    public Guid CreadaPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; }
}
