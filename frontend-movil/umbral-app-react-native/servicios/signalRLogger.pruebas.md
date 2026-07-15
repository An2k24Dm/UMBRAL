# Pruebas de clasificación de errores SignalR (móvil)

El frontend móvil no tiene runner de pruebas (no hay Jest/Vitest en
`package.json`). Según el requerimiento, las funciones de clasificación se
mantienen **puras y testeables**, se validan con `npm run typecheck` y aquí se
documentan los casos esperados para verificación manual/futura.

Funciones bajo prueba (`servicios/signalRLogger.ts`):

- `esErrorSignalRTransitorio(mensaje)`
- `esErrorSignalRFatal(mensaje)`
- `esErrorSignalRAutenticacion(mensaje)`
- `esInicioCanceladoDuranteNegociacion(mensaje)`
- `clasificarErrorSignalR(mensaje)`
- `crearLoggerSignalRMovil(etiqueta)`

Y a nivel de objeto de error (`servicios/sesionesTiempoReal.ts`):

- `esErrorConexionTransitorioTiempoReal(error)`
- `clasificarErrorConexionTiempoReal(error)`
- `registrarErrorConexionTiempoRealDev(error, contexto)`

## Casos de clasificación de mensaje

| # | Mensaje de entrada | `clasificarErrorSignalR` esperado |
|---|---|---|
| 1 | `Server timeout elapsed without receiving a message from the server.` | `transitorio` |
| 2 | `Connection disconnected with error: 'Error: Server timeout elapsed...'` | `transitorio` |
| 3 | `WebSocket closed with status code: 1001.` | `transitorio` |
| 4 | `Stream end encountered` | `transitorio` |
| 5 | `Network request failed` | `transitorio` |
| 6 | `Failed to complete negotiation with the server` | `transitorio` |
| 7 | `Connection was stopped during negotiation.` | `cancelacion` (ignorado) |
| 8 | `Status code '401' ... Unauthorized` | `autenticacion` |
| 9 | `Something unexpected went terribly wrong` | `fatal` |

## Comportamiento de `crearLoggerSignalRMovil`

| # | nivel | mensaje | Efecto esperado |
|---|---|---|---|
| A | `Error` | Server timeout (transitorio) | **NO** `console.error`; `console.warn` solo en `__DEV__` |
| B | `Error` | WebSocket 1001 (transitorio) | **NO** `console.error`; `console.warn` solo en `__DEV__` |
| C | `Error` | Stream end (transitorio) | **NO** `console.error` |
| D | `Error` | Network request failed (transitorio) | **NO** `console.error` |
| E | `Error` | Connection stopped during negotiation (cleanup) | Ignorado (sin log) |
| F | `Error` | 401 Unauthorized | `console.error` (real) |
| G | `Error` | mensaje desconocido grave | `console.error` (real) |
| H | `Warning` | cualquiera | `console.warn` |
| I | `Information` | cualquiera | `console.info` solo en `__DEV__` |

Puntos clave del requerimiento cubiertos:

- Caso 1/A/8/9: **un timeout transitorio NO llama `console.error`** (8) y un
  **error fatal sí lo hace** (9).
- Caso F: un 401 se clasifica como `autenticacion`, nunca como timeout; el hook
  ejecuta el flujo de seguridad (`cerrarSesion`) por separado.
- Caso E: la cancelación por desmontaje se ignora en silencio.

## Verificación manual asociada

1. Apagar `sesiones-servicio` unos segundos → la app registra un `warn`
   transitorio (solo en dev), no muestra `Alert`/modal, y reconecta al volver.
2. Apagar el Gateway unos segundos → mismo comportamiento.
3. Token expirado/inválido → NO se trata como timeout; se ejecuta la seguridad
   (cierre de sesión / redirección) existente.
4. Error inesperado real → sigue apareciendo como `console.error`.
5. Reiniciar `ranking-servicio` → el hook de ranking reintenta y hace refetch.
