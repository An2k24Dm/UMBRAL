using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

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

        return await _repositorio.ConsultarUsuariosInternosAsync(
            pagina, tamanio, rolFiltro, orden, cancelacion);
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
