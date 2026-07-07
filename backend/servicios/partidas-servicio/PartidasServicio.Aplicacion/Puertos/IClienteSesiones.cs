namespace PartidasServicio.Aplicacion.Puertos;

public interface IClienteSesiones
{
    Task<InfoPartidaSesionDto?> ObtenerInfoPartidaAsync(Guid sesionId, CancellationToken cancelacion);
    Task<NombresRankingClienteDto?> ObtenerNombresRankingAsync(Guid sesionId, CancellationToken cancelacion);
}

public sealed class InfoPartidaSesionDto
{
    public string Estado { get; set; } = string.Empty;
    public bool ParticipanteInscrito { get; set; }
    public Guid? EquipoId { get; set; }
}

public sealed class NombresRankingClienteDto
{
    public List<NombreEquipoClienteDto> Equipos { get; set; } = new();
    public List<NombreParticipanteClienteDto> Participantes { get; set; } = new();
}

public sealed class NombreEquipoClienteDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class NombreParticipanteClienteDto
{
    public Guid IdentidadId { get; set; }
    public string Alias { get; set; } = string.Empty;
}
