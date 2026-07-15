const sesionesFinalizacionNotificada = new Set<string>();

function normalizar(sesionId: string): string {
  return sesionId.trim().toLowerCase();
}

export function intentarMarcarFinalizacionNotificada(sesionId: string): boolean {
  const clave = normalizar(sesionId);
  if (!clave || sesionesFinalizacionNotificada.has(clave)) return false;
  sesionesFinalizacionNotificada.add(clave);
  return true;
}

// Solo para pruebas / reinicio explícito. No se usa en el flujo normal (una
// sesión finaliza una única vez).
export function reiniciarFinalizacionNotificada(sesionId?: string): void {
  if (sesionId) sesionesFinalizacionNotificada.delete(normalizar(sesionId));
  else sesionesFinalizacionNotificada.clear();
}
