namespace IdentidadServicio.Commons.Dtos;

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
