# UMBRAL

**UMBRAL** es una plataforma de juegos y sesiones en vivo con un **panel web** para
**Administrador** y **Operador**, y una **app móvil** para el **Participante**.

El backend está construido con **.NET 9 / ASP.NET Core** siguiendo **microservicios**,
**arquitectura hexagonal** y **CQRS con MediatR**. La autenticación se delega en **Keycloak**
(JWT) y cada microservicio tiene su propia base de datos **PostgreSQL**. Un **API Gateway (YARP)**
expone una sola entrada pública. El microservicio de sesiones incluye **tiempo real con
SignalR/WebSockets** para notificar cambios de sala, y el `docker-compose.yml` deja **RabbitMQ**
configurado como broker de mensajería (aún no consumido por los servicios). Todo se levanta con
**Docker Compose**.

---

## Tabla de contenido

1. [Descripción general](#1-descripción-general)
2. [Tecnologías utilizadas](#2-tecnologías-utilizadas)
3. [Arquitectura del proyecto](#3-arquitectura-del-proyecto)
4. [Arquitectura hexagonal por microservicio](#4-arquitectura-hexagonal-por-microservicio)
5. [Estructura de carpetas](#5-estructura-de-carpetas)
6. [Funcionalidades principales](#6-funcionalidades-principales)
7. [Requisitos previos](#7-requisitos-previos)
8. [Configuración de variables de entorno](#8-configuración-de-variables-de-entorno)
9. [Cómo levantar todo con Docker](#9-cómo-levantar-todo-con-docker)
10. [URLs importantes](#10-urls-importantes)
11. [Credenciales iniciales](#11-credenciales-iniciales)
12. [Bases de datos](#12-bases-de-datos)
13. [Endpoints principales](#13-endpoints-principales)
14. [Cómo correr backend, web y móvil sin Docker](#14-cómo-correr-backend-web-y-móvil-sin-docker)
15. [Cómo correr las pruebas](#15-cómo-correr-las-pruebas)
16. [Logging y manejo de errores](#16-logging-y-manejo-de-errores)
17. [Flujo básico de prueba manual](#17-flujo-básico-de-prueba-manual)
18. [Problemas comunes](#18-problemas-comunes)
19. [Estado del proyecto](#19-estado-del-proyecto)

---

## 1. Descripción general

UMBRAL tiene tres roles:

- **Administrador**: gestiona usuarios internos (operadores y otros administradores), activa,
  desactiva y elimina operadores, resetea contraseñas, administra el catálogo de juegos
  (trivias, búsquedas del tesoro y misiones) y consulta participantes. Entra por el panel web.
- **Operador**: consulta el contenido activo, crea y gestiona **sesiones** de juego y **equipos**,
  y modera las sesiones que él mismo crea (expulsiones). Entra por el panel web.
- **Participante**: usa la **app móvil**. Se registra, mantiene su perfil, consulta las sesiones
  disponibles, ingresa a sesiones (individuales o por equipo), abandona sesiones y puede eliminar
  su propia cuenta.

---

## 2. Tecnologías utilizadas

| Capa | Tecnologías |
|------|-------------|
| Backend | .NET 9, ASP.NET Core, MediatR (CQRS), EF Core 9, Mapster |
| Bases de datos | PostgreSQL 16 (una por microservicio + la de Keycloak) |
| Identidad | Keycloak 24 (realm `umbral`, JWT) |
| API Gateway | YARP (reverse proxy) |
| Tiempo real | SignalR / WebSockets (hub de sesiones) |
| Mensajería | RabbitMQ (configurado en Docker; broker preparado para eventos asíncronos) |
| Frontend web | React 18 + Vite + TypeScript |
| Frontend móvil | React Native + Expo (expo-router) |
| Contenedores | Docker / Docker Compose |
| Pruebas | xUnit, FluentAssertions, Moq, `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory), EF Core InMemory, coverlet |

---

## 3. Arquitectura del proyecto

UMBRAL se compone de **tres microservicios de negocio**, un **API Gateway**, dos frontends y la
infraestructura de apoyo (Keycloak, PostgreSQL y RabbitMQ).

- **identidad-servicio**: usuarios y autenticación. Administradores, operadores y participantes;
  roles y estados de cuenta (Activo/Inactivo); integración con Keycloak; contraseñas temporales y
  envío de correos (SMTP).
- **juegos-servicio**: catálogo de contenido. Trivias (preguntas y opciones), búsquedas del tesoro
  (pistas), misiones (etapas) y consulta de contenidos activos. Maneja estados Borrador / Activa /
  Inactiva.
- **sesiones-servicio**: sesiones de juego en vivo. Creación y gestión de sesiones, equipos,
  ingreso/abandono de participantes, expulsiones, consultas para el móvil y **hub de sesiones**
  (SignalR).
- **api-gateway** (YARP): expone una sola entrada pública en `http://localhost:5000` y enruta a
  cada microservicio; también reenvía el handshake WebSocket del hub de sesiones.
- **frontend-web**: panel administrativo/operador (React + Vite).
- **frontend-movil**: app del participante (Expo).
- **Keycloak**: fuente de verdad de credenciales; emite los JWT que consumen los microservicios.
- **PostgreSQL**: una base por microservicio, más la base propia de Keycloak.
- **RabbitMQ**: broker de mensajería configurado en Docker para eventos/asíncrono. Actualmente
  ningún microservicio lo consume todavía; queda levantado y documentado.

---

## 4. Arquitectura hexagonal por microservicio

Cada microservicio separa el negocio de los detalles técnicos en cinco proyectos:

| Proyecto | Responsabilidad |
|----------|-----------------|
| `*.Dominio` | Entidades, objetos de valor y reglas de negocio. **No** depende de EF Core, HTTP, Keycloak, logging ni infraestructura. |
| `*.Aplicacion` | Casos de uso con MediatR (comandos y consultas), **puertos** (interfaces), validaciones y servicios de aplicación. |
| `*.Infraestructura` | Implementaciones concretas: EF Core, repositorios, servicios externos, Keycloak, correo y demás adaptadores de los puertos. |
| `*.Presentacion` | Controladores, middlewares HTTP, configuración web, Swagger y autenticación. Solo orquesta hacia MediatR. |
| `*.Commons` | DTOs compartidos entre capas. |

> **¿Por qué MediatR vive en Aplicación y no se considera infraestructura?** MediatR solo
> implementa el patrón *mediator* para despachar comandos y consultas dentro de la propia capa de
> aplicación (in-process). No aporta acceso a datos, red ni framework externo: es el mecanismo que
> orquesta los casos de uso. Por eso pertenece a Aplicación y no rompe la inversión de dependencias.
> El acceso a datos, Keycloak, correo, etc. sí quedan detrás de **puertos** implementados en
> Infraestructura.

---

## 5. Estructura de carpetas

```text
UMBRAL/
├── backend/
│   ├── servicios/
│   │   ├── identidad-servicio/      # microservicio de identidad
│   │   │   ├── IdentidadServicio.Dominio/
│   │   │   ├── IdentidadServicio.Aplicacion/
│   │   │   ├── IdentidadServicio.Infraestructura/
│   │   │   ├── IdentidadServicio.Presentacion/
│   │   │   └── IdentidadServicio.Commons/
│   │   ├── juegos-servicio/         # microservicio de juegos
│   │   └── sesiones-servicio/       # microservicio de sesiones
│   ├── gateway/
│   │   └── api-gateway/             # YARP (entrada única)
│   └── pruebas/
│       ├── identidad/               # pruebas unitarias e integración de identidad
│       ├── juegos/                  # pruebas unitarias de juegos
│       └── sesiones/                # pruebas unitarias e integración de sesiones
├── frontend-web/
│   └── umbral-web-react/            # panel web (React + Vite)
├── frontend-movil/
│   └── umbral-app-react-native/     # app móvil (Expo)
├── infraestructura/
│   └── keycloak/
│       └── realm-umbral.json        # configuración del realm que Keycloak importa
├── .env.example                     # variables de referencia (correo, etc.)
├── docker-compose.yml
├── verificar-backend.ps1            # script de compilación + pruebas + cobertura
└── UMBRAL.sln                       # solución raíz (código productivo + pruebas)
```

Las pruebas viven **fuera** de los microservicios para separar el código productivo del de
pruebas. La solución raíz `UMBRAL.sln` integra todo.

---

## 6. Funcionalidades principales

- **Login web** para Administrador y Operador (`login-web`).
- **Login móvil** para Participante (`login-movil`).
- **Registro público** de participante desde la app móvil.
- **Gestión de usuarios internos**: alta de administradores/operadores, modificación, activación,
  desactivación, eliminación y reseteo de contraseña.
- **Gestión de participantes**: listado, detalle, activación/desactivación y edición/eliminación
  del propio perfil.
- **Gestión de trivias**: crear, listar (borrador/activas), modificar, agregar/editar/eliminar
  preguntas, activar y eliminar.
- **Gestión de búsquedas del tesoro**: crear, listar, modificar, agregar/editar/eliminar pistas,
  activar y eliminar.
- **Gestión de misiones**: crear, listar, modificar, agregar/eliminar etapas, activar y eliminar;
  detalle recortado para el participante.
- **Gestión de sesiones**: crear, listar, ver detalle, modificar y eliminar sesiones.
- **Gestión de equipos**: crear, listar, ver detalle, modificar y eliminar equipos.
- **Ingreso de participantes**: a sesiones individuales, por código de acceso o a un equipo.
- **Abandono de sesión** por parte del participante.
- **Expulsión** de participantes (por el líder del equipo o el operador) y de equipos (por el
  operador dueño de la sesión).
- **Consulta de sesiones disponibles** desde el móvil.
- **Logging técnico**, manejo global de excepciones y **CorrelationId** por petición.
- **Envío de correos** de contraseñas temporales cuando el SMTP está habilitado.

---

## 7. Requisitos previos

- **Docker Desktop** (para levantar todo con Docker Compose).
- **.NET 9 SDK** — necesario si se ejecuta el backend sin Docker o se corren las pruebas.
- **Node.js 20+ y npm** — necesarios si se ejecutan los frontends fuera de Docker.
- **Expo Go** (o un *development build*) en el celular para probar la app móvil.
- **PowerShell** para ejecutar `verificar-backend.ps1` en Windows.
- **DBeaver / Postman** (opcionales) para inspeccionar bases de datos y probar la API.

---

## 8. Configuración de variables de entorno

En el repo existe un archivo `.env.example` en la raíz con las variables de referencia. El
`docker-compose.yml` ya inyecta valores por defecto; solo necesitas un `.env` propio si quieres
personalizar (por ejemplo, para activar los correos).

### 8.1 Archivo `.env` en la raíz para Docker Compose (correo)

El `identidad-servicio` puede enviar correos de contraseñas temporales (alta administrativa y
reseteo) por **SMTP**. Esa configuración se toma de variables de entorno; puedes crear un `.env`
en la raíz del repositorio partiendo de `.env.example`.

Valores por defecto (correo **desactivado**):

```env
CORREO_HABILITADO=false
CORREO_HOST=
CORREO_PUERTO=587
CORREO_USAR_SSL=true
CORREO_USUARIO=
CORREO_CONTRASENA=
CORREO_REMITENTE=no-reply@umbral.local
CORREO_REMITENTE_NOMBRE=UMBRAL
```

Ejemplo para Gmail o Mailtrap (**sin credenciales reales**):

```env
CORREO_HABILITADO=true
CORREO_HOST=smtp.gmail.com
CORREO_PUERTO=587
CORREO_USAR_SSL=true
CORREO_USUARIO=tu_correo@gmail.com
CORREO_CONTRASENA=tu_app_password
CORREO_REMITENTE=tu_correo@gmail.com
CORREO_REMITENTE_NOMBRE=UMBRAL
```

Aclaraciones:

- **No** subas tu `.env` al repositorio.
- Para **Gmail** debes usar una **contraseña de aplicación (App Password)**, no la contraseña
  normal de la cuenta.
- Si `CORREO_HABILITADO=false`, el sistema **no envía correos reales**: solo registra en los logs
  que el envío fue solicitado y nunca expone la contraseña temporal.
- Nunca coloques contraseñas reales en el repositorio ni en la documentación.

### 8.2 Variables del frontend web

El frontend web usa `VITE_API_URL` para saber dónde está el gateway. En Docker Compose ya se
define como `http://localhost:5000`. Si lo ejecutas fuera de Docker, puedes crear un `.env` en
`frontend-web/umbral-web-react/`:

```env
VITE_API_URL=http://localhost:5000
```

### 8.3 Variables del frontend móvil con Expo

La app móvil lee la URL del backend desde `EXPO_PUBLIC_API_URL` (ver
`frontend-movil/umbral-app-react-native/servicios/clienteHttp.ts`). Si no se define, usa
`http://localhost:5000` por defecto.

Crea un archivo `.env` en `frontend-movil/umbral-app-react-native/`:

```env
EXPO_PUBLIC_API_URL=http://IP_DE_TU_PC:5000
```
EXPO=https://umbral-api-gateway.onrender.com para hostear con Render

Explicación importante:

- En un **celular físico**, `http://localhost:5000` **no sirve**: `localhost` sería el propio
  celular, no tu PC.
- Debes usar la **IP local de tu PC** en la misma red Wi-Fi, por ejemplo:
  `EXPO_PUBLIC_API_URL=http://192.168.1.9:5000`.
- El celular y la PC deben estar en la **misma red Wi-Fi**.
- Si usas un **emulador Android**, suele aplicar `http://10.0.2.2:5000`.
- Después de cambiar el `.env`, reinicia Expo limpiando la caché:

```bash
npx expo start -c
npx expo start --tunnel
```

---

## 9. Cómo levantar todo con Docker

Con Docker Compose se levantan los tres microservicios, el API Gateway, el frontend web, Keycloak,
RabbitMQ y las cuatro bases de datos PostgreSQL. **No hace falta instalar .NET ni Node** para
correr los contenedores; solo Docker Desktop.

Desde la raíz del repositorio:

```powershell
# Levantar todo (en segundo plano, recompilando imágenes)
docker compose up -d --build

# Ver estado de los contenedores
docker compose ps

# Ver logs en vivo (todos los servicios)
docker compose logs -f

# Apagar sin borrar datos
docker compose down

# Apagar y borrar volúmenes (resetea bases de datos y Keycloak)
docker compose down -v
```

Aclaraciones:

- `docker compose down` **no borra datos**: los volúmenes de PostgreSQL y Keycloak se conservan.
- `docker compose down -v` **borra los volúmenes**: se pierden todos los datos locales y Keycloak
  vuelve a importar `realm-umbral.json` al levantar de nuevo. Úsalo solo para arrancar desde cero.
- Las **migraciones EF Core se aplican automáticamente** al arrancar cada microservicio, así que
  las bases quedan listas sin pasos manuales.
- Si cambiaste código backend o un Dockerfile, vuelve a construir con `--build`.
- Si solo quieres reiniciar un servicio sin reconstruir: `docker compose restart <servicio>`.

La app móvil tiene un **perfil opcional** que levanta el bundler de Expo dentro de Docker:

```powershell
docker compose --profile movil up -d --build
```

> En la práctica, para probar en un **celular físico** suele ser más cómodo ejecutar Expo **fuera
> de Docker** (ver la sección 14), porque el celular se conecta directo al Metro Bundler de tu PC.

---

## 10. URLs importantes

| Servicio | URL |
|----------|-----|
| Frontend web | http://localhost:3000 |
| API Gateway | http://localhost:5000 |
| identidad-servicio (directo) | http://localhost:5001 |
| juegos-servicio (directo) | http://localhost:5002 |
| sesiones-servicio (directo) | http://localhost:5003 |
| Keycloak | http://localhost:8080 |
| RabbitMQ (panel de administración) | http://localhost:15672 |
| Swagger identidad (entorno Development) | http://localhost:5001/swagger |
| Swagger juegos (entorno Development) | http://localhost:5002/swagger |
| Swagger sesiones (entorno Development) | http://localhost:5003/swagger |
| Hub de sesiones vía gateway | http://localhost:5000/hubs/sesiones |
| Hub de sesiones (directo) | http://localhost:5003/hubs/sesiones |

---

## 11. Credenciales iniciales

> ⚠️ Estas credenciales son **únicamente para desarrollo local** y deben rotarse en producción.

**Administrador inicial** (sembrado en Keycloak y en UMBRAL):

- Usuario: `administrador01`
- Contraseña: `Temporal123*`

**Administrador de Keycloak** (consola en `http://localhost:8080`):

- Usuario: `admin`
- Contraseña: `admin`

**RabbitMQ** (panel en `http://localhost:15672`):

- Usuario: `umbral`
- Contraseña: `umbral123`

Los demás usuarios (operadores y participantes) se crean a través de la aplicación.

---

## 12. Bases de datos

Cada microservicio tiene su **propia base de datos** para no compartir tablas ni dependencias.
Keycloak usa su PostgreSQL aparte.

| Servicio | Host | Puerto | Base de datos | Usuario | Contraseña |
|----------|------|--------|---------------|---------|------------|
| Keycloak | localhost | 5434 | keycloak | keycloak | keycloak123 |
| Identidad | localhost | 5433 | umbral_identidad | umbral | umbral123 |
| Juegos | localhost | 5435 | umbral_juegos | umbral | umbral123 |
| Sesiones | localhost | 5436 | umbral_sesiones | umbral | umbral123 |

Conexión rápida desde DBeaver: tipo **PostgreSQL**, host `localhost`, el puerto de la tabla y las
credenciales indicadas.

---

## 13. Endpoints principales

Normalmente todo se llama a través del **API Gateway** (`http://localhost:5000`). En desarrollo
también se puede llamar directo a cada microservicio (`5001` identidad, `5002` juegos, `5003`
sesiones). Salvo los marcados como *Anónimo*, todos requieren un **JWT** válido en el encabezado
`Authorization: Bearer <token>`.

Roles usados en las tablas: **Administrador**, **Operador**, **Participante**, **Anónimo**,
**Autenticado** (cualquier usuario con token) e **Interno** (endpoints que consume otro
microservicio).

### 13.1 identidad-servicio

**Autenticación** (`/api/autenticacion`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/autenticacion/login-web` | Inicio de sesión desde el panel web. | Anónimo (Administrador / Operador) |
| POST | `/api/autenticacion/login-movil` | Inicio de sesión desde la app móvil. | Anónimo (Participante) |
| GET | `/api/autenticacion/perfil-actual` | Perfil del usuario autenticado. | Autenticado |
| POST | `/api/autenticacion/cambiar-contrasena-obligatoria` | Cambio obligatorio de la contraseña temporal. | Autenticado (Operador / Administrador) |

**Usuarios** (`/api/usuarios`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/usuarios` | Crea un Administrador u Operador. | Administrador |
| POST | `/api/usuarios/participantes/registro` | Registro público de Participante. | Anónimo |
| GET | `/api/usuarios/participantes` | Listado paginado de participantes. | Administrador / Operador |
| GET | `/api/usuarios/participantes/{id}` | Detalle de un participante. | Administrador / Operador |
| GET | `/api/usuarios/internos` | Listado paginado de administradores y operadores. | Administrador |
| GET | `/api/usuarios/internos/{id}` | Detalle de un usuario interno. | Administrador |
| PATCH | `/api/usuarios/participantes/perfil` | El participante edita su propio perfil. | Participante |
| PATCH | `/api/usuarios/operadores/{id}` | Modifica los datos de un operador. | Administrador |
| PATCH | `/api/usuarios/operadores/{id}/activar` | Activa un operador. | Administrador |
| PATCH | `/api/usuarios/operadores/{id}/desactivar` | Desactiva un operador. | Administrador |
| PATCH | `/api/usuarios/participantes/{id}/activar` | Activa un participante. | Administrador / Operador |
| PATCH | `/api/usuarios/participantes/{id}/desactivar` | Desactiva un participante. | Administrador / Operador |
| DELETE | `/api/usuarios/operadores/{id}` | Elimina un operador (definitivo). | Administrador |
| DELETE | `/api/usuarios/participantes/perfil` | El participante elimina su propia cuenta. | Participante |
| POST | `/api/usuarios/internos/{id}/resetear-contrasena` | Resetea la contraseña de un usuario interno. | Administrador |
| POST | `/api/usuarios/internos/administradores-por-ids` | Filtra administradores por ids. | Interno |
| POST | `/api/usuarios/participantes/por-ids` | Datos básicos de participantes por ids (consumido por sesiones-servicio). | Interno |

**Salud**

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/identidad/salud` (gateway) · `/salud` (directo) | Estado del servicio. |

### 13.2 juegos-servicio

En general, las operaciones de **escritura** exigen rol **Administrador**; las **consultas** de
detalle y de contenido activo admiten **Operador**; los endpoints `participante/...` son para el
**Participante**.

**Trivias** (`/api/juegos/trivias`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/juegos/trivias` | Crea una trivia en estado Borrador. | Administrador |
| GET | `/api/juegos/trivias/borrador` | Lista de trivias en Borrador. | Administrador |
| GET | `/api/juegos/trivias/activas` | Lista de trivias activas. | Operador |
| GET | `/api/juegos/trivias/{triviaId}` | Detalle de la trivia con sus preguntas. | Operador |
| PUT | `/api/juegos/trivias/{triviaId}` | Modifica datos generales de la trivia. | Administrador |
| PATCH | `/api/juegos/trivias/{triviaId}/activar` | Publica la trivia (Borrador → Activa). | Administrador |
| DELETE | `/api/juegos/trivias/{triviaId}` | Desactiva la trivia. | Administrador |
| DELETE | `/api/juegos/trivias/{triviaId}/eliminar` | Elimina la trivia de forma definitiva. | Administrador |
| POST | `/api/juegos/trivias/{triviaId}/preguntas` | Agrega una pregunta. | Administrador |
| PUT | `/api/juegos/trivias/{triviaId}/preguntas/{preguntaId}` | Modifica una pregunta. | Administrador |
| DELETE | `/api/juegos/trivias/{triviaId}/preguntas/{preguntaId}` | Elimina una pregunta. | Administrador |

**Búsquedas del tesoro** (`/api/juegos/busquedas`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/juegos/busquedas` | Crea una búsqueda del tesoro en Borrador. | Administrador |
| GET | `/api/juegos/busquedas/borrador` | Lista de búsquedas en Borrador. | Administrador |
| GET | `/api/juegos/busquedas/activas` | Lista de búsquedas activas. | Operador |
| GET | `/api/juegos/busquedas/{busquedaId}` | Detalle con sus pistas. | Operador |
| PATCH | `/api/juegos/busquedas/{busquedaId}` | Modifica la búsqueda. | Administrador |
| PATCH | `/api/juegos/busquedas/{busquedaId}/activar` | Publica la búsqueda (Borrador → Activa). | Administrador |
| DELETE | `/api/juegos/busquedas/{busquedaId}` | Desactiva la búsqueda. | Administrador |
| DELETE | `/api/juegos/busquedas/{busquedaId}/eliminar` | Elimina la búsqueda de forma definitiva. | Administrador |
| POST | `/api/juegos/busquedas/{busquedaId}/pistas` | Agrega una pista. | Administrador |
| PUT | `/api/juegos/busquedas/{busquedaId}/pistas/{pistaId}` | Modifica una pista. | Administrador |
| DELETE | `/api/juegos/busquedas/{busquedaId}/pistas/{pistaId}` | Elimina una pista. | Administrador |

**Misiones** (`/api/juegos/misiones`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/juegos/misiones` | Crea una misión en Borrador. | Administrador |
| GET | `/api/juegos/misiones/borrador` | Lista de misiones en Borrador. | Administrador |
| GET | `/api/juegos/misiones/activas` | Lista de misiones activas. | Operador |
| GET | `/api/juegos/misiones/{misionId}` | Detalle de la misión (con etapas). | Operador |
| GET | `/api/juegos/misiones/participante/{misionId}` | Detalle recortado para el participante (solo si está Activa). | Participante |
| PATCH | `/api/juegos/misiones/{misionId}` | Modifica la misión. | Administrador |
| PATCH | `/api/juegos/misiones/{misionId}/activar` | Publica la misión. | Administrador |
| DELETE | `/api/juegos/misiones/{misionId}` | Desactiva la misión. | Administrador |
| DELETE | `/api/juegos/misiones/{misionId}/eliminar` | Elimina la misión de forma definitiva. | Administrador |
| POST | `/api/juegos/misiones/{misionId}/etapas` | Agrega una etapa. | Administrador |
| DELETE | `/api/juegos/misiones/{misionId}/etapas/{etapaId}` | Elimina una etapa. | Administrador |

**Contenidos activos** (`/api/juegos/contenidos-activos`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| GET | `/api/juegos/contenidos-activos/{tipoJuego}/{contenidoId}` | Verifica que un contenido exista y esté Activo (consumido por sesiones-servicio). | Operador (interno) |

**Salud**

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/juegos/salud` (gateway) · `/salud` (directo) | Estado del servicio. |

### 13.3 sesiones-servicio

**Sesiones** (`/api/sesiones`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/sesiones` | Crea una sesión. | Operador |
| GET | `/api/sesiones` | Lista sesiones (Administrador: todas; Operador: las propias). Admite `?estado=`. | Administrador / Operador |
| GET | `/api/sesiones/{id}` | Detalle de una sesión. | Administrador / Operador |
| PUT | `/api/sesiones/{id}` | Modifica una sesión propia en estado Programada. | Operador |
| DELETE | `/api/sesiones/{id}` | Elimina una sesión propia en estado Programada. | Operador |
| DELETE | `/api/sesiones/{sesionId}/abandonar` | Abandona la sesión (individual) o el equipo (grupal). | Participante |
| GET | `/api/sesiones/misiones/{misionId}/existe-vigente` | Indica si una misión tiene sesiones vigentes (consumido por juegos-servicio). | Administrador / Operador (interno) |

**Sesiones disponibles (móvil)** (`/api/sesiones/participante/disponibles`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| GET | `/api/sesiones/participante/disponibles` | Lista sesiones disponibles. Admite `?busqueda=` y `?modo=`. | Participante |
| GET | `/api/sesiones/participante/disponibles/{sesionId}` | Detalle de una sesión disponible. | Participante |

**Ingreso de participantes**

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/sesiones/participante/ingresar` | Ingresa a una sesión por código de acceso. | Participante |
| POST | `/api/sesiones/{sesionId}/participante/ingresar-individual` | Ingresa a una sesión individual. | Participante |

**Equipos** (`/api/sesiones/{sesionId}/equipos`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| POST | `/api/sesiones/{sesionId}/equipos` | Crea un equipo. | Participante |
| GET | `/api/sesiones/{sesionId}/equipos` | Lista los equipos de la sesión. | Administrador / Operador / Participante |
| GET | `/api/sesiones/{sesionId}/equipos/{equipoId}` | Detalle de un equipo. | Administrador / Operador / Participante |
| PUT | `/api/sesiones/{sesionId}/equipos/{equipoId}` | Modifica un equipo (solo el líder). | Participante |
| DELETE | `/api/sesiones/{sesionId}/equipos/{equipoId}` | Elimina un equipo (solo el líder). | Participante |
| POST | `/api/sesiones/{sesionId}/equipos/{equipoId}/ingresar` | Ingresa a un equipo (si es privado, requiere contraseña). | Participante |
| DELETE | `/api/sesiones/{sesionId}/equipos/{equipoId}/participantes/{participanteSesionId}/expulsar` | Expulsa a un participante del equipo. | Operador / Participante (líder) |

**Expulsiones por el operador** (`/api/sesiones`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| DELETE | `/api/sesiones/{sesionId}/participantes/{participanteSesionId}/expulsar` | Expulsa a un participante de una sesión individual. | Operador |
| DELETE | `/api/sesiones/{sesionId}/equipos/{equipoId}/expulsar` | Expulsa a un equipo completo de una sesión grupal. | Operador |

**Salud y tiempo real**

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/sesiones/salud` (gateway) · `/salud` (directo) | Estado del servicio. |
| WS | `/hubs/sesiones` (gateway) · `/hubs/sesiones` (directo) | Hub SignalR protegido con JWT. Solo notifica cambios; la fuente de verdad sigue siendo HTTP. |

---

## 14. Cómo correr backend, web y móvil sin Docker

Puedes ejecutar los componentes por separado. El backend sigue necesitando **PostgreSQL y
Keycloak**, que es más cómodo levantar con Docker (`docker compose up -d postgres-identidad
postgres-juegos postgres-sesiones postgres-keycloak keycloak`).

**Backend** (desde la raíz):

```powershell
dotnet restore .\UMBRAL.sln
dotnet build .\UMBRAL.sln

# Ejecutar un microservicio (proyecto de Presentación)
dotnet run --project .\backend\servicios\identidad-servicio\IdentidadServicio.Presentacion
dotnet run --project .\backend\servicios\juegos-servicio\JuegosServicio.Presentacion
dotnet run --project .\backend\servicios\sesiones-servicio\SesionesServicio.Presentacion

# API Gateway
dotnet run --project .\backend\gateway\api-gateway
```

**Frontend web:**

```powershell
Set-Location .\frontend-web\umbral-web-react
npm install
npm run dev        # sirve en http://localhost:3000
```

**Frontend móvil** (recuerda configurar `EXPO_PUBLIC_API_URL`, sección 8.3):

```powershell
Set-Location .\frontend-movil\umbral-app-react-native
npm install
npx expo start -c  # -c limpia la caché de Metro
```

---

## 15. Cómo correr las pruebas

### 15.1 Script completo

El script `verificar-backend.ps1` hace `restore`, `build` en configuración *Release* y ejecuta
todas las suites de prueba con cobertura, dejando los resultados en
`artifacts/test-results` (archivos `.trx` y `coverage.cobertura.xml`).

```powershell
.\verificar-backend.ps1
```

Modo silencioso (baja el nivel de logging durante las pruebas):

```powershell
.\verificar-backend.ps1 -Silencioso
```

Si PowerShell bloquea la ejecución de scripts:

```powershell
powershell -ExecutionPolicy Bypass -File .\verificar-backend.ps1
```

Las suites que ejecuta son: `sesiones-unitarias`, `sesiones-integracion`, `identidad-unitarias`,
`identidad-integracion` y `juegos-unitarias`.

### 15.2 Pruebas individuales

```powershell
dotnet test .\backend\pruebas\identidad\IdentidadServicio.PruebasUnitarias\IdentidadServicio.PruebasUnitarias.csproj
dotnet test .\backend\pruebas\identidad\IdentidadServicio.PruebasIntegracion\IdentidadServicio.PruebasIntegracion.csproj
dotnet test .\backend\pruebas\juegos\JuegosServicio.PruebasUnitarias\JuegosServicio.PruebasUnitarias.csproj
dotnet test .\backend\pruebas\sesiones\SesionesServicio.PruebasUnitarias\SesionesServicio.PruebasUnitarias.csproj
dotnet test .\backend\pruebas\sesiones\SesionesServicio.PruebasIntegracion\SesionesServicio.PruebasIntegracion.csproj
```

> Actualmente **no existe** un proyecto `JuegosServicio.PruebasIntegracion`; las pruebas de juegos
> son unitarias.

Comandos útiles sobre un proyecto de pruebas:

```powershell
# Listar todas las pruebas de un proyecto
dotnet test RUTA_DEL_CSPROJ --list-tests

# Ejecutar una sola prueba por nombre
dotnet test RUTA_DEL_CSPROJ --filter "FullyQualifiedName~NombreDeLaPrueba"

# Ejecutar todas las pruebas de una clase
dotnet test RUTA_DEL_CSPROJ --filter "FullyQualifiedName~NombreDeLaClase"
```

---

## 16. Logging y manejo de errores

- Los **logs técnicos salen por consola / Docker**. **No** se guardan en base de datos y **no**
  existe un módulo web de auditoría persistente.
- El `LoggingSolicitudesMiddleware` (capa de Presentación) registra, por cada petición HTTP:
  **Servicio**, **Descripción**, **método HTTP**, **ruta**, **código de respuesta**, **duración
  (ms)**, **UsuarioId**, **Usuario**, **Rol**, **IP**, **UserAgent** y **CorrelationId** (según el
  estado actual del código).
- Un `ManejadorErroresMiddleware` centraliza las excepciones y devuelve respuestas de error
  consistentes.

**CorrelationId (`X-Correlation-Id`)**

- Si el cliente envía el encabezado `X-Correlation-Id`, el backend **lo conserva**.
- Si no lo envía, el backend **genera uno** (GUID) y lo devuelve en la respuesta.
- Sirve para **relacionar todos los logs de una misma petición** entre middlewares y servicios.

**Comandos útiles para ver logs en Docker:**

```powershell
docker compose logs -f
docker compose logs -f --no-log-prefix identidad-servicio
docker compose logs -f --no-log-prefix juegos-servicio
docker compose logs -f --no-log-prefix sesiones-servicio
```

**Filtrar logs (PowerShell):**

```powershell
docker compose logs --no-log-prefix identidad-servicio | Select-String "CorrelationId"
docker compose logs --no-log-prefix sesiones-servicio | Select-String "LoggingSolicitudesMiddleware"
docker compose logs --since 5m --no-log-prefix sesiones-servicio
```

**Seguir una petición por su CorrelationId:**

```powershell
curl.exe -i http://localhost:5000/api/sesiones/participante/disponibles `
  -H "Authorization: Bearer TU_TOKEN" `
  -H "X-Correlation-Id: prueba-logs-001"
```

Y luego buscar ese identificador en los logs:

```powershell
docker compose logs --no-log-prefix sesiones-servicio | Select-String "prueba-logs-001"
```

> Si en el log aparecen `Usuario`/`Rol` como `null`, verifica que estés enviando el **token JWT**
> y que sus *claims* (usuario y roles) estén correctamente configurados en Keycloak.

---

## 17. Problemas comunes

- **El celular no conecta con el backend**: usa la **IP de tu PC** en `EXPO_PUBLIC_API_URL`, no
  `localhost`, y asegúrate de estar en la misma red Wi-Fi.
- **Expo no toma los cambios**: reinicia limpiando caché con `npx expo start -c`.
- **Docker no refleja cambios de backend**: reconstruye con `docker compose up -d --build`.
- **Keycloak o las bases quedaron con datos viejos**: `docker compose down -v` y vuelve a levantar
  (Keycloak reimporta `realm-umbral.json`).
- **Puerto ocupado**: revisa contenedores (`docker compose ps`) y procesos que usen los puertos
  3000, 5000–5003, 8080, 5433–5436, 5672 o 15672.
- **No llegan correos**: verifica `CORREO_HABILITADO=true` y los valores de host, puerto, SSL,
  usuario y **App Password** (sección 8.1).
- **401 / 403**: revisa el token, el rol del usuario y si estás usando `login-web` o `login-movil`
  según corresponda al rol.

---

