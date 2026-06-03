namespace SesionesServicio.Dominio.Abstract;

// Contrato común de cualquier Sesion (individual o grupal).
//
// Declara únicamente comportamiento — sin propiedades, sin campos, sin
// modo, sin estado persistible. El estado (datos comunes) vive en la
// clase abstracta Sesion. El polimorfismo de población (participantes
// vs equipos) lo aportan las clases hijas, no esta interfaz.
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
