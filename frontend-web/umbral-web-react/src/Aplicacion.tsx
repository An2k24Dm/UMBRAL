import { Navigate, Route, Routes } from 'react-router-dom'
import { PaginaInicioSesion } from './paginas/PaginaInicioSesion'
import { PaginaAdministrador } from './paginas/PaginaAdministrador'
import { PaginaRegistrarUsuario } from './paginas/PaginaRegistrarUsuario'
import { PaginaOperador } from './paginas/PaginaOperador'
import { PaginaParticipante } from './paginas/PaginaParticipante'
import { RutaProtegida } from './autenticacion/RutaProtegida'

export function Aplicacion() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/iniciar-sesion" replace />} />
      <Route path="/iniciar-sesion" element={<PaginaInicioSesion />} />

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
        path="/operador/sesiones"
        element={
          <RutaProtegida rolesPermitidos={['Operador']}>
            <PaginaOperador />
          </RutaProtegida>
        }
      />
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
