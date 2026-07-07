using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartidasServicio.Aplicacion.Puertos;

namespace PartidasServicio.Presentacion.Controladores;

[ApiController]
[Route("api/partidas/sesiones/{sesionId:guid}")]
[Authorize(Policy = "PoliticaAdministradorUOperador")]
public sealed class GestionPartidaControlador : ControllerBase
{
    private readonly IServicioPartidas _servicio;

    public GestionPartidaControlador(IServicioPartidas servicio) => _servicio = servicio;

    [HttpPost("iniciar")]
    public async Task<IActionResult> Iniciar(Guid sesionId, CancellationToken cancelacion)
    {
        await _servicio.IniciarPartidaAsync(sesionId, cancelacion);
        return Ok();
    }

    [HttpPost("pausar")]
    public async Task<IActionResult> Pausar(Guid sesionId, CancellationToken cancelacion)
    {
        await _servicio.PausarPartidaAsync(sesionId, cancelacion);
        return Ok();
    }

    [HttpPost("reanudar")]
    public async Task<IActionResult> Reanudar(Guid sesionId, CancellationToken cancelacion)
    {
        await _servicio.ReanudarPartidaAsync(sesionId, cancelacion);
        return Ok();
    }

    [HttpPost("finalizar")]
    public async Task<IActionResult> Finalizar(Guid sesionId, CancellationToken cancelacion)
    {
        await _servicio.FinalizarPartidaAsync(sesionId, cancelacion);
        return Ok();
    }

    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar(Guid sesionId, CancellationToken cancelacion)
    {
        await _servicio.CancelarPartidaAsync(sesionId, cancelacion);
        return Ok();
    }
}
