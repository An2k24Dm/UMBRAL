// Canal ligero (pub/sub en memoria) para avisar que la membresía del
// participante cambió: ingresó a una sesión por código, creó/ingresó a un
// equipo, etc. El hook global de SignalR (useAvisosSesionTiempoReal) reacciona
// re-sincronizando los grupos de la conexión EXISTENTE (UnirseASesion /
// UnirseAEquipo) — sin crear otra HubConnection ni reiniciar la app.

type SuscriptorMembresia = () => void;

const suscriptores = new Set<SuscriptorMembresia>();

// Lo llaman los flujos de ingreso tras un ingreso/creación exitosos.
export function notificarMembresiaTiempoRealActualizada(): void {
  for (const suscriptor of [...suscriptores]) {
    try {
      suscriptor();
    } catch {
      // Aislar el fallo de un suscriptor para no romper a los demás.
    }
  }
}

// Lo usa el hook global para reaccionar. Devuelve una función para desuscribirse.
export function suscribirMembresiaTiempoReal(cb: SuscriptorMembresia): () => void {
  suscriptores.add(cb);
  return () => {
    suscriptores.delete(cb);
  };
}
