using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

// HU34 — Listado de sesiones con visibilidad por rol.
//
//   Administrador → ve todas las sesiones.
//   Operador      → ve las propias + las creadas por algún
//                   Administrador, según lo reporta identidad-servicio.
//   Participante  → 403 (lo bloquea la política, pero defendemos
//                   aquí también por si el endpoint se desprotege).
//   No autenticado→ 401 (lo emite el middleware de Auth).
//
// El rol del creador NO se persiste en la sesión: el manejador
// pregunta a IClienteIdentidadUsuarios qué identificadores
// corresponden a Administrador y filtra en memoria sólo lo necesario.
// Con paginación futura, la llamada de identidad sigue siendo barata
// porque sólo viaja la lista de ids de la página actual.
public sealed class ListarSesionesManejador
    : IRequestHandler<ListarSesionesConsulta, List<SesionListadoDto>>
{
    private const string RolAdministrador = "Administrador";
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IClienteIdentidadUsuarios _clienteIdentidad;

    public ListarSesionesManejador(
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual,
        IClienteIdentidadUsuarios clienteIdentidad)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
        _clienteIdentidad = clienteIdentidad;
    }

    public async Task<List<SesionListadoDto>> Handle(
        ListarSesionesConsulta consulta, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado)
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para consultar sesiones.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Sólo Administrador u Operador pueden consultar sesiones.");

        // El repositorio trae las candidatas aplicando filtros opcionales.
        // Para Administrador, esta es la lista final; para Operador, se
        // recorta más abajo. Si más adelante se agrega paginación, la
        // ventana viaja a identidad-servicio sólo una vez por página.
        var candidatas = await _repositorio.ListarAsync(
            consulta.TipoJuego, consulta.Estado, cancelacion);

        IReadOnlyList<Sesion> visibles;
        if (_usuarioActual.TieneAlgunRol(RolAdministrador))
        {
            visibles = candidatas;
        }
        else
        {
            visibles = await FiltrarVisiblesParaOperadorAsync(candidatas, cancelacion);
        }

        return visibles.Select(s => new SesionListadoDto
        {
            Id = s.Id,
            Nombre = s.Nombre,
            TipoJuego = s.TipoJuego.ToString(),
            ContenidoJuegoId = s.ContenidoJuegoId,
            Modo = s.Modo.ToString(),
            Estado = s.Estado.ToString(),
            FechaProgramada = s.FechaProgramada,
            CreadaPorUsuarioId = s.CreadaPorUsuarioId,
            NumeroGrupos = 0
        }).ToList();
    }

    private async Task<IReadOnlyList<Sesion>> FiltrarVisiblesParaOperadorAsync(
        IReadOnlyList<Sesion> candidatas, CancellationToken cancelacion)
    {
        if (candidatas.Count == 0) return candidatas;

        var operadorId = _usuarioActual.Id ?? Guid.Empty;

        // 1) Separar las propias del Operador para no preguntar de más a
        //    identidad: ya sabemos que tiene permiso sobre ellas.
        var propias = candidatas
            .Where(s => s.CreadaPorUsuarioId == operadorId)
            .ToList();

        var deOtros = candidatas
            .Where(s => s.CreadaPorUsuarioId != operadorId)
            .ToList();

        if (deOtros.Count == 0) return propias;

        // 2) Preguntar a identidad cuáles de los OTROS creadores son
        //    Administrador. La lista que viaja son sólo ids únicos.
        var idsCreadores = deOtros
            .Select(s => s.CreadaPorUsuarioId)
            .Distinct()
            .ToList();

        var administradores = await _clienteIdentidad.FiltrarAdministradoresAsync(
            idsCreadores, cancelacion);

        var administradoresSet = new HashSet<Guid>(administradores);

        // 3) Concatenar propias + las creadas por algún Administrador.
        //    Se mantiene el orden por FechaProgramada descendente que
        //    venía del repositorio.
        var resultado = candidatas
            .Where(s => s.CreadaPorUsuarioId == operadorId
                        || administradoresSet.Contains(s.CreadaPorUsuarioId))
            .ToList();
        return resultado;
    }
}
