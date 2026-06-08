using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.Aplicacion.Servicios.Usuarios;

public sealed class AplicadorCambiosUsuario
{
    public ResultadoCambiosUsuario Aplicar(
        Usuario usuario, ModificarPerfilUsuarioDto dto)
    {
        var nombreUsuarioOriginal = usuario.NombreUsuario.Valor;
        var correoOriginal = usuario.Correo.Valor;
        var nombreOriginal = usuario.NombrePersona.Nombre;
        var apellidoOriginal = usuario.NombrePersona.Apellido;
        var direccionOriginal = usuario.DatosContacto.Direccion;
        var telefonoOriginal = usuario.DatosContacto.Telefono;
        var sexoOriginal = usuario.Sexo;
        var fechaOriginal = usuario.FechaNacimiento;

        var camposActualizados = new List<string>();
        string? nombreUsuarioKeycloak = null;
        string? correoKeycloak = null;
        string? nombreKeycloak = null;
        string? apellidoKeycloak = null;

        // ---------- Nombre de usuario ----------
        if (dto.NombreUsuario is not null)
        {
            var nuevo = NombreUsuario.Crear(dto.NombreUsuario);
            if (!string.Equals(nuevo.Valor, nombreUsuarioOriginal, StringComparison.Ordinal))
            {
                usuario.ActualizarNombreUsuario(nuevo);
                camposActualizados.Add("nombreUsuario");
                nombreUsuarioKeycloak = nuevo.Valor;
            }
        }

        // ---------- Correo ----------
        if (dto.Correo is not null)
        {
            var nuevo = Correo.Crear(dto.Correo);
            if (!string.Equals(nuevo.Valor, correoOriginal, StringComparison.Ordinal))
            {
                usuario.ActualizarCorreo(nuevo);
                camposActualizados.Add("correo");
                correoKeycloak = nuevo.Valor;
            }
        }

        // ---------- Nombre / Apellido (VO compuesto) ----------
        var nombreNuevo = dto.Nombre?.Trim();
        var apellidoNuevo = dto.Apellido?.Trim();
        var cambioNombre = nombreNuevo is not null && nombreNuevo != nombreOriginal;
        var cambioApellido = apellidoNuevo is not null && apellidoNuevo != apellidoOriginal;
        if (cambioNombre || cambioApellido)
        {
            var nombreFinal = cambioNombre ? nombreNuevo! : nombreOriginal;
            var apellidoFinal = cambioApellido ? apellidoNuevo! : apellidoOriginal;
            usuario.ActualizarNombrePersona(NombrePersona.Crear(nombreFinal, apellidoFinal));
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
            usuario.ActualizarDatosContacto(DatosContacto.Crear(direccionFinal, telefonoFinal));
            if (cambioDireccion) camposActualizados.Add("datosContacto.direccion");
            if (cambioTelefono) camposActualizados.Add("datosContacto.telefono");
        }

        // ---------- Sexo ----------
        if (dto.Sexo is not null)
        {
            var nuevoSexo = DtoMapeador.ParsearSexo(dto.Sexo);
            if (nuevoSexo != sexoOriginal)
            {
                usuario.ActualizarSexo(nuevoSexo);
                camposActualizados.Add("sexo");
            }
        }

        // ---------- FechaNacimiento ----------
        if (dto.FechaNacimiento is not null)
        {
            var nuevaFecha = DateTime.SpecifyKind(dto.FechaNacimiento.Value.Date, DateTimeKind.Utc);
            if (nuevaFecha != fechaOriginal)
            {
                usuario.ActualizarFechaNacimiento(nuevaFecha);
                camposActualizados.Add("fechaNacimiento");
            }
        }

        if (usuario is Dominio.Entidades.Participante participante &&
            dto is Commons.Dtos.ModificarParticipanteSolicitudDto dtoParticipante &&
            dtoParticipante.Alias is not null)
        {
            var aliasNuevo = dtoParticipante.Alias.Trim();
            if (!string.Equals(aliasNuevo, participante.Alias, StringComparison.Ordinal))
            {
                participante.ActualizarAlias(aliasNuevo);
                camposActualizados.Add("alias");
            }
        }

        // El cambio de contraseña SOLO aplica al flujo del Participante
        // editando su propio perfil (HU10). Para Operador/Administrador
        // (HU09) se usa el endpoint dedicado de reseteo administrativo.
        var cambiaContrasena = false;
        string? nuevaContrasena = null;
        if (dto is Commons.Dtos.ModificarParticipanteSolicitudDto dtoParticipanteContrasena &&
            dtoParticipanteContrasena.NuevaContrasena is not null)
        {
            cambiaContrasena = true;
            nuevaContrasena = dtoParticipanteContrasena.NuevaContrasena;
        }

        var datosKeycloak = new DatosActualizacionUsuarioIdentidad(
            NombreUsuario: nombreUsuarioKeycloak,
            Correo: correoKeycloak,
            Nombre: nombreKeycloak,
            Apellido: apellidoKeycloak);

        return new ResultadoCambiosUsuario(
            camposActualizados: camposActualizados,
            datosKeycloak: datosKeycloak,
            huboCambiosDatosUsuario: camposActualizados.Count > 0,
            cambiaContrasena: cambiaContrasena,
            nuevaContrasena: nuevaContrasena);
    }
}
