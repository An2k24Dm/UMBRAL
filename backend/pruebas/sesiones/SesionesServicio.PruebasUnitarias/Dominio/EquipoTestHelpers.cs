using System;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Atajo de pruebas: conserva la forma sencilla CrearEquipo(nombre, lider, ...)
// para los casos que solo necesitan un equipo público sin contraseña. El
// método de instancia del dominio toma 6 parámetros (NombreEquipo, TipoEquipo,
// ContrasenaEquipoHash?, ...), por lo que estas llamadas de 4 argumentos
// resuelven a esta extensión sin colisionar con la firma real.
internal static class EquipoTestHelpers
{
    public static Equipo CrearEquipo(
        this SesionGrupal sesion,
        string nombre,
        Guid liderIdentidadId,
        DateTime fechaUnionSesionUtc,
        DateTime fechaUnionEquipoUtc)
        => sesion.CrearEquipo(
            NombreEquipo.Crear(nombre),
            TipoEquipo.Publico,
            null,
            liderIdentidadId,
            fechaUnionSesionUtc,
            fechaUnionEquipoUtc);
}
