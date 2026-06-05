using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ModificarMisionManejador : IRequestHandler<ModificarMisionComando>
{
    private readonly IRepositorioMisiones _repositorio;
    private readonly IValidador<ModificarMisionComando> _validador;

    public ModificarMisionManejador(
        IRepositorioMisiones repositorio,
        IValidador<ModificarMisionComando> validador)
    {
        _repositorio = repositorio;
        _validador = validador;
    }

    public async Task Handle(ModificarMisionComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        var mision = await _repositorio.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        mision.Modificar(comando.Dto.Nombre, comando.Dto.Descripcion, (NivelDificultad)comando.Dto.Dificultad);
        await _repositorio.ActualizarMisionAsync(mision, cancelacion);
    }
}
