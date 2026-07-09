namespace SesionesServicio.Dominio.Excepciones;

// Se lanza cuando ya existe una evidencia válida para la combinación
// (Sesión, Etapa, Jugador) en una Búsqueda del Tesoro. En sesión individual el
// jugador es el participante; en sesión grupal el jugador es el equipo, por lo
// que basta con que un integrante haya encontrado el QR primero. La primera
// evidencia válida persistida gana y no se sustituye.
//
// La bandera EsEquipo permite al manejo global de errores diferenciar el
// código/mensaje para el cliente (equipo ya completó vs. participante ya completó).
public sealed class EvidenciaTesoroDuplicadaExcepcion : Exception
{
    public bool EsEquipo { get; }

    public EvidenciaTesoroDuplicadaExcepcion(bool esEquipo)
        : base(esEquipo
            ? "Tu equipo ya completó esta etapa."
            : "Ya completaste esta etapa.")
    {
        EsEquipo = esEquipo;
    }
}
