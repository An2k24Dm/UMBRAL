namespace IdentidadServicio.Aplicacion.Validaciones;

// Catálogo centralizado de mensajes. Mantenerlos aquí evita literales dispersos
// y permite ajustar el texto en un solo lugar.
public static class MensajesValidacionUsuario
{
    // Campos
    public const string CampoNombreUsuario = "nombreUsuario";
    public const string CampoCorreo = "correo";
    public const string CampoContrasena = "contrasena";
    public const string CampoNombre = "nombre";
    public const string CampoApellido = "apellido";
    // Los campos de DatosContacto se reportan con notación punteada para que el
    // frontend pueda mapearlos al input correcto sin ambigüedad.
    public const string CampoTelefono = "datosContacto.telefono";
    public const string CampoDireccion = "datosContacto.direccion";
    public const string CampoFechaNacimiento = "fechaNacimiento";
    public const string CampoSexo = "sexo";
    public const string CampoTipoUsuario = "tipoUsuario";
    // Alias del Participante (HU03 — registro público desde la app móvil).
    public const string CampoAlias = "alias";
    // Codigos OP-### y AD-### los genera el backend; no son campos del DTO,
    // pero conservamos los nombres por si una versión futura los expone como
    // errores específicos (p. ej. choque de duplicado por carrera).
    public const string CampoCodigoOperador = "codigoOperador";
    public const string CampoCodigoAdministrador = "codigoAdministrador";
    // HU09 — cambio administrativo de contraseña desde el panel del
    // Administrador. El DTO de edición expone los dos campos por separado.
    public const string CampoNuevaContrasena = "nuevaContrasena";
    public const string CampoConfirmacionContrasena = "confirmacionContrasena";

    // Nombre de usuario
    public const string NombreUsuarioObligatorio = "El nombre de usuario es obligatorio.";
    public const string NombreUsuarioFormato =
        "El nombre de usuario debe tener entre 4 y 30 caracteres y solo puede contener letras, números, punto o guion bajo.";
    public const string NombreUsuarioDuplicado = "El nombre de usuario ya está registrado.";

    // Correo
    public const string CorreoObligatorio = "El correo es obligatorio.";
    public const string CorreoFormato = "El correo no tiene un formato válido.";
    public const string CorreoDuplicado = "El correo ya está registrado.";

    // Contraseña
    public const string ContrasenaObligatoria = "La contraseña es obligatoria.";
    public const string ContrasenaLongitud = "La contraseña debe tener entre 5 y 10 caracteres.";
    public const string ContrasenaSinNumero = "La contraseña debe contener al menos un número.";
    public const string ContrasenaSinEspecial = "La contraseña debe contener al menos un carácter especial.";
    // HU09 — confirmación de la nueva contraseña.
    public const string ContrasenasNoCoinciden =
        "La nueva contraseña y la confirmación no coinciden.";

    // Nombre
    public const string NombreObligatorio = "El nombre es obligatorio.";
    public const string NombreLongitud = "El nombre debe tener entre 2 y 50 caracteres.";
    public const string NombreSoloLetras = "El nombre solo puede contener letras y espacios.";

    // Apellido
    public const string ApellidoObligatorio = "El apellido es obligatorio.";
    public const string ApellidoLongitud = "El apellido debe tener entre 2 y 50 caracteres.";
    public const string ApellidoSoloLetras = "El apellido solo puede contener letras y espacios.";

    // Dirección
    public const string DireccionObligatoria = "La dirección es obligatoria.";
    public const string DireccionLongitud = "La dirección debe tener al menos 5 caracteres.";

    // Teléfono
    public const string TelefonoObligatorio = "El teléfono es obligatorio.";
    public const string TelefonoSoloNumeros = "El teléfono debe contener solo números.";
    public const string TelefonoLongitud = "El teléfono debe tener 11 dígitos.";
    public const string TelefonoCodigoInvalido =
        "El teléfono debe comenzar con un código válido, por ejemplo 0414, 0212, 0424 o 0412.";
    public const string TelefonoDuplicado = "El teléfono ya está registrado.";

    // Fecha de nacimiento
    public const string FechaNacimientoObligatoria = "La fecha de nacimiento es obligatoria.";
    public const string FechaNacimientoFutura = "La fecha de nacimiento no puede ser futura.";
    public const string EdadMinima = "El usuario debe tener al menos 18 años.";
    public const string EdadMaxima = "El usuario no puede tener más de 100 años.";

    // Sexo
    public const string SexoInvalido = "El sexo no es válido.";

    // Tipo de usuario
    public const string TipoUsuarioInvalidoWeb = "El rol seleccionado no es válido para registro web.";

    // Alias del Participante
    public const string AliasObligatorio = "El alias es obligatorio.";
    public const string AliasLongitud = "El alias debe tener entre 6 y 15 caracteres.";
    public const string AliasFormato =
        "El alias solo puede contener letras, números y guion bajo.";
    public const string AliasDuplicado = "El alias ya está registrado.";
}
