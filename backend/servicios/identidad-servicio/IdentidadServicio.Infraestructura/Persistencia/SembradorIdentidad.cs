using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia;

public static class SembradorIdentidad
{
    private static readonly Guid IdAdmin = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid IdOperador = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid IdParticipanteActivo = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid IdParticipanteInactivo = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private const string IdKeycloakAdmin = "kc-administrador-001";
    private const string IdKeycloakOperador = "kc-operador-001";
    private const string IdKeycloakParticipanteActivo = "kc-participante-001";
    private const string IdKeycloakParticipanteInactivo = "kc-inactivo-001";

    public static async Task SembrarAsync(
        ContextoIdentidad contexto,
        IProveedorFechaHora reloj,
        CancellationToken cancelacion)
    {
        await EjecutarMigracionConReintentosAsync(contexto, cancelacion);

        var ahora = reloj.ObtenerFechaHoraUtc();
        var nacimientoAdmin = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var nacimientoOperador = new DateTime(1998, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var nacimientoParticipante = new DateTime(2000, 5, 20, 0, 0, 0, DateTimeKind.Utc);
        var nacimientoInactivo = new DateTime(2001, 8, 15, 0, 0, 0, DateTimeKind.Utc);

        await SembrarAdministradorAsync(
            contexto,
            IdAdmin,
            "administrador01",
            IdKeycloakAdmin,
            "Angelo",
            "Di Martino",
            "administrador01@umbral.com",
            SexoPersona.Masculino,
            nacimientoAdmin,
            "ADM-001",
            ahora,
            cancelacion);

        await SembrarOperadorAsync(
            contexto,
            IdOperador,
            "operador01",
            IdKeycloakOperador,
            "Carlos",
            "Perez",
            "operador01@umbral.com",
            SexoPersona.Masculino,
            nacimientoOperador,
            "OP-001",
            ahora,
            cancelacion);

        await SembrarParticipanteAsync(
            contexto,
            IdParticipanteActivo,
            "participante01",
            IdKeycloakParticipanteActivo,
            "Maria",
            "Gomez",
            "participante01@umbral.com",
            SexoPersona.Femenino,
            nacimientoParticipante,
            "participante01",
            EstadoUsuario.Activo,
            ahora,
            cancelacion);

        await SembrarParticipanteAsync(
            contexto,
            IdParticipanteInactivo,
            "participante_inactivo01",
            IdKeycloakParticipanteInactivo,
            "Pedro",
            "Inactivo",
            "participante.inactivo01@umbral.com",
            SexoPersona.Masculino,
            nacimientoInactivo,
            "participante_inactivo01",
            EstadoUsuario.Inactivo,
            ahora,
            cancelacion);

        await contexto.SaveChangesAsync(cancelacion);
    }

    private static async Task SembrarAdministradorAsync(
        ContextoIdentidad contexto,
        Guid usuarioId,
        string nombreUsuario,
        string idKeycloak,
        string nombre,
        string apellido,
        string correo,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string codigoAdministrador,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        var usuario = await ObtenerOCrearUsuarioAsync(
            contexto,
            usuarioId,
            nombreUsuario,
            idKeycloak,
            RolUsuario.Administrador,
            EstadoUsuario.Activo,
            ahora,
            cancelacion);

        var persona = await ObtenerOCrearPersonaAsync(
            contexto,
            usuario.Id,
            nombre,
            apellido,
            correo,
            sexo,
            fechaNacimiento,
            ahora,
            cancelacion);

        var existeAdministrador = await contexto.Administradores
            .AnyAsync(a => a.PersonaId == persona.Id, cancelacion);

        if (!existeAdministrador)
        {
            contexto.Administradores.Add(new AdministradorModelo
            {
                Id = Guid.NewGuid(),
                PersonaId = persona.Id,
                CodigoAdministrador = codigoAdministrador,
                FechaRegistro = ahora
            });
        }
    }

    private static async Task SembrarOperadorAsync(
        ContextoIdentidad contexto,
        Guid usuarioId,
        string nombreUsuario,
        string idKeycloak,
        string nombre,
        string apellido,
        string correo,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string codigoOperador,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        var usuario = await ObtenerOCrearUsuarioAsync(
            contexto,
            usuarioId,
            nombreUsuario,
            idKeycloak,
            RolUsuario.Operador,
            EstadoUsuario.Activo,
            ahora,
            cancelacion);

        var persona = await ObtenerOCrearPersonaAsync(
            contexto,
            usuario.Id,
            nombre,
            apellido,
            correo,
            sexo,
            fechaNacimiento,
            ahora,
            cancelacion);

        var existeOperador = await contexto.Operadores
            .AnyAsync(o => o.PersonaId == persona.Id, cancelacion);

        if (!existeOperador)
        {
            contexto.Operadores.Add(new OperadorModelo
            {
                Id = Guid.NewGuid(),
                PersonaId = persona.Id,
                CodigoOperador = codigoOperador,
                FechaRegistro = ahora
            });
        }
    }

    private static async Task SembrarParticipanteAsync(
        ContextoIdentidad contexto,
        Guid usuarioId,
        string nombreUsuario,
        string idKeycloak,
        string nombre,
        string apellido,
        string correo,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string alias,
        EstadoUsuario estado,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        var usuario = await ObtenerOCrearUsuarioAsync(
            contexto,
            usuarioId,
            nombreUsuario,
            idKeycloak,
            RolUsuario.Participante,
            estado,
            ahora,
            cancelacion);

        var persona = await ObtenerOCrearPersonaAsync(
            contexto,
            usuario.Id,
            nombre,
            apellido,
            correo,
            sexo,
            fechaNacimiento,
            ahora,
            cancelacion);

        var existeParticipante = await contexto.Participantes
            .AnyAsync(p => p.PersonaId == persona.Id, cancelacion);

        if (!existeParticipante)
        {
            contexto.Participantes.Add(new ParticipanteModelo
            {
                Id = Guid.NewGuid(),
                PersonaId = persona.Id,
                Alias = alias,
                FechaRegistro = ahora
            });
        }
    }

    private static async Task<UsuarioModelo> ObtenerOCrearUsuarioAsync(
        ContextoIdentidad contexto,
        Guid id,
        string nombreUsuario,
        string idKeycloak,
        RolUsuario rol,
        EstadoUsuario estado,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        var usuario = await contexto.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, cancelacion);

        if (usuario is not null)
        {
            usuario.IdKeycloak = idKeycloak;
            usuario.Rol = (int)rol;
            usuario.Estado = (int)estado;
            return usuario;
        }

        usuario = new UsuarioModelo
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            IdKeycloak = idKeycloak,
            Rol = (int)rol,
            Estado = (int)estado,
            FechaRegistro = ahora
        };

        contexto.Usuarios.Add(usuario);

        return usuario;
    }

    private static async Task<PersonaModelo> ObtenerOCrearPersonaAsync(
        ContextoIdentidad contexto,
        Guid usuarioId,
        string nombre,
        string apellido,
        string correo,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        var persona = await contexto.Personas
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, cancelacion);

        if (persona is not null)
        {
            persona.Nombre = nombre;
            persona.Apellido = apellido;
            persona.Correo = correo;
            persona.Sexo = (int)sexo;
            persona.FechaNacimiento = fechaNacimiento;
            return persona;
        }

        persona = new PersonaModelo
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Nombre = nombre,
            Apellido = apellido,
            Correo = correo,
            Sexo = (int)sexo,
            FechaNacimiento = fechaNacimiento,
            FechaRegistro = ahora
        };

        contexto.Personas.Add(persona);

        return persona;
    }

    private static async Task EjecutarMigracionConReintentosAsync(
        ContextoIdentidad contexto,
        CancellationToken cancelacion)
    {
        const int intentosMaximos = 15;
        var espera = TimeSpan.FromSeconds(3);

        for (var intento = 1; intento <= intentosMaximos; intento++)
        {
            try
            {
                await contexto.Database.MigrateAsync(cancelacion);
                return;
            }
            catch when (intento < intentosMaximos)
            {
                await Task.Delay(espera, cancelacion);
            }
        }
    }
}