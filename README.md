# UMBRAL

Plataforma con **panel web** (Administrador y Operador) y **app móvil** (Participante) para
gestionar usuarios, sesiones de juego, **trivias** y **búsquedas del tesoro**. El backend está
construido sobre **.NET 9** con **microservicios**, **arquitectura hexagonal** y **CQRS con
MediatR**. La autenticación se delega en **Keycloak** y cada microservicio tiene su propia base
de datos **PostgreSQL**. Todo se levanta con **Docker Compose**.

---

## 1. Descripción general

UMBRAL es una plataforma con tres roles:

- **Administrador**: gestiona operadores, participantes y cuentas desde el panel web.
- **Operador**: gestiona participantes y prepara contenido (trivias, búsquedas del tesoro).
- **Participante**: usa la app móvil, mantiene su perfil y puede eliminar su propia cuenta.

---

## 2. Tecnologías utilizadas

| Capa | Tecnologías |
|------|-------------|
| Backend | .NET 9, ASP.NET Core, MediatR (CQRS), EF Core 9, Mapster |
| Base de datos | PostgreSQL 16 |
| Identidad | Keycloak 24 (realm `umbral`) |
| API Gateway | YARP |
| Frontend web | React + Vite + TypeScript |
| Frontend móvil | React Native + Expo |
| Contenedores | Docker / Docker Compose |
| Pruebas | xUnit, FluentAssertions, Moq, Microsoft.AspNetCore.Mvc.Testing, EF Core InMemory |

---

## 3. Arquitectura del proyecto

UMBRAL se compone por ahora de **dos microservicios de negocio** y un **api-gateway** que centraliza el
acceso desde los frontends.

- **identidad-servicio**: usuarios internos (administrador, operador), participantes, perfiles,
  estados de cuenta (Activo/Inactivo), autenticación contra Keycloak.
- **juegos-servicio**: trivias (preguntas y opciones) y búsquedas del tesoro (etapas y
  misiones), incluyendo borradores y publicaciones.
- **api-gateway** (YARP): expone una sola URL pública y enruta a cada microservicio.
- **frontend-web**: panel de Administrador y Operador.
- **frontend-movil**: app del Participante (Expo).
- **Keycloak**: gestiona credenciales y emite tokens JWT.
- **PostgreSQL**: cada microservicio tiene su propia base de datos; Keycloak la suya.

### Arquitectura hexagonal por microservicio

Cada microservicio se divide en cuatro proyectos:

| Proyecto | Responsabilidad |
|----------|-----------------|
| `*.Dominio` | Entidades, objetos de valor y reglas de negocio. No conoce EF Core ni HTTP. |
| `*.Aplicacion` | Casos de uso con MediatR (comandos y consultas), validaciones, estrategias. |
| `*.Infraestructura` | EF Core, repositorios, integración con Keycloak, persistencia. |
| `*.Api` | Controladores y endpoints HTTP. Solo orquesta hacia MediatR. |
| `*.Commons` | DTOs compartidos entre capas. |

---

## 4. Estructura de carpetas

```
UMBRAL/
├── backend/
│   ├── servicios/
│   │   ├── identidad-servicio/     # microservicio de identidad
│   │   └── juegos-servicio/        # microservicio de juegos
│   ├── gateway/
│   │   └── api-gateway/            # YARP api-gateway
│   └── pruebas/
│       ├── identidad/              # pruebas unitarias e integración de identidad
│       └── juegos/                 # pruebas unitarias de juegos
├── frontend-web/
│   └── umbral-web-react/           # panel web (React + Vite)
├── frontend-movil/
│   └── umbral-app-react-native/    # app móvil (Expo)
├── infraestructura/
│   └── keycloak/
│       └── realm-umbral.json       # configuración del realm
├── docker-compose.yml
└── UMBRAL.sln
```

Las pruebas viven **fuera** de los microservicios para separar el código productivo del de
pruebas. Cada microservicio queda con su propio `.sln` solo con su código productivo, y la
solución raíz `UMBRAL.sln` integra todo (productivo + pruebas).

---

## 5. Roles del sistema

| Rol | Dónde entra | Qué puede hacer |
|-----|-------------|-----------------|
| **Administrador** | Panel web | Registra y modifica operadores, activa/desactiva/elimina operadores, lista y administra participantes. |
| **Operador** | Panel web | Activa/desactiva participantes, crea y publica trivias y búsquedas del tesoro. |
| **Participante** | App móvil | Se registra, modifica su perfil, cambia su contraseña en Keycloak y puede eliminar su cuenta. |

---

## 6. Funcionalidades principales

- Registro de participantes desde la app móvil.
- Inicio de sesión en panel web y app móvil.
- Gestión de operadores (alta, modificación, activar, desactivar, eliminar).
- Listado y detalle de participantes y de usuarios internos.
- Modificación del perfil del participante.
- Activación / desactivación de cuentas.
- Eliminación de cuenta del propio participante.
- Eliminación de cuenta de operador.
- Gestión de trivias: crear, listar borradores, modificar, agregar/editar/eliminar preguntas,
  activar, archivar.
- Gestión de búsquedas del tesoro: crear, listar borradores, agregar/editar/eliminar etapas y
  misiones, activar, archivar.
- Autenticación y cambio de contraseña administrados por Keycloak.

---

## 7. Endpoints principales

Todas las rutas se llaman a través del **api-gateway** (`http://localhost:5000`). En desarrollo
también pueden llamarse directamente a cada microservicio.

### identidad-servicio (`http://localhost:5001`)

#### Autenticación

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/autenticacion/login-web` | Inicio de sesión desde panel web. | Administrador / Operador |
| POST | `/api/autenticacion/login-movil` | Inicio de sesión desde la app móvil. | Participante |
| GET  | `/api/autenticacion/perfil-actual` | Devuelve el perfil del usuario autenticado. | Cualquiera con token |

#### Usuarios

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST   | `/api/usuarios` | Crea Administrador u Operador. | Administrador |
| POST   | `/api/usuarios/participantes/registro` | Registro público de Participante. | Anónimo |
| GET    | `/api/usuarios/participantes` | Listado paginado de participantes. | Administrador / Operador |
| GET    | `/api/usuarios/participantes/{id}` | Detalle de un participante. | Administrador / Operador |
| GET    | `/api/usuarios/internos` | Listado paginado de Administradores y Operadores. | Administrador |
| GET    | `/api/usuarios/internos/{id}` | Detalle de un usuario interno. | Administrador |
| PATCH  | `/api/usuarios/participantes/perfil` | El Participante modifica su propio perfil. | Participante |
| PATCH  | `/api/usuarios/operadores/{id}` | Modifica los datos de un operador. | Administrador |
| PATCH  | `/api/usuarios/operadores/{id}/activar` | Activa un operador (Inactivo → Activo). | Administrador |
| PATCH  | `/api/usuarios/operadores/{id}/desactivar` | Desactiva un operador. | Administrador |
| PATCH  | `/api/usuarios/participantes/{id}/activar` | Activa un participante. | Administrador / Operador |
| PATCH  | `/api/usuarios/participantes/{id}/desactivar` | Desactiva un participante. | Administrador / Operador |
| DELETE | `/api/usuarios/operadores/{id}` | Elimina un operador (definitivo). | Administrador |
| DELETE | `/api/usuarios/participantes/perfil` | El Participante elimina su propia cuenta. | Participante |

#### Salud

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/salud` | Devuelve `{ estado: "ok" }`. |

### juegos-servicio (`http://localhost:5002`)

Todos los endpoints de juegos exigen el rol **Operador** (la política `PoliticaOperador`
también admite Administradores cuando aplica).

#### Trivias

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST   | `/api/juegos/trivias` | Crea una trivia en estado Borrador. |
| GET    | `/api/juegos/trivias/{triviaId}` | Detalle de una trivia con sus preguntas. |
| GET    | `/api/juegos/trivias/borrador` | Listado de trivias en Borrador. |
| GET    | `/api/juegos/trivias/activas` | Listado de trivias activas. |
| PUT    | `/api/juegos/trivias/{triviaId}` | Modifica datos generales de una trivia. |
| PATCH  | `/api/juegos/trivias/{triviaId}/activar` | Publica la trivia (Borrador → Activa). |
| DELETE | `/api/juegos/trivias/{triviaId}` | Archiva la trivia (soft delete). |
| POST   | `/api/juegos/trivias/{triviaId}/preguntas` | Agrega una pregunta. |
| PUT    | `/api/juegos/trivias/{triviaId}/preguntas/{preguntaId}` | Modifica una pregunta. |
| DELETE | `/api/juegos/trivias/{triviaId}/preguntas/{preguntaId}` | Elimina una pregunta. |

#### Búsquedas del tesoro

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST   | `/api/juegos/busquedas` | Crea una búsqueda del tesoro en Borrador. |
| GET    | `/api/juegos/busquedas/{busquedaId}` | Detalle con etapas y misiones. |
| GET    | `/api/juegos/busquedas/borrador` | Listado de búsquedas en Borrador. |
| GET    | `/api/juegos/busquedas/activas` | Listado de búsquedas activas. |
| PATCH  | `/api/juegos/busquedas/{busquedaId}/activar` | Publica la búsqueda (Borrador → Activa). |
| DELETE | `/api/juegos/busquedas/{busquedaId}` | Archiva la búsqueda. |
| POST   | `/api/juegos/busquedas/{busquedaId}/etapas` | Agrega una etapa. |
| PUT    | `/api/juegos/busquedas/{busquedaId}/etapas/{etapaId}` | Modifica una etapa. |
| DELETE | `/api/juegos/busquedas/{busquedaId}/etapas/{etapaId}` | Elimina una etapa. |
| POST   | `/api/juegos/busquedas/{busquedaId}/etapas/{etapaId}/misiones` | Agrega una misión. |
| PUT    | `/api/juegos/busquedas/{busquedaId}/etapas/{etapaId}/misiones/{misionId}` | Modifica una misión. |
| DELETE | `/api/juegos/busquedas/{busquedaId}/etapas/{etapaId}/misiones/{misionId}` | Elimina una misión. |

---

## 8. Requisitos para ejecutar localmente

Si quieres correr el proyecto **sin contenedores** (o mezclado), necesitas:

- **.NET 9 SDK**
- **Node.js 20+** y **npm**
- **Docker Desktop** (para Keycloak y PostgreSQL, aunque no levantes el backend en Docker)
- **Expo CLI** (opcional; `npx expo` ya funciona sin instalarlo global)
- **DBeaver** (opcional, para inspeccionar las bases de datos)

Comandos típicos (PowerShell desde la raíz):

```powershell
# Backend
dotnet restore .\UMBRAL.sln
dotnet build .\UMBRAL.sln
dotnet test  .\UMBRAL.sln

# Frontend web
cd .\frontend-web\umbral-web-react
npm install
npm run dev

# Frontend móvil
cd .\frontend-movil\umbral-app-react-native
npm install
npx expo start -c --tunnel
```

Equivalentes en Linux/Mac (bash):

```bash
dotnet restore ./UMBRAL.sln
dotnet build ./UMBRAL.sln
dotnet test  ./UMBRAL.sln

cd frontend-web/umbral-web-react && npm install && npm run dev
cd frontend-movil/umbral-app-react-native && npm install && npx expo start -c --tunnel
```

Para que el celular se conecte al backend desde Expo, ajusta la IP del PC en `app.json` y en
`clienteApi.ts` del proyecto móvil.

---

## 9. Cómo ejecutar todo con Docker

Con Docker Compose se levantan backend, frontend web, Keycloak, api-gateway y las tres bases
de datos. **No hace falta instalar .NET ni Node** para correr los contenedores; solo Docker
Desktop.

Comandos desde la raíz:

```powershell
# Levantar todo (en segundo plano y recompilando imágenes)
docker compose up -d --build

# Ver estado de los contenedores
docker compose ps

# Ver logs en vivo (todos)
docker compose logs -f

# Ver logs de un servicio en particular
docker compose logs -f identidad-servicio

# Apagar sin borrar datos
docker compose down

# Apagar y borrar volúmenes (resetea Keycloak y las bases de datos)
docker compose down -v
docker compose up -d --build
```

> ⚠️ `docker compose down -v` **borra los volúmenes**: se pierden todos los datos locales de
> PostgreSQL y la configuración persistida de Keycloak. Úsalo solo si quieres arrancar desde
> cero.

El perfil opcional `movil` levanta también el contenedor de Expo:

```powershell
docker compose --profile movil up -d --build
```

---

## 10. URLs importantes

| Servicio | URL |
|----------|-----|
| Frontend web | http://localhost:3000 |
| API Gateway | http://localhost:5000 |
| identidad-servicio (directo) | http://localhost:5001 |
| juegos-servicio (directo) | http://localhost:5002 |
| Keycloak | http://localhost:8080 |
| Swagger identidad (Development) | http://localhost:5001/swagger |
| Swagger juegos (Development) | http://localhost:5002/swagger |

---

## 11. Credenciales iniciales (solo desarrollo)

> Estas credenciales son **únicamente para entorno local**. En producción deben rotarse.

**Administrador inicial** (sembrado en Keycloak y en UMBRAL):

- Usuario: `administrador01`
- Contraseña: `Temporal123*`

**Administrador de Keycloak** (consola en `http://localhost:8080`):

- Usuario: `admin`
- Contraseña: `admin`

Los demás usuarios (operadores y participantes) se crean a través de la aplicación.

---

## 12. Bases de datos

Cada microservicio tiene su **propia base de datos** para que no compartan tablas ni
dependencias. Keycloak también usa su PostgreSQL aparte.

| Servicio | Host | Puerto | Base | Usuario | Contraseña |
|----------|------|--------|------|---------|------------|
| Identidad | localhost | 5433 | umbral_identidad | umbral | umbral123 |
| Juegos | localhost | 5435 | umbral_juegos | umbral | umbral123 |
| Keycloak | localhost | 5434 | keycloak | keycloak | keycloak123 |

Conexiones rápidas desde DBeaver: tipo PostgreSQL, host `localhost`, puerto el de la tabla,
y las credenciales indicadas.

---

## 13. Cómo correr las pruebas

Las pruebas viven en `backend/pruebas/` separadas del código productivo.

Desde la raíz, en PowerShell:

```powershell
# Todas las pruebas
dotnet test .\UMBRAL.sln

# Solo unitarias de identidad
dotnet test .\backend\pruebas\identidad\IdentidadServicio.PruebasUnitarias\IdentidadServicio.PruebasUnitarias.csproj

# Solo unitarias de juegos
dotnet test .\backend\pruebas\juegos\JuegosServicio.PruebasUnitarias\JuegosServicio.PruebasUnitarias.csproj

# Pruebas de integración de identidad
dotnet test .\backend\pruebas\identidad\IdentidadServicio.PruebasIntegracion\IdentidadServicio.PruebasIntegracion.csproj
```

> Actualmente **no existe** un proyecto `JuegosServicio.PruebasIntegracion`; las pruebas de
> juegos son unitarias.

---

## 14. Cómo crear migraciones EF Core

Las migraciones se aplican automáticamente al arrancar cada microservicio (`Database.MigrateAsync`).
Si necesitas crear una nueva, ejecuta desde la raíz:

**Identidad:**

```powershell
dotnet ef migrations add NombreMigracion `
  --project backend\servicios\identidad-servicio\IdentidadServicio.Infraestructura `
  --startup-project backend\servicios\identidad-servicio\IdentidadServicio.Api `
  --context ContextoIdentidad `
  --output-dir Persistencia\Migraciones
```

**Juegos:**

```powershell
dotnet ef migrations add NombreMigracion `
  --project backend\servicios\juegos-servicio\JuegosServicio.Infraestructura `
  --startup-project backend\servicios\juegos-servicio\JuegosServicio.Api `
  --context ContextoJuegos `
  --output-dir Persistencia\Migraciones
```

Equivalente bash: cambia `` ` `` por `\` y las barras invertidas por `/`.

Si no tienes la herramienta instalada: `dotnet tool install --global dotnet-ef`.

---

## 15. Notas importantes

- **Keycloak es la fuente de verdad de credenciales y tokens.** UMBRAL nunca guarda
  contraseñas en PostgreSQL.
- UMBRAL guarda **datos de negocio**: rol, estado (Activo/Inactivo), perfil, alias, códigos
  (`OP-###`, `AD-###`).
- Un usuario **inactivo** no puede iniciar sesión, y sus tokens previos quedan bloqueados por
  el middleware `BloqueoUsuarioInactivoMiddleware`.
- **Desactivar** una cuenta solo cambia el estado a Inactivo: los datos siguen ahí.
- **Eliminar** una cuenta borra los datos según la historia correspondiente (operador o
  participante) y elimina el usuario de Keycloak.
- No modifiques manualmente la base de datos de Keycloak salvo para revisión; la
  administración debe hacerse desde su consola web.
- Si se sufre una corrupción del realm o sembrado, basta con `docker compose down -v` y volver
  a subir; el `realm-umbral.json` se vuelve a importar.

---

## 16. Estado del proyecto

- En desarrollo activo.
- Funcionalidades de **identidad** (HU01 a HU14 aprox.) implementadas: login, registro de
  participantes, alta y mantenimiento de operadores, gestión de estados y eliminación de
  cuentas.
- Funcionalidades de **juegos** (HU15 a HU26): creación y mantenimiento de trivias y búsquedas
  del tesoro con etapas y misiones, en borrador y activas.
- Puede requerir ajustes y nuevas migraciones según se agreguen historias de usuario.

## Bases de datos locales

Cada microservicio tiene su propia base de datos PostgreSQL.

### Identidad

- Host: localhost
- Puerto: 5433
- Base de datos: umbral_identidad
- Usuario: umbral
- Contraseña: umbral123

### Keycloak

- Host: localhost
- Puerto: 5434
- Base de datos: keycloak
- Usuario: keycloak
- Contraseña: keycloak123

### Juegos

- Host: localhost
- Puerto: 5435
- Base de datos: umbral_juegos
- Usuario: umbral
- Contraseña: umbral123

### Sesiones

- Host: localhost
- Puerto: 5436
- Base de datos: umbral_sesiones
- Usuario: umbral
- Contraseña: umbral123

### Crear un .env.local en app movil 
- Colocar EXPO_PUBLIC_API_URL=http://tui_ip:5000
- luego hacer npx expo start -c --tunnel en la raiz de la app movil
- instalar las dependencias si es necesario npm install expo