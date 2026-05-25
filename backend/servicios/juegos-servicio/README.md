# UMBRAL — HU15 (Crear Trivia) + HU16 (Agregar Pregunta) + HU17 (Modificar / Eliminar Pregunta)

Microservicio `juegos-servicio` que aplica **microservicios + Clean Architecture + CQRS con MediatR + EF Core** para las historias de usuario **HU15, HU16 y HU17** de la plataforma UMBRAL.

---

## 1. Idea clave: dominio ≠ base de datos

| Dominio (`JuegosServicio.Dominio`) | Persistencia (`JuegosServicio.Infraestructura`) |
|---|---|
| `Trivia` (Aggregate Root) | `TriviaModelo` (tabla `Trivia`) |
| `Pregunta` | `PreguntaModelo` (tabla `Pregunta`) |
| `Opcion` | `OpcionModelo` (tabla `Opcion`) |
| Propiedades con `private set`, listas `readonly` | Propiedades con `set` públicos (EF Core las necesita) |
| Constructores privados, métodos de fábrica (`Crear`, `Reconstituir`) | Sin constructores especiales |
| Lanza `ExcepcionDominio` si viola una regla de negocio | Sin lógica de negocio |

- Dominio **no conoce EF Core**: las entidades de dominio no tienen atributos ni referencias a `Microsoft.EntityFrameworkCore`.
- El único puente entre capas es `JuegosMapeador.cs` (dominio ↔ modelos).
- Tablas en schema **`juegos`**, nombres en singular (`Trivia`, `Pregunta`, `Opcion`).
- **`IProveedorFechaHora`** abstrae el reloj. Sólo `ProveedorFechaHoraSistema` usa `DateTime.UtcNow`.
- **Sin `FechaActualizacion`** en ningún archivo.

---

## 2. Reglas de negocio críticas del dominio

Las reglas viven **dentro de las entidades**, no en los controladores ni en los manejadores:

| Regla | Dónde se aplica | Excepción que lanza |
|---|---|---|
| Nombre de trivia no puede estar vacío | `Trivia.Crear(...)` | `ExcepcionDominio` |
| Tiempo límite debe ser mayor a cero | `Trivia.Crear(...)` | `ExcepcionDominio` |
| Solo se pueden agregar preguntas a trivias en **Borrador** | `Trivia.AgregarPregunta(...)` | `ExcepcionDominio` |
| Solo se pueden modificar preguntas de trivias en **Borrador** | `Trivia.ModificarPregunta(...)` | `ExcepcionDominio` |
| Solo se pueden eliminar preguntas de trivias en **Borrador** | `Trivia.EliminarPregunta(...)` | `ExcepcionDominio` |
| Una pregunta debe tener al menos 2 opciones | `Pregunta.Crear(...)` | `ExcepcionDominio` |
| Al menos una opción debe ser correcta | `Pregunta.Crear(...)` | `ExcepcionDominio` |
| El puntaje asignado debe ser mayor a cero | `Pregunta.Crear(...)` | `ExcepcionDominio` |

El middleware `ManejadorErroresMiddleware` captura estas excepciones y las convierte en respuestas HTTP:

| Excepción | Código HTTP | `codigo` en JSON |
|---|---|---|
| `ExcepcionDominio` | `422 Unprocessable Entity` | `REGLA_NEGOCIO` |
| `ExcepcionNoEncontrado` | `404 Not Found` | `NO_ENCONTRADO` |
| Cualquier otra | `500 Internal Server Error` | `ERROR_INTERNO` |

Ejemplo de respuesta de error:
```json
{
  "codigo": "REGLA_NEGOCIO",
  "mensaje": "No se pueden agregar preguntas a una trivia que no está en estado Borrador."
}
```

---

## 3. HU15 — Crear Trivia (cascarón inicial)

**Como** Operador, **quiero** crear una nueva trivia estableciendo su nombre, descripción y parámetros generales, **para** posteriormente agregarle preguntas.

```
Frontend → POST /api/juegos/trivias
           Authorization: Bearer <token-operador>
           { nombre, descripcion, tiempoLimitePorPregunta }
        ↓
TriviasControlador.CrearTrivia(CrearTriviaDto)
        ↓
CrearTriviaComando(dto, creadorId) → _mediador.Send(...)
        ↓
CrearTriviaManejador:
  1. ExisteTriviaConNombreAsync(nombre)  ← si existe → ExcepcionDominio
  2. Trivia.Crear(nombre, descripcion, creadorId, tiempo, fechaHoraUtc)
         → Estado: Borrador
         → Registra TriviaCreadaEvento (in-memory)
  3. repositorio.AgregarTriviaAsync(trivia)
        ↓
HTTP 201 Created  { id: "guid-de-la-trivia" }
```

El `creadorId` se extrae automáticamente del claim `sub` del token JWT; el frontend **no lo envía** en el body.

---

## 4. HU16 — Agregar Pregunta a Trivia

**Como** Operador, **quiero** agregar preguntas de selección simple o múltiple a una trivia en estado de borrador, **para** definir el contenido antes de activarla.

```
Frontend → POST /api/juegos/trivias/{triviaId}/preguntas
           Authorization: Bearer <token-operador>
           { enunciado, puntajeAsignado, opciones: [{ texto, esCorrecta }] }
        ↓
TriviasControlador.AgregarPregunta(triviaId, AgregarPreguntaDto)
        ↓
AgregarPreguntaComando(triviaId, dto) → _mediador.Send(...)
        ↓
AgregarPreguntaManejador:
  1. repositorio.ObtenerTriviaPorIdAsync(triviaId)
         → carga Trivia + Preguntas + Opciones con Include/ThenInclude
         → si no existe → ExcepcionNoEncontrado → 404
  2. trivia.AgregarPregunta(enunciado, puntaje, opciones)
         → ValidarEstadoBorrador()    ← si Activa → ExcepcionDominio → 422
         → Pregunta.Crear(...)        ← valida ≥2 opciones, ≥1 correcta, puntaje > 0
         → agrega Pregunta a _preguntas
  3. repositorio.AgregarPreguntaAsync(triviaId, pregunta)
        ↓
HTTP 201 Created  { id: "guid-de-la-pregunta" }
```

---

## 5. HU17 — Modificar / Eliminar Pregunta

**Como** Operador, **quiero** editar o eliminar una pregunta específica de una trivia en borrador, **para** corregir errores antes de activarla.

### Modificar

```
Frontend → PUT /api/juegos/trivias/{triviaId}/preguntas/{preguntaId}
           Authorization: Bearer <token-operador>
           { nuevoEnunciado, nuevasOpciones: [{ texto, esCorrecta }] }
        ↓
ModificarPreguntaComando(triviaId, preguntaId, dto) → _mediador.Send(...)
        ↓
ModificarPreguntaManejador:
  1. repositorio.ObtenerTriviaPorIdAsync(triviaId) ← carga el agregado completo
  2. trivia.ModificarPregunta(preguntaId, nuevoEnunciado, nuevasOpciones)
         → ValidarEstadoBorrador()
         → busca Pregunta por ID → si no existe → ExcepcionNoEncontrado → 404
         → pregunta.Modificar(...)   ← borra opciones y crea nuevas
  3. repositorio.ModificarPreguntaAsync(triviaId, preguntaModificada)
         → DELETE opciones anteriores + INSERT nuevas (reemplazo completo)
         → UPDATE enunciado en PreguntaModelo
        ↓
HTTP 204 No Content
```

### Eliminar

```
Frontend → DELETE /api/juegos/trivias/{triviaId}/preguntas/{preguntaId}
           Authorization: Bearer <token-operador>
        ↓
EliminarPreguntaComando(triviaId, preguntaId) → _mediador.Send(...)
        ↓
EliminarPreguntaManejador:
  1. repositorio.ObtenerTriviaPorIdAsync(triviaId) ← carga el agregado completo
  2. trivia.EliminarPregunta(preguntaId)
         → ValidarEstadoBorrador()
         → busca Pregunta por ID → si no existe → ExcepcionNoEncontrado → 404
         → remueve de _preguntas
  3. repositorio.EliminarPreguntaAsync(triviaId, preguntaId)
         → DELETE PreguntaModelo (CASCADE elimina sus OpcionModelo)
        ↓
HTTP 204 No Content
```

---

## 6. Endpoints

| Método | Ruta | Auth | HU | Descripción |
|---|---|---|---|---|
| `POST` | `/api/juegos/trivias` | `Operador` / `Administrador` | HU15 | Crear trivia en borrador |
| `GET` | `/api/juegos/trivias/borrador` | `Operador` / `Administrador` | HU15 | Listar trivias en borrador del operador autenticado |
| `POST` | `/api/juegos/trivias/{id}/preguntas` | `Operador` / `Administrador` | HU16 | Agregar pregunta a trivia |
| `PUT` | `/api/juegos/trivias/{id}/preguntas/{pid}` | `Operador` / `Administrador` | HU17 | Modificar pregunta |
| `DELETE` | `/api/juegos/trivias/{id}/preguntas/{pid}` | `Operador` / `Administrador` | HU17 | Eliminar pregunta |
| `GET` | `/salud` | Anónimo | — | Health check del servicio |

### Cuerpos de ejemplo

**Crear trivia:**
```json
{
  "nombre": "Trivia de Geografía",
  "descripcion": "Preguntas sobre capitales del mundo",
  "tiempoLimitePorPregunta": 30
}
```

**Agregar pregunta:**
```json
{
  "enunciado": "¿Cuál es la capital de Venezuela?",
  "puntajeAsignado": 10,
  "opciones": [
    { "texto": "Caracas",   "esCorrecta": true  },
    { "texto": "Maracaibo", "esCorrecta": false },
    { "texto": "Valencia",  "esCorrecta": false }
  ]
}
```

**Modificar pregunta:**
```json
{
  "nuevoEnunciado": "¿Capital de Venezuela?",
  "nuevasOpciones": [
    { "texto": "Caracas",   "esCorrecta": true  },
    { "texto": "Lima",      "esCorrecta": false },
    { "texto": "Bogotá",    "esCorrecta": false },
    { "texto": "La Paz",    "esCorrecta": false }
  ]
}
```

---

## 7. Modelo ER (base de datos)

```
juegos.Trivia (id, nombre★, descripcion, creador_id, tiempo_limite_por_pregunta, estado, fecha_creacion)
   1 ─── N
juegos.Pregunta (id, trivia_id, enunciado, puntaje_asignado)
   1 ─── N
juegos.Opcion (id, pregunta_id, texto, es_correcta)

★ índice único — no pueden existir dos trivias con el mismo nombre
CASCADE: eliminar Trivia → elimina sus Preguntas → elimina sus Opciones
```

**`estado`** se guarda como entero en la base de datos:

| Valor | Nombre |
|---|---|
| `0` | `Borrador` |
| `1` | `Activa` |
| `2` | `Archivada` |

---

## 8. Consulta optimizada (CQRS — lado lectura)

`GET /api/juegos/trivias/borrador` **no carga entidades de dominio**. El repositorio proyecta directamente desde la base de datos a `TriviaResumenDto` usando `AsNoTracking()` y `Select`:

```csharp
_contexto.Trivias
    .AsNoTracking()
    .Where(t => t.CreadorId == creadorId && t.Estado == 0)
    .Select(t => new TriviaResumenDto { ... TotalPreguntas = t.Preguntas.Count ... })
    .ToListAsync()
```

Esto sigue el principio CQRS del manual: las consultas de lectura no pasan por el agregado de dominio.

---

## 9. Comandos

### Migración EF Core (primera vez, obligatorio antes de correr)

```powershell
cd backend/servicios/juegos-servicio

dotnet ef migrations add InicialJuegos `
  --project JuegosServicio.Infraestructura `
  --startup-project JuegosServicio.Api `
  --output-dir Persistencia/Migraciones
```

La migración se aplica **automáticamente** al arranque del servicio via `contexto.Database.MigrateAsync()` en `Program.cs`.

### Compilar

```powershell
dotnet build backend/servicios/juegos-servicio/JuegosServicio.sln
```

### Docker

```powershell
# Levantar todo el ecosistema
docker compose up -d --build

# Ver logs del servicio
docker compose logs -f juegos-servicio

# Ver logs de la base de datos
docker compose logs -f postgres-juegos

# Detener
docker compose down
```

### Verificación rápida

```powershell
# 1. Obtener token (como operador)
$response = Invoke-RestMethod -Method POST `
  -Uri "http://localhost:5000/api/autenticacion/iniciar-sesion" `
  -ContentType "application/json" `
  -Body '{"nombreUsuario":"operador","contrasena":"Temporal123*"}'

$token = $response.tokenAcceso

# 2. Crear trivia
Invoke-RestMethod -Method POST `
  -Uri "http://localhost:5000/api/juegos/trivias" `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body '{"nombre":"Mi Trivia","descripcion":"Descripcion","tiempoLimitePorPregunta":30}'
```

Debe devolver `HTTP 201` con `{ id: "..." }`.

---

## 10. Accesos

| Servicio | URL |
|---|---|
| **API Gateway** | http://localhost:5000 |
| **Swagger juegos** | http://localhost:5002/swagger |
| **Swagger identidad** | http://localhost:5001/swagger |
| **Keycloak** | http://localhost:8080 (admin / admin) |
| **Postgres juegos** | localhost:**5435** (umbral / umbral123) |
| **Postgres identidad** | localhost:5433 (umbral / umbral123) |
| **Frontend web** | http://localhost:3000 |

---

## 11. Estructura del microservicio

```
juegos-servicio/
├── JuegosServicio.sln
│
├── JuegosServicio.Dominio/               ← Sin dependencias externas (C# puro)
│   ├── Entidades/
│   │   ├── Trivia.cs                     ← Aggregate Root
│   │   ├── Pregunta.cs
│   │   └── Opcion.cs
│   ├── Enums/
│   │   └── EstadoTrivia.cs
│   ├── Eventos/
│   │   ├── EventoDominio.cs
│   │   └── TriviaCreadaEvento.cs
│   └── Excepciones/
│       ├── ExcepcionDominio.cs           ← Regla de negocio violada → 422
│       └── ExcepcionNoEncontrado.cs      ← Entidad inexistente → 404
│
├── JuegosServicio.Commons/               ← DTOs sin lógica
│   └── Dtos/
│       ├── CrearTriviaDto.cs
│       ├── AgregarPreguntaDto.cs
│       ├── ModificarPreguntaDto.cs
│       ├── OpcionDto.cs
│       ├── TriviaResumenDto.cs
│       └── PreguntaDetalleDto.cs
│
├── JuegosServicio.Aplicacion/            ← CQRS. No depende de EF Core
│   ├── CasosDeUso/
│   │   ├── Comandos/
│   │   │   ├── CrearTriviaComando.cs
│   │   │   ├── AgregarPreguntaComando.cs
│   │   │   ├── ModificarPreguntaComando.cs
│   │   │   └── EliminarPreguntaComando.cs
│   │   ├── Consultas/
│   │   │   └── ObtenerTriviasEnBorradorConsulta.cs
│   │   └── Manejadores/
│   │       ├── CrearTriviaManejador.cs
│   │       ├── AgregarPreguntaManejador.cs
│   │       ├── ModificarPreguntaManejador.cs
│   │       ├── EliminarPreguntaManejador.cs
│   │       └── ObtenerTriviasEnBorradorManejador.cs
│   ├── Puertos/
│   │   ├── IRepositorioJuegos.cs         ← Contrato de persistencia
│   │   └── IProveedorFechaHora.cs        ← Abstracción del reloj
│   └── Dependencias/
│       └── RegistroAplicacion.cs
│
├── JuegosServicio.Infraestructura/       ← EF Core + PostgreSQL
│   ├── Persistencia/
│   │   ├── Modelos/
│   │   │   ├── TriviaModelo.cs
│   │   │   ├── PreguntaModelo.cs
│   │   │   └── OpcionModelo.cs
│   │   ├── ContextoJuegos.cs             ← DbContext con Fluent API, schema "juegos"
│   │   ├── JuegosMapeador.cs             ← Dominio ↔ Modelos (sin Mapster)
│   │   ├── RepositorioJuegos.cs          ← Implementa IRepositorioJuegos
│   │   └── SembradorJuegos.cs
│   ├── Tiempo/
│   │   └── ProveedorFechaHoraSistema.cs
│   └── Dependencias/
│       └── RegistroInfraestructura.cs
│
└── JuegosServicio.Api/                   ← ASP.NET Core 8
    ├── Controladores/
    │   └── TriviasControlador.cs         ← 5 endpoints HU15/16/17
    ├── Middlewares/
    │   └── ManejadorErroresMiddleware.cs ← Excepciones → HTTP codes
    ├── Configuraciones/
    │   ├── RegistroCors.cs
    │   └── RegistroSeguridad.cs          ← JWT Keycloak + política Operador
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    └── Dockerfile
```

---

## 12. Diferencia con `identidad-servicio`

| Aspecto | `identidad-servicio` | `juegos-servicio` |
|---|---|---|
| Base de datos | `postgres-identidad` (puerto 5433) | `postgres-juegos` (puerto 5435) |
| Puerto expuesto | 5001 | 5002 |
| Schema PostgreSQL | `identidad` | `juegos` |
| Patrón de dominio | Herencia (`Usuario` → `Administrador`, etc.) | Composición (`Trivia` → `Pregunta` → `Opcion`) |
| Mapeador | Mapster 7.4.0 (complejo: 1 entidad ↔ 3 tablas) | Manual `JuegosMapeador.cs` (directo: 1 entidad ↔ 1 tabla) |
| Keycloak | Crea usuarios, asigna roles | Solo valida tokens JWT |
| Evento externo | No (borrador no notifica al motor) | No (se implementará cuando Trivia se active) |

---

## 13. Checklist final

- [x] Dominio NO es igual a la base de datos.
- [x] `Trivia` es Aggregate Root: `Pregunta` y `Opcion` solo se crean a través de `Trivia`.
- [x] `Aplicacion` no depende de EF Core (solo `IRepositorioJuegos` + `IProveedorFechaHora`).
- [x] `Api` no tiene lógica de negocio (controladores → `_mediador.Send`).
- [x] `Commons` solo tiene carpeta `Dtos/`.
- [x] `JuegosMapeador` traduce dominio ↔ persistencia.
- [x] **Sin `FechaActualizacion`** en dominio/modelos.
- [x] Regla de negocio "solo Borrador" validada en el **dominio**, no en el controlador.
- [x] `ExcepcionDominio` → middleware → `422 { codigo: "REGLA_NEGOCIO", mensaje }`.
- [x] `ExcepcionNoEncontrado` → middleware → `404 { codigo: "NO_ENCONTRADO", mensaje }`.
- [x] Consulta `GET /borrador` usa `AsNoTracking()` y proyección directa a DTO (CQRS lectura).
- [x] `CreadorId` extraído del token JWT, nunca enviado en el body.
- [x] `docker compose up -d --build` levanta `postgres-juegos` + `juegos-servicio` correctamente.
- [x] API Gateway enruta `/api/juegos/**` al `juegos-cluster`.
- [x] `dotnet build` pasa sin errores.
- [ ] Migración inicial generada (`dotnet ef migrations add InicialJuegos ...`).
- [ ] Pruebas unitarias (dominio + manejadores).
- [ ] Pruebas de integración (endpoints con `WebApplicationFactory`).
