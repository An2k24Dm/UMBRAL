import { Navigate, Route, Routes } from 'react-router-dom'
import { PaginaInicioSesion } from './paginas/PaginaInicioSesion'
import { PaginaAdministrador } from './paginas/PaginaAdministrador'
import { PaginaRegistrarUsuario } from './paginas/PaginaRegistrarUsuario'
import { PaginaOperador } from './paginas/PaginaOperador'
import { PaginaParticipante } from './paginas/PaginaParticipante'
import { PaginaPerfilUsuarioAutenticado } from './paginas/PaginaPerfilUsuarioAutenticado'
import { PaginaListaParticipantes } from './paginas/PaginaListaParticipantes'
import { PaginaListaUsuariosInternos } from './paginas/PaginaListaUsuariosInternos'
import { PaginaDetalleUsuario } from './paginas/PaginaDetalleUsuario'
import { obtenerDetalleUsuarioInterno } from './autenticacion/clienteApi'
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
            <PaginaListaParticipantes rutaBaseDetalle="/administrador/usuarios" />
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
          /api/usuarios/internos/{id} que excluye Participantes. */}
      <Route
        path="/administrador/usuarios/internos/:id"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaDetalleUsuario
              rolesPermitidosVista={['Operador', 'Administrador']}
              obtenerUsuario={obtenerDetalleUsuarioInterno}
            />
          </RutaProtegida>
        }
      />
      <Route
        path="/administrador/usuarios/:id"
        element={
          <RutaProtegida rolesPermitidos={['Administrador']}>
            <PaginaDetalleUsuario rolesPermitidosVista={['Participante', 'Operador', 'Administrador']} />
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
            <PaginaListaParticipantes rutaBaseDetalle="/operador/usuarios" />
          </RutaProtegida>
        }
      />
      <Route
        path="/operador/usuarios/:id"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaDetalleUsuario rolesPermitidosVista={['Participante']} />
          </RutaProtegida>
        }
      />
      {/* Compatibilidad con el destino anterior usado por la respuesta de inicio
          de sesión (rutaRedireccion = /operador/sesiones). Redirige al dashboard. */}
      <Route
        path="/operador/sesiones"
        element={<Navigate to="/operador" replace />}
      />

      {/* ----- Participante (sin panel web; se mantiene la lógica actual) ----- */}
      <Route
        path="/participante/sesiones"
        element={
          <RutaProtegida rolesPermitidos={['Participante']}>
            <PaginaParticipante />
          </RutaProtegida>
        }
      />

      <Route path="*" element={<Navigate to="/iniciar-sesion" replace />} />
    </Routes>
  )
}
