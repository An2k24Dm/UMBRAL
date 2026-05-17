using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.ObjetosDeValor;
using Mapster;

namespace IdentidadServicio.Infraestructura.Persistencia;

// Mapeo dominio ↔ persistencia con Mapster.
// El dominio NO conoce IdKeycloak: viaja como parámetro al mapear hacia los
// modelos. Las VOs NombreUsuario/Correo/NombrePersona/DatosContacto se
// desempaquetan vía .Map(...) explícitos.
public sealed class IdentidadMapeador
{
    public sealed record ModelosAdministrador(
        UsuarioModelo Usuario, PersonaModelo Persona, AdministradorModelo Administrador);
    public sealed record ModelosOperador(
        UsuarioModelo Usuario, PersonaModelo Persona, OperadorModelo Operador);
    public sealed record ModelosParticipante(
        UsuarioModelo Usuario, PersonaModelo Persona, ParticipanteModelo Participante);

    private sealed record TripletaAdmin(
        UsuarioModelo Usuario, PersonaModelo Persona, AdministradorModelo Administrador);
    private sealed record TripletaOperador(
        UsuarioModelo Usuario, PersonaModelo Persona, OperadorModelo Operador);
    private sealed record TripletaParticipante(
        UsuarioModelo Usuario, PersonaModelo Persona, ParticipanteModelo Participante);

    private readonly TypeAdapterConfig _config;

    public IdentidadMapeador()
    {
        _config = new TypeAdapterConfig();

        ConfigurarMapeoDominioAModelos<Administrador>();
        ConfigurarMapeoDominioAModelos<Operador>();
        ConfigurarMapeoDominioAModelos<Participante>();

        _config.NewConfig<Administrador, AdministradorModelo>()
            .Map(d => d.Id, _ => Guid.NewGuid())
            .Map(d => d.CodigoAdministrador, s => s.CodigoAdministrador)
            .Map(d => d.FechaRegistro, s => s.FechaRegistro)
            .Ignore(d => d.PersonaId)
            .Ignore(d => d.Persona!);

        _config.NewConfig<Operador, OperadorModelo>()
            .Map(d => d.Id, _ => Guid.NewGuid())
            .Map(d => d.CodigoOperador, s => s.CodigoOperador)
            .Map(d => d.FechaRegistro, s => s.FechaRegistro)
            .Ignore(d => d.PersonaId)
            .Ignore(d => d.Persona!);

        _config.NewConfig<Participante, ParticipanteModelo>()
            .Map(d => d.Id, _ => Guid.NewGuid())
            .Map(d => d.Alias, s => s.Alias)
            .Map(d => d.FechaRegistro, s => s.FechaRegistro)
            .Ignore(d => d.PersonaId)
            .Ignore(d => d.Persona!);

        // Reverso vía ConstructUsing.
        _config.NewConfig<TripletaAdmin, Administrador>()
            .MapToConstructor(true)
            .ConstructUsing(t => new Administrador(
                t.Usuario.Id,
                NombreUsuario.Crear(t.Usuario.NombreUsuario),
                Correo.Crear(t.Persona.Correo ?? string.Empty),
                (EstadoUsuario)t.Usuario.Estado,
                t.Usuario.FechaRegistro,
                NombrePersona.Crear(t.Persona.Nombre, t.Persona.Apellido),
                DatosContacto.Crear(t.Persona.Direccion, t.Persona.Telefono),
                (SexoPersona)t.Persona.Sexo,
                t.Persona.FechaNacimiento,
                t.Administrador.CodigoAdministrador));

        _config.NewConfig<TripletaOperador, Operador>()
            .MapToConstructor(true)
            .ConstructUsing(t => new Operador(
                t.Usuario.Id,
                NombreUsuario.Crear(t.Usuario.NombreUsuario),
                Correo.Crear(t.Persona.Correo ?? string.Empty),
                (EstadoUsuario)t.Usuario.Estado,
                t.Usuario.FechaRegistro,
                NombrePersona.Crear(t.Persona.Nombre, t.Persona.Apellido),
                DatosContacto.Crear(t.Persona.Direccion, t.Persona.Telefono),
                (SexoPersona)t.Persona.Sexo,
                t.Persona.FechaNacimiento,
                t.Operador.CodigoOperador));

        _config.NewConfig<TripletaParticipante, Participante>()
            .MapToConstructor(true)
            .ConstructUsing(t => new Participante(
                t.Usuario.Id,
                NombreUsuario.Crear(t.Usuario.NombreUsuario),
                Correo.Crear(t.Persona.Correo ?? string.Empty),
                (EstadoUsuario)t.Usuario.Estado,
                t.Usuario.FechaRegistro,
                NombrePersona.Crear(t.Persona.Nombre, t.Persona.Apellido),
                DatosContacto.Crear(t.Persona.Direccion, t.Persona.Telefono),
                (SexoPersona)t.Persona.Sexo,
                t.Persona.FechaNacimiento,
                t.Participante.Alias));
    }

    private void ConfigurarMapeoDominioAModelos<T>() where T : Usuario
    {
        _config.NewConfig<T, UsuarioModelo>()
            .Map(d => d.Id, s => s.Id == Guid.Empty ? Guid.NewGuid() : s.Id)
            .Map(d => d.NombreUsuario, s => s.NombreUsuario.Valor)
            .Map(d => d.Rol, s => (int)s.Rol)
            .Map(d => d.Estado, s => (int)s.Estado)
            .Map(d => d.FechaRegistro, s => s.FechaRegistro)
            .Ignore(d => d.IdKeycloak)
            .Ignore(d => d.Persona!);

        _config.NewConfig<T, PersonaModelo>()
            .Map(d => d.Id, _ => Guid.NewGuid())
            .Map(d => d.UsuarioId, s => s.Id)
            .Map(d => d.Nombre, s => s.NombrePersona.Nombre)
            .Map(d => d.Apellido, s => s.NombrePersona.Apellido)
            .Map(d => d.Correo, s => s.Correo.Valor)
            .Map(d => d.Direccion, s => s.DatosContacto.Direccion)
            .Map(d => d.Telefono, s => s.DatosContacto.Telefono)
            .Map(d => d.Sexo, s => (int)s.Sexo)
            .Map(d => d.FechaNacimiento, s => s.FechaNacimiento)
            .Map(d => d.FechaRegistro, s => s.FechaRegistro)
            .Ignore(d => d.Usuario!)
            .Ignore(d => d.Administrador!)
            .Ignore(d => d.Operador!)
            .Ignore(d => d.Participante!);
    }

    public ModelosAdministrador AModelos(Administrador admin, string idKeycloak)
    {
        var usuario = admin.Adapt<UsuarioModelo>(_config);
        usuario.IdKeycloak = idKeycloak;
        var persona = admin.Adapt<PersonaModelo>(_config);
        persona.UsuarioId = usuario.Id;
        var administrador = admin.Adapt<AdministradorModelo>(_config);
        administrador.PersonaId = persona.Id;
        return new ModelosAdministrador(usuario, persona, administrador);
    }

    public ModelosOperador AModelos(Operador op, string idKeycloak)
    {
        var usuario = op.Adapt<UsuarioModelo>(_config);
        usuario.IdKeycloak = idKeycloak;
        var persona = op.Adapt<PersonaModelo>(_config);
        persona.UsuarioId = usuario.Id;
        var operador = op.Adapt<OperadorModelo>(_config);
        operador.PersonaId = persona.Id;
        return new ModelosOperador(usuario, persona, operador);
    }

    public ModelosParticipante AModelos(Participante par, string idKeycloak)
    {
        var usuario = par.Adapt<UsuarioModelo>(_config);
        usuario.IdKeycloak = idKeycloak;
        var persona = par.Adapt<PersonaModelo>(_config);
        persona.UsuarioId = usuario.Id;
        var participante = par.Adapt<ParticipanteModelo>(_config);
        participante.PersonaId = persona.Id;
        return new ModelosParticipante(usuario, persona, participante);
    }

    public Administrador AAdministrador(UsuarioModelo u, PersonaModelo p, AdministradorModelo a)
        => new TripletaAdmin(u, p, a).Adapt<Administrador>(_config);

    public Operador AOperador(UsuarioModelo u, PersonaModelo p, OperadorModelo o)
        => new TripletaOperador(u, p, o).Adapt<Operador>(_config);

    public Participante AParticipante(UsuarioModelo u, PersonaModelo p, ParticipanteModelo par)
        => new TripletaParticipante(u, p, par).Adapt<Participante>(_config);
}
