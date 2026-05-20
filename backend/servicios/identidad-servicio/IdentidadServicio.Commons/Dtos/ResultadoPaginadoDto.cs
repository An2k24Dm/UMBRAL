namespace IdentidadServicio.Commons.Dtos;

// Resultado paginado genérico para consultas que entregan listados.
// Se mantiene neutral respecto al dominio para poder reutilizarse en HU07/HU08.
public sealed class ResultadoPaginadoDto<T>
{
    public IReadOnlyList<T> Elementos { get; init; } = Array.Empty<T>();
    public int Pagina { get; init; }
    public int TamanioPagina { get; init; }
    public int Total { get; init; }
}
