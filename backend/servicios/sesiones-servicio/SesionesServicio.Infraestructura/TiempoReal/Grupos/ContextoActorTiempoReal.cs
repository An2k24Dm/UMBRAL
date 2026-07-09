namespace SesionesServicio.Infraestructura.TiempoReal.Grupos;

public sealed record ContextoActorTiempoReal(
    Guid? UsuarioId,
    IReadOnlyCollection<string> Roles,
    string? NombreUsuario)
{
    public const string RolAdministrador = "Administrador";
    public const string RolOperador = "Operador";
    public const string RolParticipante = "Participante";
    public bool EsAdministrador => Roles.Contains(RolAdministrador);
    public bool EsOperador => Roles.Contains(RolOperador);
    public bool EsParticipante => Roles.Contains(RolParticipante);

    public bool TieneRolReconocido() => EsAdministrador || EsOperador || EsParticipante;

    public string RolPrincipal() =>
        EsAdministrador ? RolAdministrador
        : EsOperador ? RolOperador
        : EsParticipante ? RolParticipante
        : "Desconocido";
}
