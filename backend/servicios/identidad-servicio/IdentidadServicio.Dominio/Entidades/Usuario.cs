using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.Dominio.Entidades;

// Clase base abstracta del dominio.
// - NombreUsuario es el username de Keycloak (p. ej. "operador01") y NO es el
//   correo. Se valida como identificador alfanumérico.
// - Correo es el email del usuario (p. ej. "operador@umbral.com"). Es un VO
//   independiente y NO está dentro de DatosContacto.
// - El dominio NO conoce IdKeycloak (vive en UsuarioModelo).
// - La fecha de registro se recibe por parámetro desde IProveedorFechaHora.
public abstract class Usuario
{
    public Guid Id { get; protected set; }
    public NombreUsuario NombreUsuario { get; protected set; } = default!;
    public Correo Correo { get; protected set; } = default!;
    public RolUsuario Rol { get; protected set; }
    public EstadoUsuario Estado { get; protected set; }
    public DateTime FechaRegistro { get; protected set; }

    public NombrePersona NombrePersona { get; protected set; } = default!;
    public DatosContacto DatosContacto { get; protected set; } = default!;
    public SexoPersona Sexo { get; protected set; }
    public DateTime FechaNacimiento { get; protected set; }

    protected Usuario() { }

    protected Usuario(
        Guid id,
        NombreUsuario nombreUsuario,
        Correo correo,
        RolUsuario rol,
        EstadoUsuario estado,
        DateTime fechaRegistro,
        NombrePersona nombrePersona,
        DatosContacto datosContacto,
        SexoPersona sexo,
        DateTime fechaNacimiento)
    {
        if (nombreUsuario is null)
            throw new DatosUsuarioInvalidosExcepcion("El nombre de usuario es obligatorio.");
        if (correo is null)
            throw new DatosUsuarioInvalidosExcepcion("El correo es obligatorio.");
        if (!Enum.IsDefined(typeof(RolUsuario), rol))
            throw new RolNoValidoExcepcion();
        if (nombrePersona is null)
            throw new DatosUsuarioInvalidosExcepcion("Los datos de persona son obligatorios.");
        if (fechaNacimiento == default || fechaNacimiento > fechaRegistro)
            throw new DatosUsuarioInvalidosExcepcion("La fecha de nacimiento no es válida.");

        Id = id;
        NombreUsuario = nombreUsuario;
        Correo = correo;
        Rol = rol;
        Estado = estado;
        FechaRegistro = fechaRegistro;
        NombrePersona = nombrePersona;
        DatosContacto = datosContacto
            ?? throw new DatosUsuarioInvalidosExcepcion("Los datos de contacto son obligatorios.");
        Sexo = sexo;
        FechaNacimiento = fechaNacimiento;
    }

    public void ValidarPuedeIniciarSesion()
    {
        if (Estado != EstadoUsuario.Activo)
            throw new CuentaDesactivadaExcepcion();
        if (!Enum.IsDefined(typeof(RolUsuario), Rol))
            throw new RolNoValidoExcepcion();
    }

    public bool PuedeIniciarSesion() =>
        Estado == EstadoUsuario.Activo && Enum.IsDefined(typeof(RolUsuario), Rol);

    public void Desactivar() => Estado = EstadoUsuario.Inactivo;
    public void Activar() => Estado = EstadoUsuario.Activo;

    // HU09 — métodos de actualización parcial. Cada método valida (a través de
    // los objetos de valor existentes) y reemplaza únicamente el campo recibido.
    // Estado, Rol, FechaRegistro y Id no se modifican en esta HU: por eso no
    // hay setters públicos para ellos.
    public void ActualizarNombreUsuario(NombreUsuario nuevoNombreUsuario)
    {
        if (nuevoNombreUsuario is null)
            throw new DatosUsuarioInvalidosExcepcion("El nombre de usuario es obligatorio.");
        NombreUsuario = nuevoNombreUsuario;
    }

    public void ActualizarCorreo(Correo nuevoCorreo)
    {
        if (nuevoCorreo is null)
            throw new DatosUsuarioInvalidosExcepcion("El correo es obligatorio.");
        Correo = nuevoCorreo;
    }

    public void ActualizarNombrePersona(NombrePersona nuevoNombrePersona)
    {
        if (nuevoNombrePersona is null)
            throw new DatosUsuarioInvalidosExcepcion("Los datos de persona son obligatorios.");
        NombrePersona = nuevoNombrePersona;
    }

    public void ActualizarDatosContacto(DatosContacto nuevosDatosContacto)
    {
        if (nuevosDatosContacto is null)
            throw new DatosUsuarioInvalidosExcepcion("Los datos de contacto son obligatorios.");
        DatosContacto = nuevosDatosContacto;
    }

    public void ActualizarSexo(SexoPersona nuevoSexo)
    {
        if (!Enum.IsDefined(typeof(SexoPersona), nuevoSexo))
            throw new DatosUsuarioInvalidosExcepcion("El sexo no es válido.");
        Sexo = nuevoSexo;
    }

    public void ActualizarFechaNacimiento(DateTime nuevaFechaNacimiento)
    {
        if (nuevaFechaNacimiento == default || nuevaFechaNacimiento > FechaRegistro)
            throw new DatosUsuarioInvalidosExcepcion("La fecha de nacimiento no es válida.");
        FechaNacimiento = nuevaFechaNacimiento;
    }
}
