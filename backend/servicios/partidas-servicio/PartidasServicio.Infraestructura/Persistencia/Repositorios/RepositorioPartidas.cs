using Microsoft.EntityFrameworkCore;
using PartidasServicio.Dominio.Abstract;
using PartidasServicio.Dominio.Entidades;
using PartidasServicio.Infraestructura.Persistencia.Modelos;

namespace PartidasServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioPartidas : IRepositorioPartidas
{
    private readonly ContextoPartidas _contexto;

    public RepositorioPartidas(ContextoPartidas contexto) => _contexto = contexto;

    public async Task<Partida?> ObtenerPorSesionIdAsync(Guid sesionId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Partidas
            .FirstOrDefaultAsync(p => p.SesionId == sesionId, cancelacion);

        return modelo is null ? null : Mapear(modelo);
    }

    public async Task GuardarAsync(Partida partida, CancellationToken cancelacion)
    {
        var existente = await _contexto.Partidas.FindAsync([partida.Id], cancelacion);

        if (existente is null)
        {
            await _contexto.Partidas.AddAsync(new PartidaModelo
            {
                Id = partida.Id,
                SesionId = partida.SesionId,
                Estado = partida.NombreEstado,
                FechaCreacionUtc = partida.FechaCreacionUtc,
                FechaInicioUtc = partida.FechaInicioUtc,
                FechaFinUtc = partida.FechaFinUtc
            }, cancelacion);
        }
        else
        {
            existente.Estado = partida.NombreEstado;
            existente.FechaInicioUtc = partida.FechaInicioUtc;
            existente.FechaFinUtc = partida.FechaFinUtc;
        }
    }

    private static Partida Mapear(PartidaModelo m) =>
        Partida.Reconstituir(m.Id, m.SesionId, m.Estado, m.FechaCreacionUtc, m.FechaInicioUtc, m.FechaFinUtc);
}
