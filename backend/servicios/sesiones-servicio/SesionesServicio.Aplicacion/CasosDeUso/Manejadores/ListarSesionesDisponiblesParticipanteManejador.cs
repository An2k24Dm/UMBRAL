using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ListarSesionesDisponiblesParticipanteManejador
    : IRequestHandler<ListarSesionesDisponiblesParticipanteConsulta,
        List<SesionDisponibleMovilDto>>
{
    private readonly IConsultasSesiones _repositorio;
    private readonly FabricaMapeadorSesionDisponibleMovil _fabricaMapeador;

    public ListarSesionesDisponiblesParticipanteManejador(
        IConsultasSesiones repositorio,
        FabricaMapeadorSesionDisponibleMovil fabricaMapeador)
    {
        _repositorio = repositorio;
        _fabricaMapeador = fabricaMapeador;
    }

    public async Task<List<SesionDisponibleMovilDto>> Handle(
        ListarSesionesDisponiblesParticipanteConsulta consulta,
        CancellationToken cancelacion)
    {
        var modoNormalizado = consulta.Modo;
        if (string.IsNullOrWhiteSpace(modoNormalizado)
            || string.Equals(modoNormalizado, "Todas", StringComparison.OrdinalIgnoreCase))
        {
            modoNormalizado = null;
        }

        var sesiones = await _repositorio.ListarDisponiblesParaParticipanteAsync(
            consulta.Busqueda, modoNormalizado, cancelacion);

        // Las capacidades por tipo las resuelve la estrategia compatible; el
        // manejador no conoce SesionIndividual ni SesionGrupal.
        return sesiones.Select(_fabricaMapeador.Mapear).ToList();
    }
}
