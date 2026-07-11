export class CronometroActivo {
  private acumuladoMs = 0;
  private instanteInicio: number | null = null;

  // Reinicia el cronómetro para una nueva pregunta/etapa (queda detenido).
  reiniciar(): void {
    this.acumuladoMs = 0;
    this.instanteInicio = null;
  }

  // Comienza o reanuda el conteo activo. Idempotente si ya está corriendo.
  reanudar(ahora: number = Date.now()): void {
    if (this.instanteInicio === null) {
      this.instanteInicio = ahora;
    }
  }

  // Congela el conteo acumulando el tramo activo transcurrido. Idempotente si
  // ya está pausado.
  pausar(ahora: number = Date.now()): void {
    if (this.instanteInicio !== null) {
      this.acumuladoMs += ahora - this.instanteInicio;
      this.instanteInicio = null;
    }
  }

  // Tiempo activo total transcurrido (acumulado + tramo activo en curso).
  transcurridoMs(ahora: number = Date.now()): number {
    const enCurso = this.instanteInicio !== null ? ahora - this.instanteInicio : 0;
    return this.acumuladoMs + enCurso;
  }

  get estaCorriendo(): boolean {
    return this.instanteInicio !== null;
  }
}
