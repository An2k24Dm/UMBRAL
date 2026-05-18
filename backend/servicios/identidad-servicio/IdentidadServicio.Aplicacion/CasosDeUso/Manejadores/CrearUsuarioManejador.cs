using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class CrearUsuarioManejador
    : IRequestHandler<CrearUsuarioComando, CrearUsuarioRespuestaDto>
{
    private readonly IRepositorioIdentidad _repositorio;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IProveedorFechaHora _reloj;
    private readonly FabricaEstrategiaCreacionUsuario _fabrica;
    private readonly ILogger<CrearUsuarioManejador> _registro;

    public CrearUsuarioManejador(
        IRepositorioIdentidad repositorio,
        IProveedorIdentidad proveedor,
        IProveedorFechaHora reloj,
        FabricaEstrategiaCreacionUsuario fabrica,
        ILogger<CrearUsuarioManejador> registro)
    {
        _repositorio = repositorio;
        _proveedor = proveedor;
        _reloj = reloj;
        _fabrica = fabrica;
        _registro = registro;
    }

    public async Task<CrearUsuarioRespuestaDto> Handle(
        CrearUsuarioComando comando, CancellationToken cancelacion)
    {
        var dto = comando.Datos;

        // 1) Seleccionar la estrategia para el TipoUsuario.
        var estrategia = _fabrica.Obtener(dto.TipoUsuario);

        // 2) Validar duplicados (username y correo son únicos).
        var nombre = NombreUsuario.Crear(dto.NombreUsuario);
        var correo = Correo.Crear(dto.Correo);

        if (await _repositorio.ExisteNombreUsuarioAsync(nombre.Valor, cancelacion))
            throw new DatosUsuarioInvalidosExcepcion(
                $"El nombre de usuario '{nombre.Valor}' ya está registrado.");
        if (await _repositorio.ExisteCorreoAsync(correo.Valor, cancelacion))
            throw new DatosUsuarioInvalidosExcepcion(
                $"El correo '{correo.Valor}' ya está registrado.");

        // 3) Fecha vía IProveedorFechaHora.
        var fechaRegistro = _reloj.ObtenerFechaHoraUtc();

        // 4) Crear en Keycloak con username y correo SEPARADOS.
        var idKeycloak = await _proveedor.CrearUsuarioAsync(
            nombre.Valor, correo.Valor, dto.ContrasenaTemporal, cancelacion);

        try
        {
            await _proveedor.AsignarRolAsync(
                idKeycloak, estrategia.ObtenerRol().ToString(), cancelacion);

            var usuario = estrategia.CrearUsuarioDominio(dto, fechaRegistro);
            await estrategia.GuardarAsync(usuario, idKeycloak, _repositorio, cancelacion);

            _registro.LogInformation(
                "Usuario {NombreUsuario} ({Correo}) creado con rol {Rol}.",
                usuario.NombreUsuario.Valor, usuario.Correo.Valor, usuario.Rol);

            return new CrearUsuarioRespuestaDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario.Valor,
                Correo = usuario.Correo.Valor,
                Rol = usuario.Rol.ToString(),
                Estado = usuario.Estado.ToString(),
                Mensaje = $"{usuario.Rol} creado correctamente."
            };
        }
        catch
        {
            await CompensarKeycloakAsync(idKeycloak);
            throw;
        }
    }

    private async Task CompensarKeycloakAsync(string idKeycloak)
    {
        try { await _proveedor.EliminarUsuarioAsync(idKeycloak, CancellationToken.None); }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "Compensación fallida: requiere limpieza manual de {Id} en Keycloak.", idKeycloak);
        }
    }
}
