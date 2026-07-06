namespace PartidasServicio.Aplicacion.Puertos;

public interface IClienteSesiones
{
    Task<InfoPartidaSesionDto?> ObtenerInfoPartidaAsync(Guid sesionId, CancellationToken cancelacion);
}

public sealed class InfoPartidaSesionDto
{
    public string Estado { get; set; } = string.Empty;
    public bool ParticipanteInscrito { get; set; }
    public Guid? EquipoId { get; set; }
}
