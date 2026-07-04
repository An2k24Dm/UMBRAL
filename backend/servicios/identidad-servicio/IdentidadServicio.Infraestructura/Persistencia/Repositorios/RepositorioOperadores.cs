using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Infraestructura.Persistencia.Mapeadores;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioOperadores : IRepositorioOperadores
{
    private readonly ContextoIdentidad _contexto;
    private readonly IdentidadMapeador _mapeador;
    private readonly ReconstructorAgregadoUsuario _reconstructor;

    public RepositorioOperadores(ContextoIdentidad contexto)
    {
        _contexto = contexto;
        _mapeador = new IdentidadMapeador();
        _reconstructor = new ReconstructorAgregadoUsuario(contexto, _mapeador);
    }

    public async Task<Operador?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion)
    {
        var u = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancelacion);
        if (u is null || u.Rol != (int)RolUsuario.Operador) return null;
        var reconstruido = await _reconstructor.ReconstruirAsync(u, cancelacion);
        return reconstruido as Operador;
    }

    public Task AgregarAsync(
        Operador operador, string idKeycloak, CancellationToken cancelacion)
    {
        var modelos = _mapeador.AModelos(operador, idKeycloak);
        _contexto.Usuarios.Add(modelos.Usuario);
        _contexto.Personas.Add(modelos.Persona);
        _contexto.Operadores.Add(modelos.Operador);
        return Task.CompletedTask;
    }

    public async Task<string> ActualizarAsync(Operador operador, CancellationToken cancelacion)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == operador.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {operador.Id} no existe en base de datos.");

        if (usuario.Rol != (int)RolUsuario.Operador)
            throw new InvalidOperationException(
                "SÃ³lo se puede actualizar mediante este mÃ©todo a usuarios con rol Operador.");

        var persona = await _contexto.Personas
            .FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {operador.Id} no tiene fila Persona asociada.");

        usuario.NombreUsuario = operador.NombreUsuario.Valor;

        persona.Nombre = operador.NombrePersona.Nombre;
        persona.Apellido = operador.NombrePersona.Apellido;
        persona.Correo = operador.Correo.Valor;
        persona.Direccion = operador.DatosContacto.Direccion;
        persona.Telefono = operador.DatosContacto.Telefono;
        persona.Sexo = (int)operador.Sexo;
        persona.FechaNacimiento = operador.FechaNacimiento;

        return usuario.IdKeycloak;
    }

    public async Task<string?> ObtenerIdKeycloakAsync(
        Guid idOperador, CancellationToken cancelacion)
    {
        return await _contexto.Usuarios.AsNoTracking()
            .Where(u => u.Id == idOperador && u.Rol == (int)RolUsuario.Operador)
            .Select(u => u.IdKeycloak)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task EliminarAsync(Operador operador, CancellationToken cancelacion)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == operador.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {operador.Id} no existe en base de datos.");

        if (usuario.Rol != (int)RolUsuario.Operador)
            throw new InvalidOperationException(
                "SÃ³lo se puede eliminar mediante este mÃ©todo a usuarios con rol Operador.");

        _contexto.Usuarios.Remove(usuario);
    }

    public async Task<string?> ObtenerUltimoCodigoAsync(CancellationToken cancelacion)
    {
        return await _contexto.Operadores.AsNoTracking()
            .Where(o => o.CodigoOperador.StartsWith("OP-"))
            .OrderByDescending(o => o.CodigoOperador)
            .Select(o => o.CodigoOperador)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task ActualizarEstadoAsync(Operador operador, CancellationToken cancelacion)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == operador.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {operador.Id} no existe en base de datos.");
        if (usuario.Rol != (int)RolUsuario.Operador)
            throw new InvalidOperationException(
                "SÃ³lo se puede cambiar el estado mediante este mÃ©todo a usuarios con rol Operador.");
        usuario.Estado = (int)operador.Estado;
    }
}
