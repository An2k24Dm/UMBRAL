namespace SesionesServicio.Dominio.Abstract;

public interface ISesion
{
    void AsignarMisiones(IReadOnlyList<Guid> misionesIds);

    void Preparar();
    void Iniciar(DateTime fechaInicioUtc);
    void Pausar();
    void Reanudar();
    void Finalizar(DateTime fechaFinalizacionUtc);
    void Cancelar();
}
