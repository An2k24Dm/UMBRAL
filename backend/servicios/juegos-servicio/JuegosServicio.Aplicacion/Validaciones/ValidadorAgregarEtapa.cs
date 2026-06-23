using JuegosServicio.Aplicacion.Comandos.AgregarEtapa;

namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ValidadorAgregarEtapa : ValidadorBase<AgregarEtapaComando>
{
    protected override void ValidarSolicitud(AgregarEtapaComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Dto;

        if (dto.TipoModoDeJuego < 0 || dto.TipoModoDeJuego > 1)
            resultado.Agregar("tipoModoDeJuego", "El tipo debe ser 0 (Trivia) o 1 (BusquedaTesoro).");

        if (dto.ModoDeJuegoId == Guid.Empty)
            resultado.Agregar("modoDeJuegoId", "El identificador del modo de juego es obligatorio.");
    }
}
