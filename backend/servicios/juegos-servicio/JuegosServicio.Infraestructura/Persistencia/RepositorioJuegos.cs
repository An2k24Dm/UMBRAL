using System.Text.Json;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Infraestructura.Persistencia.Modelos;
using Microsoft.EntityFrameworkCore;

namespace JuegosServicio.Infraestructura.Persistencia;

public sealed class RepositorioJuegos : IRepositorioJuegos
{
    private readonly ContextoJuegos _contexto;

    public RepositorioJuegos(ContextoJuegos contexto)
    {
        _contexto = contexto;
    }

    public async Task<bool> ExisteTriviaConNombreAsync(string nombre, CancellationToken cancelacion)
    {
        var normalizado = nombre.Trim();
        return await _contexto.Trivias.AsNoTracking()
            .AnyAsync(t => t.Nombre == normalizado, cancelacion);
    }

    public async Task AgregarTriviaAsync(Trivia trivia, CancellationToken cancelacion)
    {
        var modelo = JuegosMapeador.AModelo(trivia);
        _contexto.Trivias.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task<Trivia?> ObtenerTriviaPorIdAsync(Guid triviaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Trivias
            .Include(t => t.Preguntas)
                .ThenInclude(p => p.Opciones)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == triviaId, cancelacion);

        return modelo is null ? null : JuegosMapeador.ADominio(modelo);
    }

    public async Task AgregarPreguntaAsync(Guid triviaId, Pregunta pregunta, CancellationToken cancelacion)
    {
        var modelo = JuegosMapeador.AModelo(pregunta);
        _contexto.Preguntas.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ModificarPreguntaAsync(Guid triviaId, Pregunta pregunta, CancellationToken cancelacion)
    {
        // Eliminar opciones anteriores y reemplazar con las nuevas.
        var opcionesAnteriores = await _contexto.Opciones
            .Where(o => o.PreguntaId == pregunta.Id)
            .ToListAsync(cancelacion);

        _contexto.Opciones.RemoveRange(opcionesAnteriores);

        var nuevasOpciones = pregunta.Opciones.Select(JuegosMapeador.AModelo).ToList();
        _contexto.Opciones.AddRange(nuevasOpciones);

        var modeloPregunta = await _contexto.Preguntas
            .FirstOrDefaultAsync(p => p.Id == pregunta.Id, cancelacion);

        if (modeloPregunta is not null)
        {
            modeloPregunta.Enunciado = pregunta.Enunciado;
            modeloPregunta.PuntajeAsignado = pregunta.PuntajeAsignado;
        }

        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task EliminarPreguntaAsync(Guid triviaId, Guid preguntaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Preguntas
            .FirstOrDefaultAsync(p => p.Id == preguntaId && p.TriviaId == triviaId, cancelacion);

        if (modelo is not null)
        {
            _contexto.Preguntas.Remove(modelo);
            await _contexto.SaveChangesAsync(cancelacion);
        }
    }

    public async Task<TriviaDetalleDto?> ObtenerDetalleTriviaAsync(
        Guid triviaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Trivias
            .AsNoTracking()
            .Include(t => t.Preguntas)
                .ThenInclude(p => p.Opciones)
            .FirstOrDefaultAsync(t => t.Id == triviaId, cancelacion);

        if (modelo is null) return null;

        return new TriviaDetalleDto
        {
            Id = modelo.Id,
            Nombre = modelo.Nombre,
            Descripcion = modelo.Descripcion,
            TiempoLimitePorPregunta = modelo.TiempoLimitePorPregunta,
            Estado = ((EstadoTrivia)modelo.Estado).ToString(),
            FechaCreacion = modelo.FechaCreacion,
            Preguntas = modelo.Preguntas.Select(p => new PreguntaDetalleDto
            {
                Id = p.Id,
                Enunciado = p.Enunciado,
                PuntajeAsignado = p.PuntajeAsignado,
                Opciones = p.Opciones.Select(o => new OpcionDetalleDto
                {
                    Id = o.Id,
                    Texto = o.Texto,
                    EsCorrecta = o.EsCorrecta
                }).ToList()
            }).ToList()
        };
    }

    public async Task ActivarTriviaAsync(Trivia trivia, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Trivias.FirstOrDefaultAsync(t => t.Id == trivia.Id, cancelacion);
        if (modelo is null) return;

        modelo.Estado = (int)EstadoTrivia.Activa;

        _contexto.EventosSalida.Add(new EventoSalidaModelo
        {
            Id = Guid.NewGuid(),
            Tipo = "TriviaActivada",
            Datos = JsonSerializer.Serialize(new
            {
                TriviaId = trivia.Id,
                trivia.Nombre,
                CantidadPreguntas = trivia.Preguntas.Count
            }),
            FechaCreacion = DateTime.UtcNow,
            Procesado = false
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ModificarDatosTriviaAsync(Trivia trivia, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Trivias.FirstOrDefaultAsync(t => t.Id == trivia.Id, cancelacion);
        if (modelo is null) return;

        modelo.Nombre = trivia.Nombre;
        modelo.Descripcion = trivia.Descripcion;
        modelo.TiempoLimitePorPregunta = trivia.TiempoLimitePorPregunta;

        _contexto.EventosSalida.Add(new EventoSalidaModelo
        {
            Id = Guid.NewGuid(),
            Tipo = "TriviaModificada",
            Datos = JsonSerializer.Serialize(new
            {
                TriviaId = trivia.Id,
                trivia.Nombre,
                trivia.TiempoLimitePorPregunta
            }),
            FechaCreacion = DateTime.UtcNow,
            Procesado = false
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ArchivarTriviaAsync(Trivia trivia, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Trivias.FirstOrDefaultAsync(t => t.Id == trivia.Id, cancelacion);
        if (modelo is null) return;

        modelo.Estado = (int)EstadoTrivia.Archivada;

        _contexto.EventosSalida.Add(new EventoSalidaModelo
        {
            Id = Guid.NewGuid(),
            Tipo = "TriviaArchivada",
            Datos = JsonSerializer.Serialize(new { TriviaId = trivia.Id }),
            FechaCreacion = DateTime.UtcNow,
            Procesado = false
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }

    // Consulta optimizada: proyección directa a DTO sin cargar el modelo de dominio.
    public async Task<List<TriviaResumenDto>> ObtenerTriviasEnBorradorAsync(
        Guid? creadorId, CancellationToken cancelacion)
    {
        var estadoBorrador = (int)EstadoTrivia.Borrador;

        return await _contexto.Trivias
            .AsNoTracking()
            .Where(t => (creadorId == null || t.CreadorId == creadorId) && t.Estado == estadoBorrador)
            .OrderByDescending(t => t.FechaCreacion)
            .Select(t => new TriviaResumenDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Descripcion = t.Descripcion,
                TiempoLimitePorPregunta = t.TiempoLimitePorPregunta,
                Estado = nameof(EstadoTrivia.Borrador),
                TotalPreguntas = t.Preguntas.Count,
                FechaCreacion = t.FechaCreacion
            })
            .ToListAsync(cancelacion);
    }

    public async Task<List<TriviaResumenDto>> ObtenerTriviasActivasAsync(CancellationToken cancelacion)
    {
        var estadoActiva = (int)EstadoTrivia.Activa;

        return await _contexto.Trivias
            .AsNoTracking()
            .Where(t => t.Estado == estadoActiva)
            .OrderBy(t => t.Nombre)
            .Select(t => new TriviaResumenDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Descripcion = t.Descripcion,
                TiempoLimitePorPregunta = t.TiempoLimitePorPregunta,
                Estado = nameof(EstadoTrivia.Activa),
                TotalPreguntas = t.Preguntas.Count,
                FechaCreacion = t.FechaCreacion
            })
            .ToListAsync(cancelacion);
    }
}
