# UMBRAL — HU01 (Login) + HU02 (Crear operador)

Proyecto académico que aplica **microservicios + arquitectura hexagonal + CQRS con MediatR + Keycloak** para la
historia de usuario **HU01: Inicio de sesión** de la plataforma UMBRAL.

---

## 1. Idea clave: dominio ≠ base de datos

| Dominio (`IdentidadServicio.Dominio`) | Persistencia (`IdentidadServicio.Infraestructura`) |
|---|---|
| `Usuario` (clase **abstracta**) | `UsuarioModelo` (tabla `Usuario`) |
| `Administrador : Usuario` | `AdministradorModelo` (tabla `Administrador`) |
| `Operador : Usuario` | `OperadorModelo` (tabla `Operador`) |
| `Participante : Usuario` | `ParticipanteModelo` (tabla `Participante`) |
| Datos personales **viven en `Usuario`** (VO: `NombrePersona`, `Email`, `DatosContacto`) | Datos personales **viven en `PersonaModelo`** (tabla `Persona`) |
| Value Objects: `NombreUsuario`, `NombrePersona`, `Email`, `DatosContacto` (sin Email) | Columnas planas (string, int, DateTime) |
| **NO conoce `IdKeycloak`** (detalle técnico) | `UsuarioModelo.IdKeycloak` (único campo con IdKeycloak en el sistema) |

- Dominio con **herencia**, persistencia con **5 tablas planas sin herencia** (no TPH/TPT/TPC).
- Tablas físicas en **singular** (`Usuario`, `Persona`, `Administrador`, `Operador`, `Participante`).
- **`Correo`** es VO independiente; `DatosContacto` sólo tiene `Direccion`/`Telefono`.
- **`NombreUsuario` ≠ `Correo`**: el username de Keycloak es distinto del email. Acepta `operador01`, `admin_umbral`, `participante123`. El realm tiene `loginWithEmailAllowed = false`.
- **Sin `FechaActualizacion`** en ningún archivo.
- **`IProveedorFechaHora`** abstrae el reloj. Sólo `ProveedorFechaHoraSistema` usa `DateTime.UtcNow`.
- Mapeo dominio↔persistencia con **Mapster** (paquete `Mapster 7.4.0`). El mapper recibe `idKeycloak` como parámetro al ir dominio→modelos.

## 3. Mapeo dominio ↔ persistencia con Mapster

**Elección: Mapster 7.4.0** (NuGet en `IdentidadServicio.Infraestructura.csproj`). Mapperly se descartó porque el caso de uso aquí es asimétrico (1 entidad de dominio se divide en 3 modelos EF Core, con value objects y herencia), y los `[MapperIgnoreSource]` por propiedad/destino lo hacían más ruidoso que útil. Mapster encaja porque permite:

- Configurar transformaciones de **value objects** vía `.Map(d => d.NombreUsuario, s => s.NombreUsuario.Valor)`.
- Configurar la **jerarquía** con un genérico `ConfigurarMapeoDominioAModelos<T>() where T : Usuario`.
- Reconstruir la entidad de dominio (constructor con VOs) vía `.ConstructUsing(t => new Administrador(...))`.

Donde se ve Mapster en uso real (todo en `IdentidadMapeador.cs`):

## 4. HU01 — Flujo de inicio de sesión

```
Frontend → POST /api/autenticacion/iniciar-sesion {nombreUsuario, contrasena}
        ↓
AutenticacionControlador.IniciarSesion(InicioSesionDto)
        ↓
IniciarSesionComando → _mediador.Send(...)
        ↓
IniciarSesionManejador:
  1. ObtenerPorNombreUsuarioAsync (IRepositorioIdentidad)
  2. usuario.ValidarPuedeIniciarSesion()          ← dominio
  3. IniciarSesionAsync (IProveedorIdentidad → Keycloak)
  4. DtoMapeador.AUsuarioAutenticado(usuario)
  5. DtoMapeador.ResolverRutaPorRol(usuario.Rol)
        ↓
ResultadoInicioSesionDto { tokenAcceso, tokenRefresco, expiraEn, tipoToken, usuario, rutaRedireccion }
```

Rutas por rol:
- Administrador → `/administrador`
- Operador → `/operador/sesiones`
- Participante → `/participante/sesiones`

## 5. Endpoints

| Método | Ruta                                  | Auth                          |
|--------|---------------------------------------|-------------------------------|
| POST   | `/api/autenticacion/iniciar-sesion`   | Anónimo                       |
| GET    | `/api/autenticacion/perfil-actual`    | Bearer                        |
| POST   | `/api/usuarios`                       | Ver §5.1 (Strategy+Factory)   |
| GET    | `/salud`                              | Anónimo                       |

### 5.1 Creación de usuarios — Strategy + Factory

Existe **un solo endpoint** `POST /api/usuarios` que recibe `CrearUsuarioDto` con `TipoUsuario` como enum (`"Administrador" | "Operador" | "Participante"` aceptado como string vía `JsonStringEnumConverter`).

Cuerpo de ejemplo:

```json
{
  "tipoUsuario": "Operador",
  "nombreUsuario": "operador01",
  "correo": "operador@umbral.com",
  "contrasena": "Operador123*",
  "nombre": "Olivia",
  "apellido": "Op",
  "sexo": "Femenino",
  "fechaNacimiento": "1990-01-01T00:00:00Z",
  "datosContacto": { "direccion": "Calle 1", "telefono": "555-1234" },
  "codigoOperador": "OP-001"
}
```

Keycloak recibe **`username = nombreUsuario` y `email = correo` separados** (no se usa email-as-username). La credencial se envía con `temporary = false`: el Operador podrá entrar con esa misma contraseña sin que Keycloak le pida cambiarla al primer login.

```
CrearUsuarioDto (TipoUsuario, NombreUsuario, Email, Contrasena, Nombre, Apellido,
                 Direccion?, Telefono?, Sexo, FechaNacimiento,
                 CodigoAdministrador?, CodigoOperador?, Alias?)
        ↓
UsuariosControlador → _mediador.Send(new CrearUsuarioComando(dto))
        ↓
CrearUsuarioManejador:
   1. fabrica.Obtener(dto.TipoUsuario)         ← Patrón Factory
        → EstrategiaCrearAdministrador
        → EstrategiaCrearOperador              ← Patrón Strategy
        → EstrategiaCrearParticipante
   2. Validar duplicados (NombreUsuario / Email).
   3. fechaRegistro = reloj.ObtenerFechaHoraUtc().     ← IProveedorFechaHora
   4. idKeycloak = proveedor.CrearUsuarioAsync(...).
   5. proveedor.AsignarRolAsync(idKeycloak, estrategia.ObtenerRol()).
   6. usuario = estrategia.CrearUsuarioDominio(dto, fechaRegistro).
   7. estrategia.GuardarAsync(usuario, idKeycloak, repositorio, ct).
        → repositorio.Guardar{Admin|Operador|Participante}Async
   8. Si falla algo después de Keycloak → compensación EliminarUsuarioAsync.
```

El manejador **no tiene `switch` por rol**: la selección polimórfica vive en la fábrica + estrategias. Añadir un nuevo tipo de usuario en el futuro implica una nueva `IEstrategiaCreacionUsuario` registrada en DI; nada más.

Estructura nueva en Aplicación:

```
IdentidadServicio.Aplicacion/
├── Estrategias/
│   ├── IEstrategiaCreacionUsuario.cs
│   ├── EstrategiaCrearAdministrador.cs
│   ├── EstrategiaCrearOperador.cs
│   ├── EstrategiaCrearParticipante.cs
│   └── BaseEstrategia.cs            (helper interno DRY)
├── Fabricas/
│   └── FabricaEstrategiaCreacionUsuario.cs
└── CasosDeUso/
    ├── Comandos/CrearUsuarioComando.cs
    └── Manejadores/CrearUsuarioManejador.cs
```

## 6. Modelo ER (base de datos)

```
usuarios (id, nombre_usuario, id_keycloak, rol, estado, fecha_registro)
   1 ─── 1
personas (id, usuario_id ★, nombre, apellido, direccion?, telefono?, email?, sexo, fecha_nacimiento, fecha_registro)
   1 ─── 0..1   administradores (id, persona_id ★, codigo_administrador?, fecha_registro)
   1 ─── 0..1   operadores      (id, persona_id ★, codigo_operador, fecha_registro)
   1 ─── 0..1   participantes   (id, persona_id ★, alias, fecha_registro)

★ índice único — relación 1 a 1
```

**No hay columna de contraseña**: vive solo en Keycloak.

## 7. Comandos

### Compilar y probar

```powershell
dotnet build backend/servicios/identidad-servicio/IdentidadServicio.sln
dotnet test  backend/servicios/identidad-servicio/IdentidadServicio.sln
```

### Migraciones EF Core

La migración inicial `20260516000001_InicialIdentidad` ya está en el repo y se aplica automáticamente al arranque vía `SembradorIdentidad.SembrarAsync` (`Database.MigrateAsync` con reintentos).

Si quieres regenerarla desde cero (requiere `dotnet-ef`):

```powershell
# 1. Borrar carpeta Migraciones existente
Remove-Item -Recurse backend/servicios/identidad-servicio/IdentidadServicio.Infraestructura/Persistencia/Migraciones

# 2. Crear migración nueva
dotnet ef migrations add InicialIdentidad `
  --project backend/servicios/identidad-servicio/IdentidadServicio.Infraestructura `
  --startup-project backend/servicios/identidad-servicio/IdentidadServicio.Api `
  --output-dir Persistencia/Migraciones
```

### Docker

```powershell
docker compose down
docker compose up --build
docker compose ps
docker compose logs -f identidad-servicio
```

## 8. Usuarios sembrados

| NombreUsuario   | Contraseña    | Rol           | Estado    |
|-----------------|---------------|---------------|-----------|
| `administrador` | `Temporal123*`| Administrador | Activo    |
| `operador`      | `Temporal123*`| Operador      | Activo    |
| `participante`  | `Temporal123*`| Participante  | Activo    |
| `inactivo`      | `Temporal123*`| Participante  | Inactivo  |

## 9. Accesos

- **Frontend web**: http://localhost:3000
- **API Gateway**: http://localhost:5000
- **Swagger identidad**: http://localhost:5001/swagger
- **Keycloak**: http://localhost:8080  (admin/admin)
- **Postgres identidad**: localhost:5433 (umbral/umbral123)
- **Postgres keycloak**: localhost:5434 (keycloak/keycloak123)

## 10. Verificación rápida

```powershell
curl -X POST http://localhost:5000/api/autenticacion/iniciar-sesion `
     -H "Content-Type: application/json" `
     -d '{"nombreUsuario":"administrador","contrasena":"Temporal123*"}'
```

Debe devolver `tokenAcceso`, `rutaRedireccion: "/administrador"`, `usuario.nombre: "Ada"`, `usuario.apellido: "Administradora"`.

## 10.1 HU02 — Registrar usuarios desde panel administrador

**Como** Administrador, **quiero** registrar usuarios (Administrador u Operador) con sus datos y rol asignado, **para que** dichos usuarios puedan acceder a la plataforma. Participante se registra desde la app móvil (HU03).

### Pantalla "Registrar usuario"

`/administrador/usuarios/registrar` — un único formulario con selector de rol (Administrador / Operador). Participante no aparece. El formulario muestra campos comunes y, según el rol seleccionado, **muestra (no edita)** el campo `Código de operador` o `Código de administrador`. El usuario nunca escribe el código: el backend lo genera y lo devuelve en la respuesta.

### Generación automática de códigos (HU02)

Los códigos `OP-###` (Operador) y `AD-###` (Administrador) los genera el backend en `IdentidadServicio.Aplicacion/Generadores/`:

```
Generadores/
├── IGeneradorCodigoUsuario.cs
└── GeneradorCodigoUsuario.cs   ← consulta último código vía IRepositorioIdentidad
```

- Sin operadores previos → `OP-001`. Si último = `OP-009` → `OP-010`. Si último = `OP-099` → `OP-100`.
- Sin administradores previos → `AD-001`. Si último = `AD-007` → `AD-008`.
- Las estrategias (`EstrategiaCrearOperador`, `EstrategiaCrearAdministrador`) reciben `IGeneradorCodigoUsuario` por DI y llaman `GenerarCodigoOperadorAsync` / `GenerarCodigoAdministradorAsync` dentro de `CrearUsuarioDominioAsync`. El `CrearUsuarioDto` ya no expone `CodigoOperador` ni `CodigoAdministrador`; si llegan por JSON, se ignoran.
- Índice único filtrado sobre `Administrador.codigo_administrador` (migración `20260519000001_AgregarCodigosUnicosUsuario`); el de `Operador.codigo_operador` ya existía. Una carrera concurrente que produzca el mismo código se rechaza en DB; el manejador compensa Keycloak.
- La respuesta incluye `codigo`:

```json
{
  "id": "…",
  "nombreUsuario": "operador02",
  "correo": "operador02@gmail.com",
  "rol": "Operador",
  "estado": "Activo",
  "codigo": "OP-002",
  "mensaje": "Operador registrado correctamente. Código generado: OP-002"
}
```

### Reutilización (sin caso de uso nuevo)

HU02 reutiliza la maquinaria de HU01 — no se crean `CrearOperadorComando`/`CrearOperadorManejador` separados:

- `CrearUsuarioDto` con `TipoUsuario = "Administrador" | "Operador"` — sin `CodigoOperador` ni `CodigoAdministrador` (los genera el backend).
- `CrearUsuarioComando` + `CrearUsuarioManejador`.
- `EstrategiaCrearAdministrador` / `EstrategiaCrearOperador` (Strategy) + `FabricaEstrategiaCreacionUsuario` (Factory). La estrategia de `Participante` queda registrada para HU03.
- Endpoint único: `POST /api/usuarios`.

### Capa de validación reutilizable

Toda regla de campo, duplicado y rango se concentra en `IdentidadServicio.Aplicacion/Validaciones/`:

```
Validaciones/
├── ErrorValidacion.cs              ← { campo, mensaje }
├── ResultadoValidacion.cs          ← acumulador con .Agregar()
├── ExcepcionValidacion.cs          ← se lanza si hay errores; el middleware la mapea a 400
├── MensajesValidacionUsuario.cs    ← catálogo de mensajes (1 sola fuente de verdad)
├── IValidadorCrearUsuario.cs
└── ValidadorCrearUsuario.cs        ← campos + duplicados + reglas por TipoUsuario
```

`CrearUsuarioManejador` llama a `await _validador.ValidarAsync(dto, ct)` antes de tocar Keycloak/PostgreSQL. Si hay errores, lanza `ExcepcionValidacion` que el middleware convierte a:

```json
{
  "codigo": "VALIDACION",
  "mensaje": "Existen errores de validación.",
  "errores": [
    { "campo": "datosContacto.telefono", "mensaje": "El teléfono ya está registrado." },
    { "campo": "datosContacto.direccion", "mensaje": "La dirección es obligatoria." },
    { "campo": "contrasena", "mensaje": "La contraseña debe contener al menos un número." }
  ]
}
```

El frontend mapea tanto los nombres planos (`telefono`, `direccion`) como los punteados (`datosContacto.telefono`, `datosContacto.direccion`) al input correcto. Si la respuesta del backend no trae `errores` ni `mensaje`, la página muestra el genérico `"No fue posible registrar el usuario."`.

### Reglas que cubre el validador

| Campo            | Reglas |
|------------------|--------|
| `nombreUsuario`  | obligatorio · 4-30 caracteres · letras, números, `.` o `_` · único en base de datos |
| `correo`         | obligatorio · formato `x@y.z` · único en base de datos |
| `contrasena`     | obligatoria · 5-10 caracteres · ≥1 dígito · ≥1 carácter especial (`!@#$%^&*_-.?`) · **nunca** se guarda en PostgreSQL (solo se envía a Keycloak con `temporary = false`) |
| `nombre`         | obligatorio · 2-50 caracteres · solo letras y espacios (acepta acentos y compuestos como "José Luis") |
| `apellido`       | obligatorio · 2-50 caracteres · solo letras y espacios (acepta "Di Martino") |
| `datosContacto.telefono` | obligatorio · 11 dígitos · comienza con `0414`/`0412`/`0424`/`0416`/`0426`/`0212` · normalizado sin espacios ni guiones · único en base de datos |
| `datosContacto.direccion` | obligatoria · mínimo 5 caracteres |
| `fechaNacimiento`| obligatoria · no futura · entre 18 y 100 años (usa `IProveedorFechaHora`) |
| `sexo`           | opcional · si viene, debe ser `Masculino`/`Femenino`/`Otro`/`Indefinido` |
| `tipoUsuario`    | desde web solo `Administrador` u `Operador`; `Participante` → 400 |
| `codigoOperador` / `codigoAdministrador` | **No se piden al frontend** — el backend los genera (`OP-###` / `AD-###`) y los devuelve en `codigo` |

### Duplicados — barreras por capa

- **Aplicación** (validador): `ExisteNombreUsuarioAsync`, `ExisteCorreoAsync`, `ExisteTelefonoAsync` consultan el repositorio. Devuelven mensajes específicos por campo.
- **Infraestructura** (EF Core / PostgreSQL): índices únicos como última línea de defensa.
  - `Usuario.nombre_usuario` único
  - `Usuario.id_keycloak` único
  - `Persona.usuario_id` único
  - `Persona.correo` único
  - `Persona.telefono` único filtrado (`WHERE telefono IS NOT NULL`) — añadido por la migración `20260518000001_AgregarValidacionesUnicasUsuario`.

### Autorización

Endpoint `[AllowAnonymous]` con verificación dentro del controlador: si `TipoUsuario != Participante` exige `PoliticaAdministrador`. Tabla:

| Token enviado    | TipoUsuario en el DTO  | Resultado          |
|------------------|------------------------|--------------------|
| ninguno          | `Operador`/`Administrador` | `401 Unauthorized` |
| `Participante`   | `Operador`             | `403 Forbidden`    |
| `Operador`       | `Operador`             | `403 Forbidden`    |
| `Administrador`  | `Operador`/`Administrador` | `201 Created`      |
| `Administrador`  | `Participante`         | `400` (rechazado por validador para registro web) |

### Autorización

El endpoint es `[AllowAnonymous]` para preservar el registro público de Participante (HU03 móvil), pero el controlador exige token de Administrador cuando `TipoUsuario != Participante`:

| Token enviado          | TipoUsuario en el DTO  | Resultado          |
|------------------------|------------------------|--------------------|
| ninguno                | `Operador`             | `401 Unauthorized` |
| `Participante`         | `Operador`             | `403 Forbidden`    |
| `Operador`             | `Operador`             | `403 Forbidden`    |
| `Administrador`        | `Operador`             | `201 Created`      |
| ninguno                | `Participante`         | `201 Created`      |

Se evalúa con `IAuthorizationService.AuthorizeAsync(User, "PoliticaAdministrador")` (la política ya existe desde HU01 en `RegistroSeguridad`). El controlador sólo decide autenticación/autorización — la lógica de creación sigue en MediatR + Strategy.

### Flujo (Administrador en panel web)

```
PaginaInicioSesion           → POST /api/autenticacion/login-web  (Bearer guardado)
       ↓
PaginaAdministrador          → enlace "Registrar usuario"
       ↓
PaginaRegistrarUsuario       → POST /api/usuarios + Authorization: Bearer ...
                               body: { tipoUsuario, nombreUsuario, correo, contrasena,
                                       nombre, apellido, sexo, fechaNacimiento,
                                       datosContacto: { direccion, telefono },
                                       codigoOperador? , codigoAdministrador? }
UsuariosControlador          → exige Administrador para TipoUsuario ≠ Participante
       ↓
CrearUsuarioManejador        → ValidadorCrearUsuario.ValidarAsync(dto, ct)
                               (campos + duplicados + edad + tipo web)
                             → Keycloak.CrearUsuarioAsync (temporary = false)
                             → AsignarRolAsync(idKc, "Operador"|"Administrador")
                             → estrategia.GuardarAsync (Usuario+Persona+rol)
                               Activo por defecto · Si falla DB → compensa Keycloak
       ↓
Frontend muestra "Usuario {nombre} registrado correctamente con rol {Rol}."
Si 400 con errores por campo → cada input muestra su mensaje.
```

### Cuerpos de ejemplo

Operador (sin `codigoOperador`):

```json
{
  "tipoUsuario": "Operador",
  "nombreUsuario": "operador02",
  "correo": "operador02@gmail.com",
  "contrasena": "Abc1*",
  "nombre": "Angelo",
  "apellido": "Di Martino",
  "sexo": "Masculino",
  "fechaNacimiento": "2000-10-24T00:00:00Z",
  "datosContacto": { "direccion": "El Paraiso, Caracas, Venezuela", "telefono": "04143710260" }
}
```

Administrador (sin `codigoAdministrador`):

```json
{
  "tipoUsuario": "Administrador",
  "nombreUsuario": "admin02",
  "correo": "admin02@gmail.com",
  "contrasena": "Adm1*",
  "nombre": "Ana",
  "apellido": "Perez",
  "sexo": "Femenino",
  "fechaNacimiento": "1995-05-10T00:00:00Z",
  "datosContacto": { "direccion": "Caracas", "telefono": "04121234567" }
}
```
- Si la persistencia falla tras crear en Keycloak → compensación `EliminarUsuarioAsync`.

### Validación manual (frontend)

1. `docker compose up -d` y `npm run dev` en `frontend-web/umbral-web-react`.
2. Iniciar sesión como `administrador01` / `Temporal123*` en `http://localhost:3000`.
3. Click en **"Registrar usuario"** en el panel de administración.
4. Verificar que el selector de rol ofrece **solo** `Administrador` y `Operador` (Participante no aparece).
5. Al elegir Operador aparece **Código de operador**; al elegir Administrador aparece **Código de administrador**.
6. Probar: vacío, contraseña sin número, teléfono `4143710260` (10 dígitos), menor de 18 — verificar que cada campo muestra su mensaje específico.
7. Llenar correctamente y enviar → mensaje de éxito con `nombreUsuario` y `rol`.
8. Cerrar sesión y entrar con las credenciales del nuevo usuario.
9. Repetir con correo o teléfono ya existente → mensaje específico debajo del campo correspondiente.

### Pruebas backend (HU02)

Unitarias en `Validaciones/ValidadorCrearUsuarioPruebas`: 27 escenarios — vacíos, formato, longitud, dígito/especial, edad 18-100, duplicados, código por rol, registro web rechaza Participante, casos felices Operador/Administrador.

Unitarias en `Manejadores/CrearUsuarioManejadorPruebas`: orquestación (validador mockeado) — manejador delega en validador, compensa Keycloak ante fallo de DB, usa `IProveedorFechaHora`, `Activo` por defecto.

Integración en `UsuariosEndpointPruebas`: 401/403/201 según token, errores por campo en HTTP 400 para correo duplicado, teléfono inválido, teléfono duplicado, contraseña inválida, código de operador vacío, registro de Participante desde web.

Integración en `FlujoHU02Pruebas`: registro de Operador como Administrador y login posterior por `/api/autenticacion/login-web`.

Las pruebas de integración reemplazan JwtBearer por `AuthHandlerPruebas` (cabecera `X-Rol-Prueba`) para no depender de Keycloak real.

---

## 11. Checklist final

- [x] Dominio NO es igual a la base de datos.
- [x] `Usuario` abstracto con `Administrador/Operador/Participante` heredando.
- [x] EF Core usa modelos planos (no TPH/TPT/TPC).
- [x] `Aplicacion` no depende de EF Core (sólo `IRepositorioIdentidad` + `IProveedorIdentidad`).
- [x] `Api` no tiene lógica de negocio (controladores → `_mediador.Send`).
- [x] `Commons` sólo tiene carpeta `Dtos/`.
- [x] `IdentidadMapeador` realmente traduce dominio ↔ persistencia (1↔3).
- [x] **Sin `FechaActualizacion`** en dominio/modelos/migración/pruebas.
- [x] Contraseña vive solo en Keycloak.
- [x] `dotnet build` correcto.
- [x] `dotnet test` → **86 unitarias + 20 integración = 106/106 verdes** (HU01 + HU02 + códigos auto-generados + dirección obligatoria + errores por campo con `codigo: "VALIDACION"`).
- [x] HU02: códigos `OP-###` / `AD-###` generados por backend (`IGeneradorCodigoUsuario`). El DTO ya no los recibe; el frontend los muestra como campo no editable y los obtiene en `respuesta.codigo`.
- [x] HU02: `POST /api/usuarios` exige Administrador para `TipoUsuario ≠ Participante`.
- [x] HU02: Operador creado puede iniciar sesión por `/api/autenticacion/login-web` (cubierto por `FlujoHU02Pruebas`).
- [x] HU02: panel web muestra el formulario en `/administrador/usuarios/registrar`. Selector de rol limita a Administrador/Operador (sin Participante).
- [x] HU02: validador reutilizable en `Aplicacion/Validaciones/` con mensajes por campo y duplicados (`nombreUsuario`, `correo`, `telefono`).
- [x] HU02: índice único filtrado sobre `Persona.telefono` (migración `20260518000001_AgregarValidacionesUnicasUsuario`).
- [x] HU02: `ExcepcionValidacion` → middleware → `400 { mensaje, errores: [{ campo, mensaje }] }`.

## 12. verificar app movil
cd frontend-movil/umbral-app-react-native
npm install
npx expo start -c --tunnel
acomodar la ip en app.json y clienteapi.ts

Entrar desde expo go en el celular