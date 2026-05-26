using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU09 — manejador del comando de edición parcial del Operador.
//
// Estrategia:
//  1. Cargar el Operador (404 si no existe / no es Operador).
//  2. Capturar el snapshot ORIGINAL de los valores editables.
//  3. Validar el DTO con ValidadorModificarOperador (mismas reglas que HU02
//     reusando el catálogo de mensajes y los Regex).
//  4. Para cada campo, si llegó un valor distinto al original aplicar el
//     método expresivo del dominio. Si no, no se toca nada.
//  5. Si no hubo cambios reales, devolver respuesta indicándolo SIN persistir.
//  6. Persistir mediante ActualizarOperadorAsync (transacción).
//  7. Si cambió correo / nombreUsuario / nombre / apellido, sincronizar
//     Keycloak con el payload parcial correspondiente.
//  8. Mapear a PerfilOperadorDto y devolver.
//
// Estado, Rol y FechaRegistro nunca se modifican: el dominio no tiene métodos
// públicos para ellos en esta HU y el repositorio no los reescribe.
public sealed class ModificarOperadorManejador
    : IRequestHandler<ModificarOperadorComando, ModificarOperadorRespuestaDto>
{
    private readonly IRepositorioOperadores _repositorioOperadores;
    private readonly IRepositorioUnicidadUsuario _unicidad;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IValidador<ModificarOperadorComando> _validador;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;
    private readonly ILogger<ModificarOperadorManejador> _registro;

    public ModificarOperadorManejador(
        IRepositorioOperadores repositorioOperadores,
        IRepositorioUnicidadUsuario unicidad,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IValidador<ModificarOperadorComando> validador,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo,
        ILogger<ModificarOperadorManejador> registro)
    {
        _repositorioOperadores = repositorioOperadores;
        _unicidad = unicidad;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _validador = validador;
        _fabricaMapeo = fabricaMapeo;
        _registro = registro;
    }

    public async Task<ModificarOperadorRespuestaDto> Handle(
        ModificarOperadorComando comando, CancellationToken cancelacion)
    {
        var operador = await _repositorioOperadores.ObtenerPorIdAsync(comando.IdOperador, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Operador con id {comando.IdOperador}.");

        // 1) Validación de formato (sincrónica, sin acceso a base).
        _validador.Validar(comando).LanzarSiHayErrores();

        // 2) Duplicados que excluyen al propio Operador.
        await ValidarDuplicadosAsync(comando.IdOperador, comando.Datos, cancelacion);

        // Snapshot del estado actual: se usa para decidir qué cambió y para
        // armar el payload parcial hacia Keycloak con los valores nuevos.
        var nombreUsuarioOriginal = operador.NombreUsuario.Valor;
        var correoOriginal = operador.Correo.Valor;
        var nombreOriginal = operador.NombrePersona.Nombre;
        var apellidoOriginal = operador.NombrePersona.Apellido;
        var direccionOriginal = operador.DatosContacto.Direccion;
        var telefonoOriginal = operador.DatosContacto.Telefono;
        var sexoOriginal = operador.Sexo;
        var fechaOriginal = operador.FechaNacimiento;

        var dto = comando.Datos;
        var camposActualizados = new List<string>();

        // ---------- Nombre de usuario ----------
        string? nombreUsuarioKeycloak = null;
        if (dto.NombreUsuario is not null)
        {
            var nuevo = NombreUsuario.Crear(dto.NombreUsuario);
            if (!string.Equals(nuevo.Valor, nombreUsuarioOriginal, StringComparison.Ordinal))
            {
                operador.ActualizarNombreUsuario(nuevo);
                camposActualizados.Add("nombreUsuario");
                nombreUsuarioKeycloak = nuevo.Valor;
            }
        }

        // ---------- Correo ----------
        string? correoKeycloak = null;
        if (dto.Correo is not null)
        {
            var nuevo = Correo.Crear(dto.Correo);
            if (!string.Equals(nuevo.Valor, correoOriginal, StringComparison.Ordinal))
            {
                operador.ActualizarCorreo(nuevo);
                camposActualizados.Add("correo");
                correoKeycloak = nuevo.Valor;
            }
        }

        // ---------- Nombre / Apellido (NombrePersona = VO con ambos) ----------
        string? nombreKeycloak = null;
        string? apellidoKeycloak = null;
        var nombreNuevo = dto.Nombre?.Trim();
        var apellidoNuevo = dto.Apellido?.Trim();
        var cambioNombre = nombreNuevo is not null && nombreNuevo != nombreOriginal;
        var cambioApellido = apellidoNuevo is not null && apellidoNuevo != apellidoOriginal;
        if (cambioNombre || cambioApellido)
        {
            var nombreFinal = cambioNombre ? nombreNuevo! : nombreOriginal;
            var apellidoFinal = cambioApellido ? apellidoNuevo! : apellidoOriginal;
            operador.ActualizarNombrePersona(NombrePersona.Crear(nombreFinal, apellidoFinal));
            if (cambioNombre)
            {
                camposActualizados.Add("nombre");
                nombreKeycloak = nombreFinal;
            }
            if (cambioApellido)
            {
                camposActualizados.Add("apellido");
                apellidoKeycloak = apellidoFinal;
            }
        }

        // ---------- DatosContacto (Dirección / Teléfono) ----------
        var direccionNueva = dto.DatosContacto?.Direccion?.Trim();
        var telefonoNuevo = dto.DatosContacto?.Telefono;
        var cambioDireccion = direccionNueva is not null && direccionNueva != direccionOriginal;
        var cambioTelefono = telefonoNuevo is not null && telefonoNuevo != telefonoOriginal;
        if (cambioDireccion || cambioTelefono)
        {
            var direccionFinal = cambioDireccion ? direccionNueva! : direccionOriginal;
            var telefonoFinal = cambioTelefono ? telefonoNuevo! : telefonoOriginal;
            operador.ActualizarDatosContacto(DatosContacto.Crear(direccionFinal, telefonoFinal));
            if (cambioDireccion) camposActualizados.Add("datosContacto.direccion");
            if (cambioTelefono) camposActualizados.Add("datosContacto.telefono");
        }

        // ---------- Sexo ----------
        if (dto.Sexo is not null)
        {
            var nuevoSexo = DtoMapeador.ParsearSexo(dto.Sexo);
            if (nuevoSexo != sexoOriginal)
            {
                operador.ActualizarSexo(nuevoSexo);
                camposActualizados.Add("sexo");
            }
        }

        // ---------- FechaNacimiento ----------
        if (dto.FechaNacimiento is not null)
        {
            var nuevaFecha = DateTime.SpecifyKind(dto.FechaNacimiento.Value.Date, DateTimeKind.Utc);
            if (nuevaFecha != fechaOriginal)
            {
                operador.ActualizarFechaNacimiento(nuevaFecha);
                camposActualizados.Add("fechaNacimiento");
            }
        }

        if (camposActualizados.Count == 0)
        {
            _registro.LogInformation(
                "Edición HU09 sin cambios para Operador {Id}.", operador.Id);

            return new ModificarOperadorRespuestaDto
            {
                HuboCambios = false,
                CamposActualizados = Array.Empty<string>(),
                Mensaje = "No había cambios para aplicar.",
                Operador = MapearPerfil(operador)
            };
        }

        // Persistencia: el repositorio aplica los cambios sobre los modelos
        // EF (sin guardar); la unidad de trabajo confirma SaveChanges. Si la
        // confirmación falla, no llegamos a Keycloak.
        var idKeycloak = await _repositorioOperadores.ActualizarAsync(operador, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        // Sincronización con Keycloak. El proveedor sólo envía los campos no
        // nulos; si todos son null, no hace llamada HTTP.
        var datosKeycloak = new DatosActualizacionUsuarioIdentidad(
            NombreUsuario: nombreUsuarioKeycloak,
            Correo: correoKeycloak,
            Nombre: nombreKeycloak,
            Apellido: apellidoKeycloak);

        if (datosKeycloak.TieneCambios && !string.IsNullOrEmpty(idKeycloak))
        {
            try
            {
                await _proveedor.ActualizarUsuarioAsync(idKeycloak, datosKeycloak, cancelacion);
            }
            catch (Exception ex)
            {
                _registro.LogError(ex,
                    "Falló la sincronización con Keycloak para Operador {Id} (KC={Kc}). " +
                    "Los datos en BD ya están actualizados; se requiere intervención manual.",
                    operador.Id, idKeycloak);
                throw;
            }
        }

        _registro.LogInformation(
            "Operador {Id} actualizado. Campos: {Campos}.",
            operador.Id, string.Join(",", camposActualizados));

        return new ModificarOperadorRespuestaDto
        {
            HuboCambios = true,
            CamposActualizados = camposActualizados,
            Mensaje = "Operador actualizado correctamente.",
            Operador = MapearPerfil(operador)
        };
    }

    private PerfilOperadorDto MapearPerfil(Operador operador)
        => (PerfilOperadorDto)_fabricaMapeo.Mapear(operador);

    // HU09 — duplicados al editar: excluyen al propio Operador para permitir
    // que el cliente reenvíe valores ya asignados al mismo usuario sin error.
    private async Task ValidarDuplicadosAsync(
        Guid idOperador,
        Commons.Dtos.ModificarOperadorSolicitudDto dto,
        CancellationToken cancelacion)
    {
        var resultado = ResultadoValidacion.Exitoso();

        if (dto.NombreUsuario is not null &&
            await _unicidad.ExisteNombreUsuarioEnOtroUsuarioAsync(
                dto.NombreUsuario, idOperador, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioDuplicado);
        }

        if (dto.Correo is not null &&
            await _unicidad.ExisteCorreoEnOtroUsuarioAsync(
                dto.Correo, idOperador, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoDuplicado);
        }

        if (!string.IsNullOrWhiteSpace(dto.DatosContacto?.Telefono) &&
            await _unicidad.ExisteTelefonoEnOtroUsuarioAsync(
                dto.DatosContacto!.Telefono!, idOperador, cancelacion))
        {
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoDuplicado);
        }

        resultado.LanzarSiHayErrores();
    }
}
