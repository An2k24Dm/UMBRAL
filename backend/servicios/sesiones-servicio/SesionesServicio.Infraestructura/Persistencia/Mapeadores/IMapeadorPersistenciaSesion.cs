using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

// Strategy de persistencia por tipo de sesión. Cubre las dos direcciones:
//   * CompletarModelo: agrega al SesionModelo la parte específica del tipo
//     (participantes individuales o equipos + sus integrantes).
//   * HaciaDominio: reconstruye el agregado concreto desde el modelo.
// La columna `tipo_sesion` actúa como discriminador. Agregar un tipo nuevo
// solo requiere una nueva estrategia, sin tocar el repositorio.
public interface IMapeadorPersistenciaSesion
{
    bool Soporta(string tipoSesion);

    void CompletarModelo(Sesion sesion, SesionModelo modelo);

    Sesion HaciaDominio(SesionModelo modelo, IReadOnlyList<SesionMision> misiones);
}
