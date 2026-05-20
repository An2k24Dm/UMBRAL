namespace IdentidadServicio.Commons.Dtos;

// Sobre genérico para listados paginados. Se mantiene en Commons para que
// pueda viajar entre la capa de Aplicación y los controladores sin acoplarlos
// a tipos específicos del dominio.
public sealed class ResultadoPaginadoDto<T>
{
    public IReadOnlyList<T> Elementos { get; init; } = Array.Empty<T>();
    public int Pagina { get; init; }
    public int TamanioPagina { get; init; }
    public int Total { get; init; }

    public ResultadoPaginadoDto() { }

    public ResultadoPaginadoDto(IReadOnlyList<T> elementos, int pagina, int tamanioPagina, int total)
    {
        Elementos = elementos;
        Pagina = pagina;
        TamanioPagina = tamanioPagina;
        Total = total;
    }
}
