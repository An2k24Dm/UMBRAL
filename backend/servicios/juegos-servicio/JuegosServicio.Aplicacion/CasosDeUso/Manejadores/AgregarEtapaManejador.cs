using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class AgregarEtapaManejador : IRequestHandler<AgregarEtapaComando, Guid>
{
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IRepositorioBusquedas _repositorioBusquedas;
    private readonly IRepositorioJuegos _repositorioJuegos;
    private readonly IValidador<AgregarEtapaComando> _validador;

    public AgregarEtapaManejador(
        IRepositorioMisiones repositorioMisiones,
        IRepositorioBusquedas repositorioBusquedas,
        IRepositorioJuegos repositorioJuegos,
        IValidador<AgregarEtapaComando> validador)
    {
        _repositorioMisiones = repositorioMisiones;
        _repositorioBusquedas = repositorioBusquedas;
        _repositorioJuegos = repositorioJuegos;
        _validador = validador;
    }

    public async Task<Guid> Handle(AgregarEtapaComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        var mision = await _repositorioMisiones.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        var tipo = (TipoModoDeJuego)comando.Dto.TipoModoDeJuego;
        await ValidarModoDeJuegoActivoAsync(tipo, comando.Dto.ModoDeJuegoId, cancelacion);

        var etapa = mision.AgregarEtapa(tipo, comando.Dto.ModoDeJuegoId);
        await _repositorioMisiones.AgregarEtapaAsync(etapa, cancelacion);
        return etapa.Id;
    }

    private async Task ValidarModoDeJuegoActivoAsync(
        TipoModoDeJuego tipo, Guid id, CancellationToken cancelacion)
    {
        if (tipo == TipoModoDeJuego.BusquedaTesoro)
        {
            var busqueda = await _repositorioBusquedas.ObtenerBusquedaPorIdAsync(id, cancelacion)
                ?? throw new ExcepcionNoEncontrado(
                    $"No se encontró la búsqueda del tesoro con ID '{id}'.");
            if (busqueda.Estado != EstadoBusqueda.Activa)
                throw new ExcepcionDominio(
                    "Solo se pueden agregar búsquedas activas como modo de juego de una etapa.");
        }
        else
        {
            var trivia = await _repositorioJuegos.ObtenerTriviaPorIdAsync(id, cancelacion)
                ?? throw new ExcepcionNoEncontrado(
                    $"No se encontró la trivia con ID '{id}'.");
            if (trivia.Estado != EstadoTrivia.Activa)
                throw new ExcepcionDominio(
                    "Solo se pueden agregar trivias activas como modo de juego de una etapa.");
        }
    }
}
