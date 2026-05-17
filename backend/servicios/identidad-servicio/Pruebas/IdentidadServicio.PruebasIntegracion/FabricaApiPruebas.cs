using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IdentidadServicio.PruebasIntegracion;

public sealed class FabricaApiPruebas : WebApplicationFactory<Program>
{
    public Mock<IProveedorIdentidad> MockProveedor { get; } = new();
    private readonly string _nombreBaseDatos = "UmbralIdentidadPruebas-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Testing");

        constructor.ConfigureServices(servicios =>
        {
            var descCtx = servicios.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ContextoIdentidad>));
            if (descCtx is not null) servicios.Remove(descCtx);

            servicios.AddDbContext<ContextoIdentidad>(opciones =>
                opciones.UseInMemoryDatabase(_nombreBaseDatos));

            var descProv = servicios.SingleOrDefault(d => d.ServiceType == typeof(IProveedorIdentidad));
            if (descProv is not null) servicios.Remove(descProv);
            servicios.AddSingleton(MockProveedor.Object);

            using var alcance = servicios.BuildServiceProvider().CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();
            contexto.Database.EnsureCreated();

            if (!contexto.Usuarios.Any()) Sembrar(contexto);
        });
    }

    private static void Sembrar(ContextoIdentidad contexto)
    {
        var idAdmin = Guid.NewGuid();
        var idParticipanteActivo = Guid.NewGuid();
        var idInactivo = Guid.NewGuid();
        var ahora = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        var nac = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        contexto.Usuarios.AddRange(
            new UsuarioModelo
            {
                Id = idAdmin, NombreUsuario = "admin_umbral", IdKeycloak = "kc-admin",
                Rol = (int)RolUsuario.Administrador, Estado = (int)EstadoUsuario.Activo, FechaRegistro = ahora
            },
            new UsuarioModelo
            {
                Id = idParticipanteActivo, NombreUsuario = "participante01", IdKeycloak = "kc-par-activo",
                Rol = (int)RolUsuario.Participante, Estado = (int)EstadoUsuario.Activo, FechaRegistro = ahora
            },
            new UsuarioModelo
            {
                Id = idInactivo, NombreUsuario = "inactivo01", IdKeycloak = "kc-inactivo",
                Rol = (int)RolUsuario.Participante, Estado = (int)EstadoUsuario.Inactivo, FechaRegistro = ahora
            });

        var pAdmin = new PersonaModelo
        {
            Id = Guid.NewGuid(), UsuarioId = idAdmin,
            Nombre = "Ada", Apellido = "Admin", Correo = "ada@umbral.com",
            Sexo = (int)SexoPersona.Femenino, FechaNacimiento = nac, FechaRegistro = ahora
        };
        var pParActivo = new PersonaModelo
        {
            Id = Guid.NewGuid(), UsuarioId = idParticipanteActivo,
            Nombre = "Pablo", Apellido = "Par", Correo = "pablo@umbral.com",
            Sexo = (int)SexoPersona.Masculino, FechaNacimiento = nac, FechaRegistro = ahora
        };
        var pInactivo = new PersonaModelo
        {
            Id = Guid.NewGuid(), UsuarioId = idInactivo,
            Nombre = "Iván", Apellido = "Inactivo", Correo = "ivan@umbral.com",
            Sexo = (int)SexoPersona.Masculino, FechaNacimiento = nac, FechaRegistro = ahora
        };
        contexto.Personas.AddRange(pAdmin, pParActivo, pInactivo);

        contexto.Administradores.Add(new AdministradorModelo
        {
            Id = Guid.NewGuid(), PersonaId = pAdmin.Id,
            CodigoAdministrador = "ADM-001", FechaRegistro = ahora
        });
        contexto.Participantes.AddRange(
            new ParticipanteModelo
            {
                Id = Guid.NewGuid(), PersonaId = pParActivo.Id,
                Alias = "pablito", FechaRegistro = ahora
            },
            new ParticipanteModelo
            {
                Id = Guid.NewGuid(), PersonaId = pInactivo.Id,
                Alias = "ivani", FechaRegistro = ahora
            });

        contexto.SaveChanges();
    }
}
