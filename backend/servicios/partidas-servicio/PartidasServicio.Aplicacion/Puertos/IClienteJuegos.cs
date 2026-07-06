namespace PartidasServicio.Aplicacion.Puertos;

public interface IClienteJuegos
{
    Task<VerificacionRespuestaDto?> VerificarRespuestaAsync(
        Guid triviaId, Guid preguntaId, Guid opcionId, CancellationToken cancelacion);
}

public sealed class VerificacionRespuestaDto
{
    public bool EsCorrecta { get; set; }
    public int PuntajeBase { get; set; }
    public int TiempoLimiteMs { get; set; }
}
