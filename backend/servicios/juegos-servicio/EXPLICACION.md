# Guía de Arquitectura: `juegos-servicio`

Este documento explica qué hace cada clase del microservicio, cómo se relacionan entre sí y por qué están diseñadas así.

---

## Visión general del flujo

Cuando el frontend llama a `POST /api/juegos/trivias`, el recorrido es:

```
HTTP Request
    → TriviasControlador       (recibe la petición)
    → CrearTriviaComando        (empaqueta los datos)
    → MediatR                   (enruta al manejador correcto)
    → CrearTriviaManejador      (ejecuta la lógica)
    → Trivia.Crear(...)         (aplica las reglas de negocio)
    → IRepositorioJuegos        (guarda en la base de datos)
    → HTTP 201 Created          (devuelve el ID de la trivia)
```

---

## Capa 1: Dominio (`JuegosServicio.Dominio`)

> **Regla de oro:** Esta capa no depende de nadie. Cero referencias a Entity Framework, MediatR ni ninguna librería externa. Solo C# puro.

### `Entidades/Trivia.cs` — El Aggregate Root

Es la entidad más importante del microservicio. Un **Aggregate Root** significa que `Trivia` es la "puerta de entrada" para modificar todo lo que tiene adentro (sus `Pregunta`s). Nadie puede agregar o eliminar preguntas sin pasar por los métodos de `Trivia`.

**Propiedades:**
| Propiedad | Descripción |
|---|---|
| `Id` | Identificador único (GUID generado al crear) |
| `Nombre` | Nombre de la trivia, único en el sistema |
| `Descripcion` | Descripción del contenido |
| `CreadorId` | ID del operador que la creó (viene del token JWT) |
| `TiempoLimitePorPregunta` | Segundos que tiene el participante para responder |
| `Estado` | `Borrador` o `Activa` (enum `EstadoTrivia`) |
| `FechaCreacion` | Cuándo fue creada |
| `Preguntas` | Lista de preguntas (solo lectura desde afuera) |
| `Eventos` | Eventos de dominio pendientes de despachar |

**Métodos:**

- `Trivia.Crear(...)` — Método estático factory. Crea una trivia nueva con estado `Borrador` y registra el evento `TriviaCreadaEvento`. Valida que el nombre no esté vacío, que el tiempo sea mayor a cero, etc.

- `AgregarPregunta(...)` — Primero llama a `ValidarEstadoBorrador()`. Si la trivia está `Activa`, lanza `ExcepcionDominio` y corta el flujo. Si está en borrador, crea la `Pregunta` y la agrega a la lista interna `_preguntas`.

- `ModificarPregunta(...)` — Igual: valida estado borrador, busca la pregunta por ID (si no existe lanza `ExcepcionNoEncontrado`), y llama al método `Modificar` de la pregunta.

- `EliminarPregunta(...)` — Valida estado borrador, busca la pregunta, la remueve de `_preguntas`.

- `Trivia.Reconstituir(...)` — Método factory **solo para la capa de infraestructura**. Reconstruye una trivia desde la base de datos sin ejecutar las validaciones de creación (porque el dato ya es válido, ya estaba guardado).

- `ValidarEstadoBorrador(...)` — Método privado que lanza `ExcepcionDominio` si el estado no es `Borrador`. Es llamado por los tres métodos de modificación.

---

### `Entidades/Pregunta.cs`

Representa una pregunta de la trivia. Solo puede ser creada o modificada a través de `Trivia` (sus métodos `internal` impiden que la capa de aplicación la toque directamente).

**Propiedades:**
| Propiedad | Descripción |
|---|---|
| `Id` | Identificador único |
| `TriviaId` | A qué trivia pertenece |
| `Enunciado` | El texto de la pregunta |
| `PuntajeAsignado` | Puntos que otorga responderla correctamente |
| `Opciones` | Lista de opciones de respuesta (solo lectura) |

**Métodos:**

- `Pregunta.Crear(...)` — Crea una pregunta validando que tenga enunciado, al menos 2 opciones, y que al menos una opción sea correcta.

- `Modificar(...)` — Reemplaza el enunciado y **borra y recrea todas las opciones**. No actualiza las existentes una por una, sino que las reemplaza completamente.

- `Pregunta.Reconstituir(...)` — Para reconstruir desde la base de datos.

---

### `Entidades/Opcion.cs`

La unidad más pequeña: una opción de respuesta para una pregunta (ej. "Lima", "Buenos Aires", "Caracas").

**Propiedades:** `Id`, `PreguntaId`, `Texto`, `EsCorrecta` (bool).

- `Opcion.Crear(...)` — Solo valida que el texto no esté vacío.
- `Opcion.Reconstituir(...)` — Para reconstruir desde la base de datos.

---

### `Enums/EstadoTrivia.cs`

```csharp
Borrador = 0   // Recién creada, se pueden agregar/modificar/eliminar preguntas
Activa   = 1   // Publicada, no se puede tocar
Archivada = 2  // Fuera de uso (para futuras HUs)
```

---

### `Eventos/EventoDominio.cs` y `TriviaCreadaEvento.cs`

Los **Eventos de Dominio** son notificaciones internas de que algo importante ocurrió.

- `EventoDominio` — Clase base abstracta. Todo evento tiene un `EventoId` (GUID) y `OcurridoEn` (timestamp).

- `TriviaCreadaEvento` — Se registra dentro de `Trivia.Crear(...)`. Contiene el `TriviaId` y `Nombre` de la trivia recién creada. Actualmente se almacena en `trivia.Eventos` pero no se despacha a RabbitMQ (como indica el manual: en borrador no hace falta notificar al motor de partidas).

---

### `Excepciones/ExcepcionDominio.cs`

Se lanza cuando se viola una **regla de negocio**. Ejemplos:
- Intentar agregar una pregunta a una trivia `Activa`
- Crear una trivia con nombre vacío
- Agregar una pregunta sin opciones correctas

El middleware la captura y devuelve **HTTP 422 Unprocessable Entity**.

### `Excepciones/ExcepcionNoEncontrado.cs`

Se lanza cuando se busca algo que no existe en la base de datos. Ejemplos:
- Intentar modificar una pregunta que no existe en la trivia
- Cargar una trivia por ID que no existe

El middleware la captura y devuelve **HTTP 404 Not Found**.

---

## Capa 2: Commons (`JuegosServicio.Commons`)

> Son los objetos de transferencia de datos (DTOs). No tienen lógica, solo propiedades. Son los "formularios" que el frontend envía y recibe.

### DTOs de Request (lo que el frontend envía)

| Clase | Cuándo se usa | Campos |
|---|---|---|
| `CrearTriviaDto` | `POST /api/juegos/trivias` | `Nombre`, `Descripcion`, `TiempoLimitePorPregunta` |
| `AgregarPreguntaDto` | `POST .../preguntas` | `Enunciado`, `PuntajeAsignado`, `List<OpcionDto>` |
| `ModificarPreguntaDto` | `PUT .../preguntas/{id}` | `NuevoEnunciado`, `List<OpcionDto>` |
| `OpcionDto` | Parte de los anteriores | `Texto`, `EsCorrecta` |

### DTOs de Response (lo que el backend devuelve)

| Clase | Cuándo se usa | Campos |
|---|---|---|
| `TriviaResumenDto` | `GET .../borrador` | Id, Nombre, Descripcion, Estado, TotalPreguntas, FechaCreacion |
| `PreguntaDetalleDto` | Para mostrar preguntas | Id, Enunciado, Puntaje, `List<OpcionDetalleDto>` |
| `OpcionDetalleDto` | Parte del anterior | Id, Texto, EsCorrecta |

---

## Capa 3: Aplicación (`JuegosServicio.Aplicacion`)

> Orquesta el flujo. No tiene lógica de negocio propia (eso es el dominio) ni sabe nada de bases de datos (eso es infraestructura).

### `Puertos/IRepositorioJuegos.cs`

Es una **interfaz** (contrato). La capa de aplicación le dice: "necesito estas operaciones de base de datos" pero no le importa CÓMO se implementan. La implementación real está en la capa de infraestructura.

```
Métodos definidos:
  ExisteTriviaConNombreAsync(nombre)      → ¿Ya existe una trivia con ese nombre?
  AgregarTriviaAsync(trivia)             → Guardar trivia nueva
  ObtenerTriviaPorIdAsync(triviaId)      → Cargar trivia con sus preguntas y opciones
  AgregarPreguntaAsync(triviaId, pregunta) → Guardar pregunta nueva
  ModificarPreguntaAsync(triviaId, pregunta) → Actualizar pregunta
  EliminarPreguntaAsync(triviaId, preguntaId) → Borrar pregunta
  ObtenerTriviasEnBorradorAsync(creadorId) → Lista para el operador (devuelve DTOs directamente)
```

### `Puertos/IProveedorFechaHora.cs`

Abstracción del reloj del sistema. En lugar de llamar `DateTime.UtcNow` directamente (que es imposible de testear), los manejadores piden la fecha a través de esta interfaz. En producción devuelve `DateTime.UtcNow`. En tests se puede inyectar una fecha fija.

---

### Comandos (CQRS — lado de escritura)

Un **Comando** es una intención de cambio. Usa `record` para ser inmutable.

| Comando | Datos que lleva | Retorna |
|---|---|---|
| `CrearTriviaComando` | `CrearTriviaDto` + `CreadorId` (del JWT) | `Guid` (ID de la trivia) |
| `AgregarPreguntaComando` | `TriviaId` + `AgregarPreguntaDto` | `Guid` (ID de la pregunta) |
| `ModificarPreguntaComando` | `TriviaId` + `PreguntaId` + `ModificarPreguntaDto` | nada |
| `EliminarPreguntaComando` | `TriviaId` + `PreguntaId` | nada |

### Consultas (CQRS — lado de lectura)

Una **Consulta** pide datos sin modificar nada.

| Consulta | Datos que lleva | Retorna |
|---|---|---|
| `ObtenerTriviasEnBorradorConsulta` | `OperadorId` (del JWT) | `List<TriviaResumenDto>` |

---

### Manejadores (Handlers)

Cada comando/consulta tiene exactamente un manejador. MediatR los conecta automáticamente.

#### `CrearTriviaManejador`
1. Verifica que no exista otra trivia con el mismo nombre (`_repositorio.ExisteTriviaConNombreAsync`)
2. Llama a `Trivia.Crear(...)` con los datos del comando y la fecha del reloj
3. Guarda con `_repositorio.AgregarTriviaAsync`
4. Retorna el `Guid` de la trivia creada

#### `AgregarPreguntaManejador`
1. Carga la trivia completa con `_repositorio.ObtenerTriviaPorIdAsync` (lanza 404 si no existe)
2. Llama a `trivia.AgregarPregunta(...)` — aquí el **dominio** valida que la trivia esté en borrador y que las opciones sean válidas
3. Guarda la pregunta con `_repositorio.AgregarPreguntaAsync`
4. Retorna el `Guid` de la pregunta creada

#### `ModificarPreguntaManejador`
1. Carga la trivia con sus preguntas (404 si no existe)
2. Llama a `trivia.ModificarPregunta(preguntaId, nuevoEnunciado, nuevasOpciones)` — el dominio valida todo
3. Busca la pregunta ya modificada dentro de `trivia.Preguntas` y la pasa al repositorio para actualizar
4. No retorna nada (HTTP 204)

#### `EliminarPreguntaManejador`
1. Carga la trivia (404 si no existe)
2. Llama a `trivia.EliminarPregunta(preguntaId)` — el dominio valida que esté en borrador y que la pregunta exista
3. Llama a `_repositorio.EliminarPreguntaAsync` para borrarla físicamente de la BD
4. No retorna nada (HTTP 204)

#### `ObtenerTriviasEnBorradorManejador`
Solo delega al repositorio. No pasa por el modelo de dominio porque es una consulta de lectura (siguiendo el patrón CQRS: las lecturas no necesitan pasar por el agregado).

---

### `Dependencias/RegistroAplicacion.cs`

Clase de extensión de `IServiceCollection`. Registra MediatR y le dice: "busca todos los manejadores en este ensamblado". Es llamada desde `Program.cs` con `servicios.AgregarAplicacion()`.

---

## Capa 4: Infraestructura (`JuegosServicio.Infraestructura`)

> Implementa los detalles técnicos. Sabe de bases de datos, SQL, Entity Framework. Esta es la única capa que "ensucia" las manos con infraestructura.

### `Persistencia/Modelos/`

Son clases "planas" que representan las tablas de la base de datos. Son **distintas** de las entidades del dominio porque:
- EF Core necesita clases mutables con setters públicos
- EF Core necesita constructores sin parámetros
- Las entidades del dominio tienen constructores privados y listas readonly

| Modelo EF | Tabla BD | Columnas principales |
|---|---|---|
| `TriviaModelo` | `juegos.Trivia` | id, nombre, descripcion, creador_id, tiempo_limite_por_pregunta, estado (int), fecha_creacion |
| `PreguntaModelo` | `juegos.Pregunta` | id, trivia_id, enunciado, puntaje_asignado |
| `OpcionModelo` | `juegos.Opcion` | id, pregunta_id, texto, es_correcta |

Cada modelo tiene navegaciones: `TriviaModelo` tiene `List<PreguntaModelo> Preguntas`. EF Core las usa para los `Include()` en las consultas.

---

### `Persistencia/ContextoJuegos.cs`

El `DbContext` de Entity Framework Core. Configura cómo mapear los modelos a la base de datos usando **Fluent API** (sin atributos `[Column]` en los modelos, para que los modelos queden limpios).

**Configuraciones relevantes:**
- Schema por defecto: `juegos` (todas las tablas van en el schema `juegos` de PostgreSQL)
- `Trivia.nombre` tiene índice único (no puede haber dos trivias con el mismo nombre)
- `Pregunta` tiene FK `trivia_id` con `OnDelete: Cascade` (si borras la trivia, se borran sus preguntas)
- `Opcion` tiene FK `pregunta_id` con `OnDelete: Cascade` (si borras la pregunta, se borran sus opciones)
- La tabla de historial de migraciones va en `juegos.__historial_migraciones`

---

### `Persistencia/JuegosMapeador.cs`

Traduce entre el mundo del **dominio** y el mundo de la **base de datos**.

| Método | Dirección | Descripción |
|---|---|---|
| `AModelo(Trivia)` | Dominio → BD | Convierte entidad Trivia a TriviaModelo (para guardar) |
| `AModelo(Pregunta)` | Dominio → BD | Convierte Pregunta a PreguntaModelo |
| `AModelo(Opcion)` | Dominio → BD | Convierte Opcion a OpcionModelo |
| `ADominio(TriviaModelo)` | BD → Dominio | Reconstruye Trivia con `Trivia.Reconstituir(...)` |
| `ADominio(PreguntaModelo)` | BD → Dominio | Reconstruye Pregunta con `Pregunta.Reconstituir(...)` |
| `ADominio(OpcionModelo)` | BD → Dominio | Reconstruye Opcion con `Opcion.Reconstituir(...)` |

---

### `Persistencia/RepositorioJuegos.cs`

Implementa `IRepositorioJuegos`. Es donde vive el código de EF Core.

**Método por método:**

- `ExisteTriviaConNombreAsync` — Consulta `AnyAsync` con `AsNoTracking`. Rápida y sin cargar la entidad completa.

- `AgregarTriviaAsync` — Mapea `Trivia` a `TriviaModelo` (incluyendo sus preguntas y opciones), hace `Add` y `SaveChangesAsync`.

- `ObtenerTriviaPorIdAsync` — Usa `Include(...).ThenInclude(...)` para cargar la trivia con todas sus preguntas y opciones en una sola consulta SQL. Luego mapea a dominio con `JuegosMapeador.ADominio`.

- `AgregarPreguntaAsync` — Solo mapea y agrega la pregunta (con sus opciones). No recarga la trivia.

- `ModificarPreguntaAsync` — Primero borra todas las opciones antiguas de la pregunta, luego agrega las nuevas (las opciones se reemplazan completamente, no se hace diff). También actualiza el enunciado del `PreguntaModelo`.

- `EliminarPreguntaAsync` — Busca el `PreguntaModelo` por `id` y `triviaId`. Al borrarlo, el `CASCADE` en la base de datos borra automáticamente sus `OpcionModelo`.

- `ObtenerTriviasEnBorradorAsync` — **Consulta de lectura optimizada.** Usa `AsNoTracking` y proyecta directamente con `Select` a `TriviaResumenDto` sin pasar por las entidades del dominio. Esto genera SQL eficiente y es la forma recomendada para consultas de listado.

---

### `Persistencia/SembradorJuegos.cs`

Existe por consistencia arquitectural. Actualmente no hace nada (el catálogo de juegos no tiene datos semilla), pero es el lugar donde irían datos de prueba o datos iniciales si se necesitan.

### `Tiempo/ProveedorFechaHoraSistema.cs`

Implementa `IProveedorFechaHora`. En producción simplemente retorna `DateTime.UtcNow`.

### `Dependencias/RegistroInfraestructura.cs`

Registra en el contenedor de DI:
- `DbContext` → `ContextoJuegos` (con PostgreSQL, reintentos automáticos en fallo de conexión)
- `IRepositorioJuegos` → `RepositorioJuegos` (Scoped: una instancia por request HTTP)
- `IProveedorFechaHora` → `ProveedorFechaHoraSistema` (Singleton)

---

## Capa 5: API (`JuegosServicio.Presentacion`)

> El punto de entrada HTTP. Recibe requests, los convierte en comandos/consultas, y devuelve respuestas.

### `Controladores/TriviasControlador.cs`

Controlador REST con ruta base `api/juegos/trivias`. Todos los endpoints requieren el rol `Operador` o `Administrador` (política `PoliticaOperador`).

| Método HTTP | Ruta | Acción | HU |
|---|---|---|---|
| `POST` | `/api/juegos/trivias` | Crear trivia → devuelve `201 Created` con el ID | HU15 |
| `GET` | `/api/juegos/trivias/borrador` | Listar trivias en borrador del operador autenticado | HU15 |
| `POST` | `/api/juegos/trivias/{id}/preguntas` | Agregar pregunta → `201 Created` | HU16 |
| `PUT` | `/api/juegos/trivias/{id}/preguntas/{pid}` | Modificar pregunta → `204 No Content` | HU17 |
| `DELETE` | `/api/juegos/trivias/{id}/preguntas/{pid}` | Eliminar pregunta → `204 No Content` | HU17 |

El método privado `ObtenerCreadorId()` extrae el `sub` claim del token JWT para identificar al operador sin necesidad de pasarlo manualmente en el body.

---

### `Middlewares/ManejadorErroresMiddleware.cs`

Intercepta todas las excepciones antes de que lleguen al cliente y las convierte en respuestas JSON con código HTTP apropiado:

| Excepción | Código HTTP | `codigo` en JSON |
|---|---|---|
| `ExcepcionNoEncontrado` | 404 Not Found | `NO_ENCONTRADO` |
| `ExcepcionDominio` | 422 Unprocessable Entity | `REGLA_NEGOCIO` |
| Cualquier otra | 500 Internal Server Error | `ERROR_INTERNO` |

Ejemplo de respuesta de error:
```json
{
  "codigo": "REGLA_NEGOCIO",
  "mensaje": "No se pueden agregar preguntas a una trivia que no está en estado Borrador."
}
```

---

### `Configuraciones/RegistroCors.cs`

Configura los orígenes permitidos para llamadas cross-origin. En desarrollo permite `localhost:3000` (React) y `localhost:5173` (Vite).

### `Configuraciones/RegistroSeguridad.cs`

Configura la validación de tokens JWT de Keycloak. Cuando llega un request con `Authorization: Bearer <token>`, ASP.NET Core valida el token contra el endpoint de Keycloak y extrae los claims (incluyendo el rol del usuario).

Define la política `PoliticaOperador`: solo usuarios con rol `Operador` o `Administrador` pueden acceder.

---

### `Program.cs`

El punto de arranque de la aplicación. En orden:
1. Registra controladores, Swagger, CQRS, infraestructura, seguridad y CORS
2. Configura el pipeline de middlewares (errores → CORS → Auth → Controllers)
3. Al arrancar, ejecuta las migraciones de EF Core automáticamente (`MigrateAsync`)
4. Llama al `SembradorJuegos` (que de momento no hace nada)
5. Expone `/salud` como health check del servicio

---

## Configuraciones externas

### `appsettings.json` (Docker)
```json
ConnectionStrings.BaseDatos → postgres-juegos:5432 (dentro del docker network)
Keycloak.Authority          → http://keycloak:8080/realms/umbral
```

### `appsettings.Development.json` (Local)
```json
ConnectionStrings.BaseDatos → localhost:5435 (puerto expuesto en docker-compose)
Keycloak.Authority          → http://localhost:8080/realms/umbral
```

---

## Docker y Gateway

### `Dockerfile`
Multi-stage build: primero compila con el SDK de .NET 8, luego copia solo el output compilado al runtime de ASP.NET 8. El contenedor escucha en el puerto `8080` internamente.

### `docker-compose.yml` (entradas agregadas)
- `postgres-juegos`: PostgreSQL 16 en el puerto `5435`, base de datos `umbral_juegos`
- `juegos-servicio`: El microservicio en el puerto `5002`, espera que `postgres-juegos` esté listo antes de arrancar

### `api-gateway/appsettings.json` (entradas agregadas)
Las rutas `/api/juegos/trivias` y `/api/juegos/trivias/**` son reenviadas al `juegos-cluster` (`http://juegos-servicio:8080`).

---

## Flujo completo de ejemplo: HU16 Agregar Pregunta

```
1. Frontend envía:
   POST http://localhost:5000/api/juegos/trivias/{triviaId}/preguntas
   Authorization: Bearer <token-operador>
   {
     "enunciado": "¿Capital de Venezuela?",
     "puntajeAsignado": 10,
     "opciones": [
       { "texto": "Caracas", "esCorrecta": true },
       { "texto": "Maracaibo", "esCorrecta": false },
       { "texto": "Valencia", "esCorrecta": false }
     ]
   }

2. API Gateway reenvía a juegos-servicio:8080

3. TriviasControlador.AgregarPregunta():
   - Valida el JWT (middleware de autenticación)
   - Crea AgregarPreguntaComando(triviaId, dto)
   - Lo envía por MediatR

4. AgregarPreguntaManejador.Handle():
   - Llama repositorio.ObtenerTriviaPorIdAsync(triviaId)
   - Si no existe → ExcepcionNoEncontrado → 404
   - Llama trivia.AgregarPregunta("¿Capital de Venezuela?", 10, opciones)

5. Trivia.AgregarPregunta():
   - Llama ValidarEstadoBorrador() → si no es Borrador → ExcepcionDominio → 422
   - Llama Pregunta.Crear(...) → valida opciones, crea Opcion para cada una
   - Agrega la Pregunta a _preguntas
   - Retorna la Pregunta creada

6. AgregarPreguntaManejador.Handle() continúa:
   - Llama repositorio.AgregarPreguntaAsync(triviaId, pregunta)

7. RepositorioJuegos.AgregarPreguntaAsync():
   - JuegosMapeador.AModelo(pregunta) → crea PreguntaModelo + 3 OpcionModelo
   - context.Preguntas.Add(modelo) + SaveChangesAsync()
   - SQL INSERT a juegos.Pregunta y 3 INSERT a juegos.Opcion

8. Respuesta:
   HTTP 201 Created
   { "id": "550e8400-e29b-41d4-a716-446655440000" }
```

---

## Lo que falta para correr el proyecto

Antes de `docker compose up`, necesitas generar la migración inicial de EF Core:

```powershell
# Desde la raíz del proyecto
cd backend/servicios/juegos-servicio

dotnet ef migrations add InicialJuegos `
  --project JuegosServicio.Infraestructura `
  --startup-project JuegosServicio.Presentacion

# Luego volver a la raíz y levantar todo
cd ../../..
docker compose up -d --build
```

La migración crea el schema `juegos` y las tablas `Trivia`, `Pregunta` y `Opcion` en PostgreSQL automáticamente cuando el servicio arranca.
