using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU03 — registro público de Participante desde la app móvil. Reutiliza el
// Strategy/Factory ya existentes (EstrategiaCrearParticipante) para no
// duplicar la creación del agregado de dominio, pero mantiene su propio
// validador y comando para que las reglas no se entremezclen con HU02.
public sealed class RegistrarParticipanteManejador
    : IRequestHandler<RegistrarParticipanteComando, CrearUsuarioRespuestaDto>
{
    private readonly IRepositorioIdentidad _repositorio;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IProveedorFechaHora _reloj;
    private readonly FabricaEstrategiaCreacionUsuario _fabrica;
    private readonly IValidador<RegistrarParticipanteDto> _validador;
    private readonly ILogger<RegistrarParticipanteManejador> _registro;

    public RegistrarParticipanteManejador(
        IRepositorioIdentidad repositorio,
        IProveedorIdentidad proveedor,
        IProveedorFechaHora reloj,
        FabricaEstrategiaCreacionUsuario fabrica,
        IValidador<RegistrarParticipanteDto> validador,
        ILogger<RegistrarParticipanteManejador> registro)
    {
        _repositorio = repositorio;
        _proveedor = proveedor;
        _reloj = reloj;
        _fabrica = fabrica;
        _validador = validador;
        _registro = registro;
    }

    public async Task<CrearUsuarioRespuestaDto> Handle(
        RegistrarParticipanteComando comando, CancellationToken cancelacion)
    {
        var dto = comando.Datos;

        // 1) Validación específica de HU03 (campos + duplicados, incluido alias).
        await _validador.ValidarAsync(dto, cancelacion);

        // 2) Estrategia fija: HU03 siempre registra Participante. El backend nunca
        //    permite que el cliente decida el rol por esta vía.
        var estrategia = _fabrica.Obtener(RolUsuario.Participante);

        // 3) Fecha vía IProveedorFechaHora.
        var fechaRegistro = _reloj.ObtenerFechaHoraUtc();

        // 4) Crear en Keycloak con los datos canónicos.
        var datosIdentidad = new DatosCreacionUsuarioIdentidad(
            NombreUsuario: dto.NombreUsuario.Trim(),
            Correo: dto.Correo.Trim().ToLowerInvariant(),
            Contrasena: dto.Contrasena,
            Nombre: dto.Nombre.Trim(),
            Apellido: dto.Apellido.Trim());

        var idKeycloak = await _proveedor.CrearUsuarioAsync(datosIdentidad, cancelacion);

        try
        {
            await _proveedor.AsignarRolAsync(
                idKeycloak, estrategia.ObtenerRol().ToString(), cancelacion);

            // 5) Mapeo a DatosCreacionUsuario (modelo interno de aplicación)
            //    para reutilizar la estrategia sin acoplar a un DTO de
            //    transporte. TipoUsuario lo forzamos a Participante: el dato
            //    no proviene del cliente.
            var datosCreacion = new DatosCreacionUsuario
            {
                TipoUsuario = RolUsuario.Participante,
                Alias = dto.Alias.Trim(),
                NombreUsuario = dto.NombreUsuario,
                Correo = dto.Correo,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Sexo = dto.Sexo,
                FechaNacimiento = DatosCreacionUsuario.NormalizarFechaNacimiento(dto.FechaNacimiento),
                DatosContacto = dto.DatosContacto
            };

            var usuario = await estrategia.CrearUsuarioDominioAsync(
                datosCreacion, fechaRegistro, cancelacion);
            await estrategia.GuardarAsync(usuario, idKeycloak, _repositorio, cancelacion);

            _registro.LogInformation(
                "Participante {NombreUsuario} ({Correo}) registrado desde la app móvil.",
                usuario.NombreUsuario.Valor, usuario.Correo.Valor);

            return new CrearUsuarioRespuestaDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario.Valor,
                Correo = usuario.Correo.Valor,
                Rol = usuario.Rol.ToString(),
                Estado = usuario.Estado.ToString(),
                Codigo = null,
                Mensaje = "Participante registrado correctamente."
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
