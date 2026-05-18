# UMBRAL — HU01: Inicio de sesión

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
  "contrasenaTemporal": "Temporal123*",
  "nombre": "Olivia",
  "apellido": "Op",
  "sexo": "Femenino",
  "fechaNacimiento": "1990-01-01T00:00:00Z",
  "datosContacto": { "direccion": "Calle 1", "telefono": "555-1234" },
  "codigoOperador": "OP-001"
}
```

Keycloak recibe **`username = nombreUsuario` y `email = correo` separados** (no se usa email-as-username).

```
CrearUsuarioDto (TipoUsuario, NombreUsuario, Email, ContrasenaTemporal, Nombre, Apellido,
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
- [x] `dotnet test` → **18 unitarias + 5 integración = 23/23 verdes**.

## 12. verificar app movil
cd frontend-movil/umbral-app-react-native
npm install
npx expo start -c --tunnel
acomodar la ip en app.json y clienteapi.ts

Entrar desde expo go en el celular