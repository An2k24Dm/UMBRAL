using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Infraestructura.Persistencia.Modelos;

namespace JuegosServicio.Infraestructura.Persistencia;

public static class JuegosMapeador
{
    // Dominio → Modelos

    public static TriviaModelo AModelo(Trivia trivia)
    {
        return new TriviaModelo
        {
            Id = trivia.Id,
            Nombre = trivia.Nombre,
            Descripcion = trivia.Descripcion,
            CreadorId = trivia.CreadorId,
            TiempoLimitePorPregunta = trivia.TiempoLimitePorPregunta,
            Estado = (int)trivia.Estado,
            FechaCreacion = trivia.FechaCreacion,
            Preguntas = trivia.Preguntas.Select(AModelo).ToList()
        };
    }

    public static PreguntaModelo AModelo(Pregunta pregunta)
    {
        return new PreguntaModelo
        {
            Id = pregunta.Id,
            TriviaId = pregunta.TriviaId,
            Enunciado = pregunta.Enunciado,
            PuntajeAsignado = pregunta.PuntajeAsignado,
            TiempoEstimado = pregunta.TiempoEstimado,
            Opciones = pregunta.Opciones.Select(AModelo).ToList()
        };
    }

    public static OpcionModelo AModelo(Opcion opcion)
    {
        return new OpcionModelo
        {
            Id = opcion.Id,
            PreguntaId = opcion.PreguntaId,
            Texto = opcion.Texto,
            EsCorrecta = opcion.EsCorrecta
        };
    }

    // Modelos → Dominio

    public static Trivia ADominio(TriviaModelo modelo)
    {
        var preguntas = modelo.Preguntas.Select(ADominio);
        return Trivia.Reconstituir(
            modelo.Id,
            modelo.Nombre,
            modelo.Descripcion,
            modelo.CreadorId,
            modelo.TiempoLimitePorPregunta,
            (EstadoTrivia)modelo.Estado,
            modelo.FechaCreacion,
            preguntas);
    }

    public static Pregunta ADominio(PreguntaModelo modelo)
    {
        var opciones = modelo.Opciones.Select(ADominio);
        return Pregunta.Reconstituir(
            modelo.Id,
            modelo.TriviaId,
            modelo.Enunciado,
            modelo.PuntajeAsignado,
            modelo.TiempoEstimado,
            opciones);
    }

    public static Opcion ADominio(OpcionModelo modelo)
    {
        return Opcion.Reconstituir(modelo.Id, modelo.PreguntaId, modelo.Texto, modelo.EsCorrecta);
    }
}
