using SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionEquipo;

namespace SesionesServicio.Aplicacion.Validaciones;

public sealed class ValidadorAplicarPenalizacionEquipo
    : ValidadorBase<AplicarPenalizacionEquipoComando>
{
    protected override void ValidarSolicitud(
        AplicarPenalizacionEquipoComando comando,
        ResultadoValidacion resultado)
    {
        if (comando.SesionId == Guid.Empty)
            resultado.Agregar("sesionId", "El identificador de la sesión es obligatorio.");

        if (comando.EquipoId == Guid.Empty)
            resultado.Agregar("equipoId", "El identificador del equipo es obligatorio.");

        ValidacionesPenalizacion.ValidarPuntos(comando.Puntos, resultado);
        ValidacionesPenalizacion.ValidarMotivo(comando.Motivo, resultado);
    }
}
