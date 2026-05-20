using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.ObjetosDeValor;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

// Helper interno reutilizable para construir instancias de dominio dentro de
// las pruebas unitarias del mapeo de perfil. Mantiene fechas y datos fijos
// (sin aleatoriedad) y respeta las fábricas/constructores actuales del dominio
// (NombrePersona.Crear, DatosContacto.Crear, etc.).
internal static class UsuariosDePrueba
{
    public static readonly DateTime FechaRegistro =
        new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);

    public static readonly DateTime FechaNacimiento =
        new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public const string Direccion = "Av. Bolívar, Caracas";
    public const string Telefono = "04143710260";

    public static Administrador NuevoAdministrador(
        string nombreUsuario = "admin_umbral",
        string correo = "admin@umbral.com",
        string nombre = "Ada",
        string apellido = "Admin",
        SexoPersona sexo = SexoPersona.Femenino,
        string? codigoAdministrador = "AD-001")
    {
        return Administrador.Crear(
            nombreUsuario: NombreUsuario.Crear(nombreUsuario),
            correo: Correo.Crear(correo),
            nombrePersona: NombrePersona.Crear(nombre, apellido),
            datosContacto: DatosContacto.Crear(Direccion, Telefono),
            sexo: sexo,
            fechaNacimiento: FechaNacimiento,
            codigoAdministrador: codigoAdministrador,
            fechaRegistro: FechaRegistro);
    }

    public static Operador NuevoOperador(
        string nombreUsuario = "operador01",
        string correo = "operador@umbral.com",
        string nombre = "Olivia",
        string apellido = "Operadora",
        SexoPersona sexo = SexoPersona.Femenino,
        string codigoOperador = "OP-001")
    {
        return Operador.Crear(
            nombreUsuario: NombreUsuario.Crear(nombreUsuario),
            correo: Correo.Crear(correo),
            nombrePersona: NombrePersona.Crear(nombre, apellido),
            datosContacto: DatosContacto.Crear(Direccion, Telefono),
            sexo: sexo,
            fechaNacimiento: FechaNacimiento,
            codigoOperador: codigoOperador,
            fechaRegistro: FechaRegistro);
    }

    public static Participante NuevoParticipante(
        string nombreUsuario = "participante01",
        string correo = "participante@umbral.com",
        string nombre = "Pablo",
        string apellido = "Participante",
        SexoPersona sexo = SexoPersona.Masculino,
        string alias = "pablito")
    {
        return Participante.Crear(
            nombreUsuario: NombreUsuario.Crear(nombreUsuario),
            correo: Correo.Crear(correo),
            nombrePersona: NombrePersona.Crear(nombre, apellido),
            datosContacto: DatosContacto.Crear(Direccion, Telefono),
            sexo: sexo,
            fechaNacimiento: FechaNacimiento,
            alias: alias,
            fechaRegistro: FechaRegistro);
    }
}
