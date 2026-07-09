namespace SesionesServicio.Dominio.Excepciones;

// Se lanza cuando ya existe una respuesta oficial para la combinación
// (Sesión, Etapa, Pregunta, Jugador). En sesión individual el jugador es el
// participante; en sesión grupal el jugador es el equipo, por lo que basta con
// que un integrante haya respondido primero. La primera respuesta persistida
// gana y no se sustituye.
//
// La bandera EsEquipo permite al manejo global de errores distinguir el
// mensaje/código para el cliente (respuesta grupal vs. individual).
public sealed class RespuestaTriviaDuplicadaExcepcion : Exception
{
    public bool EsEquipo { get; }

    public RespuestaTriviaDuplicadaExcepcion(bool esEquipo)
        : base(esEquipo
            ? "Otro integrante de tu equipo ya respondió esta pregunta."
            : "Ya respondiste esta pregunta.")
    {
        EsEquipo = esEquipo;
    }
}
