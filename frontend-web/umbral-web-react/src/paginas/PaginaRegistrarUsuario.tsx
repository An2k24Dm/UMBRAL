import { useMemo, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  ErrorValidacionRegistro,
  registrarUsuario,
  type DatosNuevoUsuario,
  type TipoUsuarioRegistro
} from '../autenticacion/clienteApi'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { Boton } from '../componentes/Boton'
import { Alerta } from '../componentes/Alerta'

// HU02: registro de usuarios desde panel administrador.
// Solo Administrador puede acceder. El formulario permite crear Administrador
// u Operador; el código OP-### / AD-### lo genera el backend y se muestra como
// campo de solo lectura.

const ESTADO_INICIAL: DatosNuevoUsuario = {
  tipoUsuario: 'Operador',
  nombreUsuario: '',
  correo: '',
  contrasena: '',
  nombre: '',
  apellido: '',
  sexo: 'Femenino',
  fechaNacimiento: '1995-01-01',
  direccion: '',
  telefono: ''
}

const CODIGOS_TELEFONO = ['0414', '0412', '0424', '0416', '0426', '0212']
const CARACTERES_ESPECIALES = '!@#$%^&*_-.?'

type Errores = Partial<Record<keyof DatosNuevoUsuario | 'general', string>>

const MAPA_CAMPOS_BACKEND: Record<string, keyof DatosNuevoUsuario> = {
  nombreUsuario: 'nombreUsuario',
  correo: 'correo',
  contrasena: 'contrasena',
  nombre: 'nombre',
  apellido: 'apellido',
  sexo: 'sexo',
  fechaNacimiento: 'fechaNacimiento',
  telefono: 'telefono',
  'datosContacto.telefono': 'telefono',
  direccion: 'direccion',
  'datosContacto.direccion': 'direccion'
}

function mapearCampoBackend(campo: string): keyof DatosNuevoUsuario | null {
  return MAPA_CAMPOS_BACKEND[campo] ?? null
}

function calcularEdad(fechaIso: string): number {
  const nac = new Date(fechaIso)
  const hoy = new Date()
  let edad = hoy.getFullYear() - nac.getFullYear()
  const mes = hoy.getMonth() - nac.getMonth()
  if (mes < 0 || (mes === 0 && hoy.getDate() < nac.getDate())) edad--
  return edad
}

function validarLocal(datos: DatosNuevoUsuario): Errores {
  const e: Errores = {}

  if (!datos.nombreUsuario.trim()) e.nombreUsuario = 'El nombre de usuario es obligatorio.'
  else if (!/^[a-zA-Z0-9._]{4,30}$/.test(datos.nombreUsuario.trim()))
    e.nombreUsuario =
      'Debe tener entre 4 y 30 caracteres y solo letras, números, punto o guion bajo.'

  if (!datos.correo.trim()) e.correo = 'El correo es obligatorio.'
  else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(datos.correo.trim()))
    e.correo = 'El correo no tiene un formato válido.'

  if (!datos.contrasena) e.contrasena = 'La contraseña es obligatoria.'
  else {
    if (datos.contrasena.length < 5 || datos.contrasena.length > 10)
      e.contrasena = 'La contraseña debe tener entre 5 y 10 caracteres.'
    else if (!/\d/.test(datos.contrasena))
      e.contrasena = 'La contraseña debe contener al menos un número.'
    else if (![...datos.contrasena].some((c) => CARACTERES_ESPECIALES.includes(c)))
      e.contrasena = 'La contraseña debe contener al menos un carácter especial.'
  }

  if (!datos.nombre.trim()) e.nombre = 'El nombre es obligatorio.'
  else if (!/^[A-Za-zÁÉÍÓÚáéíóúÑñÜü\s]{2,50}$/.test(datos.nombre.trim()))
    e.nombre = 'El nombre solo puede contener letras y espacios (2 a 50 caracteres).'

  if (!datos.apellido.trim()) e.apellido = 'El apellido es obligatorio.'
  else if (!/^[A-Za-zÁÉÍÓÚáéíóúÑñÜü\s]{2,50}$/.test(datos.apellido.trim()))
    e.apellido = 'El apellido solo puede contener letras y espacios (2 a 50 caracteres).'

  const telLimpio = datos.telefono.replace(/[\s-]/g, '')
  if (!telLimpio) e.telefono = 'El teléfono es obligatorio.'
  else if (!/^\d+$/.test(telLimpio)) e.telefono = 'El teléfono debe contener solo números.'
  else if (telLimpio.length !== 11) e.telefono = 'El teléfono debe tener 11 dígitos.'
  else if (!CODIGOS_TELEFONO.some((c) => telLimpio.startsWith(c)))
    e.telefono = 'Debe comenzar con un código válido (0414, 0412, 0424, 0416, 0426 o 0212).'

  const direccion = datos.direccion.trim()
  if (!direccion) e.direccion = 'La dirección es obligatoria.'
  else if (direccion.length < 5) e.direccion = 'La dirección debe tener al menos 5 caracteres.'

  if (!datos.fechaNacimiento) e.fechaNacimiento = 'La fecha de nacimiento es obligatoria.'
  else {
    const edad = calcularEdad(datos.fechaNacimiento)
    if (edad < 0) e.fechaNacimiento = 'La fecha de nacimiento no puede ser futura.'
    else if (edad < 18) e.fechaNacimiento = 'El usuario debe tener al menos 18 años.'
    else if (edad > 100) e.fechaNacimiento = 'El usuario no puede tener más de 100 años.'
  }

  return e
}

export function PaginaRegistrarUsuario() {
  const { token, cerrar } = usarAutenticacion()
  const navegar = useNavigate()
  const [datos, setDatos] = useState<DatosNuevoUsuario>(ESTADO_INICIAL)
  const [errores, setErrores] = useState<Errores>({})
  const [exito, setExito] = useState<string | null>(null)
  const [codigoGenerado, setCodigoGenerado] = useState<string | null>(null)
  const [cargando, setCargando] = useState(false)
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const actualizar = <K extends keyof DatosNuevoUsuario>(campo: K, valor: DatosNuevoUsuario[K]) => {
    setDatos((d) => ({ ...d, [campo]: valor }))
    setErrores((prev) => ({ ...prev, [campo]: undefined }))
  }

  const cambiarRol = (rol: TipoUsuarioRegistro) => {
    setDatos((d) => ({ ...d, tipoUsuario: rol }))
    setErrores({})
    setCodigoGenerado(null)
  }

  const erroresLocales = useMemo(() => validarLocal(datos), [datos])

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault()
    setErrorGeneral(null)
    setExito(null)
    setCodigoGenerado(null)

    if (!token) {
      setErrorGeneral('Debe iniciar sesión como administrador.')
      return
    }

    if (Object.values(erroresLocales).some((v) => v)) {
      setErrores(erroresLocales)
      setErrorGeneral('Revise los campos marcados.')
      return
    }

    setCargando(true)
    try {
      const fechaIso = new Date(datos.fechaNacimiento + 'T00:00:00Z').toISOString()
      const telefonoLimpio = datos.telefono.replace(/[\s-]/g, '')
      const respuesta = await registrarUsuario(
        { ...datos, telefono: telefonoLimpio, fechaNacimiento: fechaIso },
        token
      )
      const codigo = respuesta.codigo ?? ''
      setCodigoGenerado(codigo)
      setExito(
        codigo
          ? `Usuario ${respuesta.nombreUsuario} registrado correctamente con rol ${respuesta.rol}. Código generado: ${codigo}`
          : `Usuario ${respuesta.nombreUsuario} registrado correctamente con rol ${respuesta.rol}.`
      )
      setDatos({ ...ESTADO_INICIAL, tipoUsuario: datos.tipoUsuario })
      setErrores({})
    } catch (e) {
      if (e instanceof ErrorValidacionRegistro && e.errores.length > 0) {
        const nuevos: Errores = {}
        for (const err of e.errores) {
          const clave = mapearCampoBackend(err.campo)
          if (clave) nuevos[clave] = err.mensaje
        }
        setErrores(nuevos)
        setErrorGeneral('No fue posible registrar el usuario. Revise los campos marcados.')
      } else if (e instanceof Error && e.message && e.message.trim().length > 0) {
        setErrorGeneral(e.message)
        if (e.message.includes('iniciar sesión')) {
          cerrar()
          navegar('/iniciar-sesion', { replace: true })
        }
      } else {
        setErrorGeneral('No fue posible registrar el usuario.')
      }
    } finally {
      setCargando(false)
    }
  }

  const etiquetaCodigo = datos.tipoUsuario === 'Operador'
    ? 'Código de operador'
    : 'Código de administrador'
  const placeholderCodigo = datos.tipoUsuario === 'Operador' ? 'OP-### (automático)' : 'AD-### (automático)'

  return (
    <LayoutPanel
      titulo="Registrar usuario"
      descripcion="Crear cuentas de Operador o Administrador. El código se genera automáticamente."
    >
      <div className="cabecera-pagina">
        <div>
          <h2 style={{ margin: 0 }}>Nuevo usuario</h2>
          <p style={{ margin: '4px 0 0', color: 'var(--color-texto-tenue)' }}>
            Complete todos los campos. Los datos se enviarán al backend para crear la cuenta.
          </p>
        </div>
        <div className="cabecera-pagina-acciones">
          <Boton variante="volver" onClick={() => navegar('/administrador')}>← Volver</Boton>
        </div>
      </div>

      <section className="seccion">
        {errorGeneral && <Alerta tono="error">{errorGeneral}</Alerta>}
        {exito && <Alerta tono="exito">{exito}</Alerta>}

        <form onSubmit={enviar} className="formulario-usuario" noValidate>
          <CampoFormulario etiqueta="Rol" htmlFor="rol">
            <select
              id="rol"
              value={datos.tipoUsuario}
              onChange={(e) => cambiarRol(e.target.value as TipoUsuarioRegistro)}
            >
              <option value="Administrador">Administrador</option>
              <option value="Operador">Operador</option>
            </select>
          </CampoFormulario>

          <CampoFormulario etiqueta={etiquetaCodigo} opcional="generado automáticamente">
            <input
              value={codigoGenerado ?? ''}
              placeholder={placeholderCodigo}
              readOnly
              disabled
              aria-readonly="true"
              className="campo-no-editable"
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Nombre de usuario" htmlFor="nombreUsuario" error={errores.nombreUsuario}>
            <input
              id="nombreUsuario"
              value={datos.nombreUsuario}
              onChange={(e) => actualizar('nombreUsuario', e.target.value)}
              required
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Correo" htmlFor="correo" error={errores.correo}>
            <input
              id="correo"
              type="email"
              value={datos.correo}
              onChange={(e) => actualizar('correo', e.target.value)}
              required
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Contraseña" htmlFor="contrasena" error={errores.contrasena}>
            <input
              id="contrasena"
              type="password"
              value={datos.contrasena}
              maxLength={10}
              onChange={(e) => actualizar('contrasena', e.target.value)}
              required
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Nombre" htmlFor="nombre" error={errores.nombre}>
            <input
              id="nombre"
              value={datos.nombre}
              onChange={(e) => actualizar('nombre', e.target.value)}
              required
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Apellido" htmlFor="apellido" error={errores.apellido}>
            <input
              id="apellido"
              value={datos.apellido}
              onChange={(e) => actualizar('apellido', e.target.value)}
              required
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Sexo" htmlFor="sexo">
            <select
              id="sexo"
              value={datos.sexo}
              onChange={(e) => actualizar('sexo', e.target.value)}
            >
              <option value="Femenino">Femenino</option>
              <option value="Masculino">Masculino</option>
              <option value="Otro">Otro</option>
              <option value="Indefinido">Indefinido</option>
            </select>
          </CampoFormulario>

          <CampoFormulario etiqueta="Fecha de nacimiento" htmlFor="fechaNacimiento" error={errores.fechaNacimiento}>
            <input
              id="fechaNacimiento"
              type="date"
              value={datos.fechaNacimiento}
              onChange={(e) => actualizar('fechaNacimiento', e.target.value)}
              required
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Dirección" htmlFor="direccion" error={errores.direccion}>
            <input
              id="direccion"
              value={datos.direccion}
              onChange={(e) => actualizar('direccion', e.target.value)}
              required
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Teléfono" htmlFor="telefono" error={errores.telefono}>
            <input
              id="telefono"
              value={datos.telefono}
              placeholder="04141356230"
              onChange={(e) => actualizar('telefono', e.target.value)}
              required
            />
          </CampoFormulario>

          <div className="acciones-formulario">
            <Boton variante="secundario" type="button" onClick={() => navegar('/administrador')}>
              Cancelar
            </Boton>
            <Boton variante="primario" type="submit" disabled={cargando}>
              {cargando ? 'Registrando…' : 'Registrar usuario'}
            </Boton>
          </div>
        </form>
      </section>
    </LayoutPanel>
  )
}
