import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { Aplicacion } from './Aplicacion'
import { ProveedorAutenticacion } from './autenticacion/ProveedorAutenticacion'
import './estilos.css'

ReactDOM.createRoot(document.getElementById('raiz')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <ProveedorAutenticacion>
        <Aplicacion />
      </ProveedorAutenticacion>
    </BrowserRouter>
  </React.StrictMode>
)
