# Pruebas — aviso de finalización de sesión (móvil)

El frontend móvil no tiene runner de pruebas (no hay Jest/Vitest en
`package.json`). La lógica de deduplicación es una función pura y testeable; aquí
se documentan los casos esperados y la validación manual. La corrección se valida
además con `npm run typecheck`.

## Unidad: `intentarMarcarFinalizacionNotificada(sesionId)`

Archivo: `servicios/notificacionesFinalizacionSesion.ts`

| # | Escenario | Esperado |
|---|-----------|----------|
| 1 | Primera llamada para una sesión | `true` (marca) |
| 2 | Segunda llamada para la misma sesión | `false` (ya notificada) |
| 3 | Mismo id con distinta capitalización/espacios | `false` (se normaliza) |
| 4 | `""` / id vacío | `false` |
| 5 | `reiniciarFinalizacionNotificada(id)` y volver a llamar | `true` de nuevo |

## Comportamiento de los avisos (integración conceptual)

| # | Evento / contexto | Esperado |
|---|-------------------|----------|
| 1 | `SesionActualizada(Finalizada)` individual, participante en cualquier pantalla | Aviso "La sesión finalizó / Ver resultado" **una vez** |
| 2 | `SesionActualizada(Finalizada)` grupal, integrante esperando fuera del detalle | Aviso mostrado por el hook global **una vez** |
| 3 | `Finalizada` recibido por Detalle **y** hook global | Una sola alerta (dedup compartida) |
| 4 | `SesionActualizada(Activa)` | **No** muestra aviso final |
| 5 | `SesionActualizada(Pausada)` | **No** muestra aviso final |
| 6 | `SesionActualizada(EnPreparacion)` | **No** muestra aviso final |
| 7 | "Ver resultado" en grupal | Navega a `/participante/historial/[id]` con `modo=Grupal` |
| 8 | "Ver resultado" en individual | Navega con `modo=Individual` |
| 9 | `Finalizada` | **No** navega a una nueva etapa (`navegarAEjecucionActual` no se invoca) |
| 10 | — | No se toca cálculo de puntaje ni ranking |

## Validación manual

**A. Individual:** completar todas las etapas → aparece "La sesión finalizó",
botón "Ver resultado" abre el historial (modo Individual), una sola alerta.

**B. Grupal (≥2 equipos):** un equipo termina antes y sus integrantes esperan;
el último equipo completa la última etapa → **todos** los inscritos reciben
"La sesión finalizó" / "Puedes revisar tus resultados y el ranking final." /
"Ver resultado", sin refrescar, sin alertas duplicadas, y el resultado abre en
modo **Grupal**.
