namespace SesionesServicio.Commons.Dtos;

// Tipo de equipo expuesto por la API. Se mantiene separado del enum de
// dominio para no acoplar el contrato HTTP a la capa interna.
public enum TipoEquipoDto
{
    Publico = 0,
    Privado = 1
}

// Solicitud para crear un equipo. La identidad del participante líder NO
// viaja en el body: se obtiene del usuario autenticado. La contraseña en
// texto plano solo existe aquí (entrada) y se hashea antes de tocar el
// agregado; nunca se persiste ni se devuelve.
public sealed class CrearEquipoDto
{
    public string Nombre { get; set; } = string.Empty;
    public TipoEquipoDto Tipo { get; set; }
    public string? Contrasena { get; set; }
}
