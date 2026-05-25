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

    // Consulta optimizada: proyección directa a DTO sin cargar el modelo de dominio.
    public async Task<List<TriviaResumenDto>> ObtenerTriviasEnBorradorAsync(
        Guid creadorId, CancellationToken cancelacion)
    {
        var estadoBorrador = (int)EstadoTrivia.Borrador;

        return await _contexto.Trivias
            .AsNoTracking()
            .Where(t => t.CreadorId == creadorId && t.Estado == estadoBorrador)
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
}
