import { Navigate, Route, Routes } from 'react-router-dom'
import { PaginaInicioSesion } from './paginas/PaginaInicioSesion'
import { PaginaAdministrador } from './paginas/PaginaAdministrador'
import { PaginaRegistrarUsuario } from './paginas/PaginaRegistrarUsuario'
import { PaginaOperador } from './paginas/PaginaOperador'
import { PaginaPerfilUsuarioAutenticado } from './paginas/PaginaPerfilUsuarioAutenticado'
import { PaginaListaParticipantes } from './paginas/PaginaListaParticipantes'
import { PaginaListaUsuariosInternos } from './paginas/PaginaListaUsuariosInternos'
import { PaginaDetalleUsuario } from './paginas/PaginaDetalleUsuario'
import { PaginaListaTrivias } from './paginas/PaginaListaTrivias'
import { PaginaCrearTrivia } from './paginas/PaginaCrearTrivia'
import { PaginaGestionPreguntas } from './paginas/PaginaGestionPreguntas'
import { PaginaListaTriviasActivas } from './paginas/PaginaListaTriviasActivas'
import { PaginaListaBusquedas } from './paginas/PaginaListaBusquedas'
import { PaginaCrearBusqueda } from './paginas/PaginaCrearBusqueda'
import { PaginaGestionEtapas } from './paginas/PaginaGestionEtapas'
import {
  obtenerDetalleParticipante,
  obtenerDetalleUsuarioInterno
} from './autenticacion/clienteApi'
import { RutaProtegida } from './autenticacion/RutaProtegida'

export function Aplicacion() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/iniciar-sesion" replace />} />
      <Route path="/iniciar-sesion" element={<PaginaInicioSesion />} />

      {/* ----- Administrador ----- */}
      <Route
        path="/administrador"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaAdministrador />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/usuarios/registrar"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaRegistrarUsuario />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/perfil"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaPerfilUsuarioAutenticado />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/usuarios/participantes"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaListaParticipantes rutaBaseDetalle="/administrador/usuarios/participantes" />
          </RutaProtegida>
        }
      />
      {/* HU07 — detalle de un Participante: usa el endpoint
          /api/usuarios/participantes/{id}. */}
      <Route
        path="/administrador/usuarios/participantes/:id"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaDetalleUsuario
              rolesPermitidosVista={['Participante']}
              obtenerUsuario={obtenerDetalleParticipante}
            />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/usuarios/internos"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaListaUsuariosInternos />
          </RutaProtegida>
        }
      />
      {/* HU08 — detalle de Operadores y Administradores: usa el endpoint
          /api/usuarios/internos/{id} que excluye Participantes.
          HU09 — habilitamos modo edición sólo para Operadores. La pantalla
          ignora la edición si el detalle resulta ser Administrador. */}
      <Route
        path="/administrador/usuarios/internos/:id"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaDetalleUsuario
              rolesPermitidosVista={['Operador', 'Administrador']}
              obtenerUsuario={obtenerDetalleUsuarioInterno}
              permiteEditarOperador
            />
          </RutaProtegida>
        }
      />

      {/* ----- Operador ----- */}
      <Route
        path="/operador"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaOperador />
          </RutaProtegida>
        }
      />
      <Route
        path="/operador/perfil"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaPerfilUsuarioAutenticado />
          </RutaProtegida>
        }
      />
      <Route
        path="/operador/usuarios/participantes"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaListaParticipantes rutaBaseDetalle="/operador/usuarios/participantes" />
          </RutaProtegida>
        }
      />
      {/* HU07 — detalle de un Participante consultado por el Operador. */}
      <Route
        path="/operador/usuarios/participantes/:id"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaDetalleUsuario
              rolesPermitidosVista={['Participante']}
              obtenerUsuario={obtenerDetalleParticipante}
            />
          </RutaProtegida>
        }
      />
      {/* HU15 — Listar trivias en borrador. */}
      <Route
        path="/operador/trivias"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaListaTrivias />
          </RutaProtegida>
        }
      />
      {/* HU15 — Crear trivia. */}
      <Route
        path="/operador/trivias/crear"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaCrearTrivia />
          </RutaProtegida>
        }
      />
      {/* HU20 — Listar trivias activas. */}
      <Route
        path="/operador/trivias/activas"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaListaTriviasActivas />
          </RutaProtegida>
        }
      />
      {/* HU16/HU17 — Gestionar preguntas de una trivia. */}
      <Route
        path="/operador/trivias/:triviaId/preguntas"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaGestionPreguntas />
          </RutaProtegida>
        }
      />
      {/* HU21 — Listar búsquedas del tesoro en borrador. */}
      <Route
        path="/operador/busquedas"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaListaBusquedas />
          </RutaProtegida>
        }
      />
      {/* HU21 — Crear búsqueda del tesoro. */}
      <Route
        path="/operador/busquedas/crear"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaCrearBusqueda />
          </RutaProtegida>
        }
      />
      {/* HU22 — Gestionar etapas de una búsqueda del tesoro. */}
      <Route
        path="/operador/busquedas/:busquedaId/etapas"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaGestionEtapas />
          </RutaProtegida>
        }
      />

      {/* Compatibilidad con el destino anterior usado por la respuesta de inicio
          de sesión (rutaRedireccion = /operador/sesiones). Redirige al dashboard. */}
      <Route
        path="/operador/sesiones"
        element={<Navigate to="/operador" replace />}
      />

      <Route path="*" element={<Navigate to="/iniciar-sesion" replace />} />
    </Routes>
  )
}
