using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU08 — arma el listado paginado de cuentas internas (Operador y
// Administrador) para el panel web.
// - Normaliza los parámetros (página >= 1, tamaño fijo en 10, rol y orden).
// - Delega la consulta al repositorio, que devuelve entidades de dominio.
// - Mapea cada entidad concreta a UsuarioInternoListadoDto. Si por alguna
//   razón apareciera un Participante, se descarta defensivamente.
public sealed class ConsultarUsuariosInternosManejador
    : IRequestHandler<ConsultarUsuariosInternosConsulta, ResultadoPaginadoDto<UsuarioInternoListadoDto>>
{
    // HU08 fija tamaño de página en 10 elementos. Si llega otro valor desde
    // el cliente, se normaliza para mantener la regla del caso de uso.
    private const int TamanioPaginaPorDefecto = 10;

    private readonly IRepositorioIdentidad _repositorio;

    public ConsultarUsuariosInternosManejador(IRepositorioIdentidad repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ResultadoPaginadoDto<UsuarioInternoListadoDto>> Handle(
        ConsultarUsuariosInternosConsulta consulta, CancellationToken cancelacion)
    {
        var pagina = consulta.Pagina < 1 ? 1 : consulta.Pagina;
        // HU08 fija el tamaño de página: el cliente puede enviar el valor,
        // pero el caso de uso siempre opera con TamanioPaginaPorDefecto.
        var tamanio = TamanioPaginaPorDefecto;

        var rolFiltro = MapearFiltroRol(consulta.Rol);
        var orden = NormalizarOrden(consulta.OrdenEstado);

        var usuarios = await _repositorio.ConsultarUsuariosInternosAsync(
            pagina, tamanio, rolFiltro, orden, cancelacion);
        var total = await _repositorio.ContarUsuariosInternosAsync(rolFiltro, cancelacion);

        var elementos = new List<UsuarioInternoListadoDto>(usuarios.Count);
        foreach (var usuario in usuarios)
        {
            var fila = MapearAFila(usuario);
            if (fila is not null) elementos.Add(fila);
        }

        return new ResultadoPaginadoDto<UsuarioInternoListadoDto>(
            elementos, pagina, tamanio, total);
    }

    // Construye la fila de listado a partir de la entidad concreta. Cualquier
    // tipo no soportado (p. ej. Participante) se descarta: HU08 solo expone
    // cuentas internas.
    private static UsuarioInternoListadoDto? MapearAFila(Usuario usuario)
    {
        return usuario switch
        {
            Operador operador => new UsuarioInternoListadoDto
            {
                Id = operador.Id,
                CodigoOperador = operador.CodigoOperador,
                CodigoAdministrador = null,
                NombreUsuario = operador.NombreUsuario.Valor,
                Nombre = operador.NombrePersona.Nombre,
                Apellido = operador.NombrePersona.Apellido,
                Rol = operador.Rol.ToString(),
                Estado = operador.Estado.ToString(),
                Sexo = operador.Sexo.ToString()
            },
            Administrador administrador => new UsuarioInternoListadoDto
            {
                Id = administrador.Id,
                CodigoOperador = null,
                CodigoAdministrador = administrador.CodigoAdministrador,
                NombreUsuario = administrador.NombreUsuario.Valor,
                Nombre = administrador.NombrePersona.Nombre,
                Apellido = administrador.NombrePersona.Apellido,
                Rol = administrador.Rol.ToString(),
                Estado = administrador.Estado.ToString(),
                Sexo = administrador.Sexo.ToString()
            },
            _ => null
        };
    }

    private static RolUsuario? MapearFiltroRol(string? rol)
    {
        if (string.IsNullOrWhiteSpace(rol)) return null;
        var valor = rol.Trim();
        if (string.Equals(valor, "Todos", StringComparison.OrdinalIgnoreCase)) return null;
        if (string.Equals(valor, "Operador", StringComparison.OrdinalIgnoreCase))
            return RolUsuario.Operador;
        if (string.Equals(valor, "Administrador", StringComparison.OrdinalIgnoreCase))
            return RolUsuario.Administrador;
        // Cualquier otro valor se ignora (equivale a "Todos").
        return null;
    }

    private static string? NormalizarOrden(string? orden)
    {
        if (string.IsNullOrWhiteSpace(orden)) return null;
        var valor = orden.Trim().ToLowerInvariant();
        return valor is "asc" or "desc" ? valor : null;
    }
}
