namespace JuegosServicio.Dominio.Enums;

// Tipos de contenido de juego que el dominio reconoce. Se replica la
// definición que vive en sesiones-servicio (mismo orden, mismos
// nombres) porque ambos microservicios necesitan hablar el mismo
// vocabulario en la frontera HTTP. Mantener los enteros sincronizados
// con SesionesServicio.Dominio.Enums.TipoJuego.
public enum TipoJuego
{
    Trivia = 0,
    BusquedaTesoro = 1
}
