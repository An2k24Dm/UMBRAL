using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Comandos.LiberarPista;

// PROXY PATTERN: este manejador actúa como proxy para la liberación de pistas.
// Controla el acceso validando: sesión activa, no duplicados, y coherencia (pistaId o contenido).
public sealed class LiberarPistaManejador : IRequestHandler<LiberarPistaComando>
{
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IRepositorioPistasLiberadas _repositorioPistas;
    private readonly INotificadorSesionesTiempoReal _notificador;

    public LiberarPistaManejador(
        IRepositorioSesiones repositorioSesiones,
        IRepositorioPistasLiberadas repositorioPistas,
        INotificadorSesionesTiempoReal notificador)
    {
        _repositorioSesiones = repositorioSesiones;
        _repositorioPistas = repositorioPistas;
        _notificador = notificador;
    }

    public async Task Handle(LiberarPistaComando comando, CancellationToken cancelacion)
    {
        // PROXY: validar que la sesión está activa antes de permitir la operación.
        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new InvalidOperationException("Sesión no encontrada.");

        if (sesion.Estado != EstadoSesion.Activa)
            throw new InvalidOperationException(
                "Solo se pueden liberar pistas en sesiones activas.");

        string contenido;

        if (comando.PistaId.HasValue)
        {
            // PROXY: control de duplicidad — no se puede liberar la misma pista dos veces.
            var yaLiberada = await _repositorioPistas.ExistePistaLiberadaAsync(
                comando.SesionId, comando.EtapaId, comando.PistaId.Value, cancelacion);
            if (yaLiberada)
                throw new InvalidOperationException(
                    "Esta pista ya fue liberada en esta etapa de la sesión.");

            if (string.IsNullOrWhiteSpace(comando.Contenido))
                throw new InvalidOperationException(
                    "El contenido de la pista es requerido.");

            contenido = comando.Contenido;
        }
        else
        {
            // Pista personalizada: solo requiere contenido.
            if (string.IsNullOrWhiteSpace(comando.Contenido))
                throw new InvalidOperationException(
                    "Debe indicar el contenido de la pista personalizada.");

            contenido = comando.Contenido;
        }

        await _repositorioPistas.AgregarAsync(new PistaLiberadaRegistro(
            SesionId: comando.SesionId,
            EtapaId: comando.EtapaId,
            PistaId: comando.PistaId,
            Contenido: contenido,
            FechaLiberacionUtc: DateTime.UtcNow),
            cancelacion);

        // Notificar en tiempo real a todos los participantes de la sesión.
        await _notificador.NotificarPistaLiberadaAsync(
            comando.SesionId, comando.EtapaId,
            comando.PistaId, contenido, cancelacion);
    }
}
