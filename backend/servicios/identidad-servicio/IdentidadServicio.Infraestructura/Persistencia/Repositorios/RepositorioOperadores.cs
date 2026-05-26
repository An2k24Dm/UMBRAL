using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

// Repositorio específico de Operadores (HU02 alta, HU09 edición, generador de
// códigos OP-###). Las operaciones de escritura no llaman a SaveChangesAsync;
// la confirmación la hace el manejador vía IUnidadTrabajoIdentidad.
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
                "Sólo se puede actualizar mediante este método a usuarios con rol Operador.");

        var persona = await _contexto.Personas
            .FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {operador.Id} no tiene fila Persona asociada.");

        // Campos editables únicamente. Estado, Rol, FechaRegistro e
        // IdKeycloak no se reescriben.
        usuario.NombreUsuario = operador.NombreUsuario.Valor;

        persona.Nombre = operador.NombrePersona.Nombre;
        persona.Apellido = operador.NombrePersona.Apellido;
        persona.Correo = operador.Correo.Valor;
        persona.Direccion = operador.DatosContacto.Direccion;
        persona.Telefono = operador.DatosContacto.Telefono;
        persona.Sexo = (int)operador.Sexo;
        persona.FechaNacimiento = operador.FechaNacimiento;

        // No se llama a SaveChangesAsync aquí: la unidad de trabajo decide
        // cuándo persistir. Devolvemos el IdKeycloak para que el manejador
        // pueda sincronizar Keycloak tras GuardarCambiosAsync.
        return usuario.IdKeycloak;
    }

    // Códigos con formato OP-### zero-padded a 3 dígitos: el orden
    // descendente alfabético coincide con el numérico hasta 999.
    public async Task<string?> ObtenerUltimoCodigoAsync(CancellationToken cancelacion)
    {
        return await _contexto.Operadores.AsNoTracking()
            .Where(o => o.CodigoOperador.StartsWith("OP-"))
            .OrderByDescending(o => o.CodigoOperador)
            .Select(o => o.CodigoOperador)
            .FirstOrDefaultAsync(cancelacion);
    }
}
