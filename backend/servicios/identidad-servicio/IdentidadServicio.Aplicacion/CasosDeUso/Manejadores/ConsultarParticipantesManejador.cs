using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ConsultarParticipantesManejador
    : IRequestHandler<ConsultarParticipantesConsulta, ResultadoPaginadoDto<ParticipanteListadoDto>>
{
    private const int TamanioPaginaFijo = 10;

    private readonly IRepositorioParticipantes _repositorio;

    public ConsultarParticipantesManejador(IRepositorioParticipantes repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ResultadoPaginadoDto<ParticipanteListadoDto>> Handle(
        ConsultarParticipantesConsulta consulta, CancellationToken cancelacion)
    {
        var pagina = consulta.Pagina <= 0 ? 1 : consulta.Pagina;
        var tamanio = consulta.TamanioPagina <= 0 ? TamanioPaginaFijo : consulta.TamanioPagina;
        var orden = NormalizarOrden(consulta.OrdenEstado);

        var participantes = await _repositorio.ConsultarAsync(
            pagina, tamanio, orden, cancelacion);
        var total = await _repositorio.ContarAsync(cancelacion);

        var elementos = participantes
            .Select(p => new ParticipanteListadoDto
            {
                Id = p.Id,
                Alias = p.Alias,
                NombreUsuario = p.NombreUsuario.Valor,
                Nombre = p.NombrePersona.Nombre,
                Apellido = p.NombrePersona.Apellido,
                Estado = p.Estado.ToString(),
                Sexo = p.Sexo.ToString()
            })
            .ToList();

        return new ResultadoPaginadoDto<ParticipanteListadoDto>(
            elementos, pagina, tamanio, total);
    }

    private static string? NormalizarOrden(string? ordenEstado)
    {
        if (string.IsNullOrWhiteSpace(ordenEstado)) return null;
        var valor = ordenEstado.Trim().ToLowerInvariant();
        return valor is "asc" or "desc" ? valor : null;
    }
}
