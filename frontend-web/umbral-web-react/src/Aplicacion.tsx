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
import { PaginaListaBusquedas } from './paginas/PaginaListaBusquedas'
import { PaginaCrearBusqueda } from './paginas/PaginaCrearBusqueda'
import { PaginaGestionEtapas } from './paginas/PaginaGestionEtapas'
import { PaginaSesiones } from './paginas/PaginaSesiones'
import { PaginaCrearSesion } from './paginas/PaginaCrearSesion'
import { PaginaDetalleSesion } from './paginas/PaginaDetalleSesion'
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

      <Route path="/administrador/busquedas/activas" element={<Navigate to="/administrador/busquedas" replace />} />

      {/* HU21/22/23 — Búsquedas del tesoro accesibles también por el Administrador. */}
      <Route
        path="/administrador/busquedas"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaListaBusquedas />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/busquedas/crear"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaCrearBusqueda />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/busquedas/:busquedaId/etapas"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaGestionEtapas />
          </RutaProtegida>
        }
      />

      {/* HU15/18/19/20 — Trivias accesibles también por el Administrador. */}
      <Route
        path="/administrador/trivias"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaListaTrivias />
          </RutaProtegida>
        }
      />
      <Route path="/administrador/trivias/activas" element={<Navigate to="/administrador/trivias" replace />} />
      <Route
        path="/administrador/trivias/crear"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaCrearTrivia />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/trivias/:triviaId/preguntas"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaGestionPreguntas />
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

      {/* HU33 — Sesiones en vivo (Administrador). */}
      <Route
        path="/administrador/sesiones"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaSesiones />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/sesiones/crear"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaCrearSesion />
          </RutaProtegida>
        }
      />
      {/* HU34 — Detalle de sesión (Administrador). */}
      <Route
        path="/administrador/sesiones/:id"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaDetalleSesion />
          </RutaProtegida>
        }
      />

      {/* HU33 — Sesiones en vivo (Operador). */}
      <Route
        path="/operador/sesiones"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaSesiones />
          </RutaProtegida>
        }
      />
      <Route
        path="/operador/sesiones/crear"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaCrearSesion />
          </RutaProtegida>
        }
      />
      {/* HU34 — Detalle de sesión (Operador). */}
      <Route
        path="/operador/sesiones/:id"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaDetalleSesion />
          </RutaProtegida>
        }
      />

      <Route path="*" element={<Navigate to="/iniciar-sesion" replace />} />
    </Routes>
  )
}
