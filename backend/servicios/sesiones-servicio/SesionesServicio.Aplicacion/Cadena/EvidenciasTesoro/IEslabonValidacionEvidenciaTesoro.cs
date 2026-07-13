namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

// Handler del patrón Chain of Responsibility para la validación estructural de
// una evidencia de Búsqueda del Tesoro. Cada eslabón procesa su validación y,
// si pasa, delega en el siguiente mediante la referencia enlazada.
public interface IEslabonValidacionEvidenciaTesoro
{
    // Enlaza el siguiente eslabón y lo devuelve para poder encadenar llamadas
    // (estilo builder): a.EstablecerSiguiente(b).EstablecerSiguiente(c)...
    IEslabonValidacionEvidenciaTesoro EstablecerSiguiente(
        IEslabonValidacionEvidenciaTesoro siguiente);

    // Ejecuta este eslabón y, si no detiene la cadena, el resto de la cadena.
    Task ManejarAsync(
        ContextoValidacionEvidenciaTesoro contexto,
        CancellationToken cancelacion);
}
