namespace JuegosServicio.Infraestructura.Persistencia;

// Por ahora el catálogo de juegos no requiere datos semilla.
// Este seeder existe para mantener la consistencia estructural con el resto
// de microservicios y para facilitar la incorporación de datos de prueba futuros.
public static class SembradorJuegos
{
    public static Task SembrarAsync(ContextoJuegos contexto, CancellationToken cancelacion)
    {
        return Task.CompletedTask;
    }
}
