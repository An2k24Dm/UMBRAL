import { useMemo, useState, type FormEvent } from 'react'
import {
  ErrorValidacionRegistro,
  modificarOperador,
  type ModificarOperadorPayload,
  type ModificarOperadorRespuesta
} from '../autenticacion/clienteApi'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { Alerta } from './Alerta'
import { Boton } from './Boton'
import { CampoFormulario } from './CampoFormulario'
import type { UsuarioDetalle } from '../autenticacion/tipos'

// HU09 — Formulario de edición parcial del perfil de un Operador.
//
// La estrategia es comparar el estado original (snapshot al cargar) contra el
// estado del formulario en el momento del envío. Sólo los campos que
// efectivamente cambiaron viajan en el PATCH; los demás se omiten para que el
// backend no sobrescriba nada. Estado, FechaRegistro, Rol, Código y Username
// (Keycloak) se muestran como solo lectura en la vista de detalle, NO en este
// formulario para minimizar riesgo de cambios accidentales.

interface Props {
  usuario: UsuarioDetalle
  alCancelar: () => void
  alGuardado: (respuesta: ModificarOperadorRespuesta, mensaje: string) => void
}

interface Campos {
  nombreUsuario: string
  correo: string
  nombre: string
  apellido: string
  sexo: string
  fechaNacimiento: string
  direccion: string
  telefono: string
  // Sección "Seguridad" — HU09. La nueva contraseña vive solo en estado del
  // formulario y se borra tras guardar correctamente. Nunca se persiste en
  // memoria del navegador más tiempo del necesario.
  nuevaContrasena: string
  confirmacionContrasena: string
}

const CODIGOS_TELEFONO = ['0414', '0412', '0424', '0416', '0426', '0212']
const CARACTERES_ESPECIALES = '!@#$%^&*_-.?'

type Errores = Partial<Record<keyof Campos | 'general', string>>

const MAPA_CAMPOS_BACKEND: Record<string, keyof Campos> = {
  nombreUsuario: 'nombreUsuario',
  correo: 'correo',
  nombre: 'nombre',
  apellido: 'apellido',
  sexo: 'sexo',
  fechaNacimiento: 'fechaNacimiento',
  telefono: 'telefono',
  'datosContacto.telefono': 'telefono',
  direccion: 'direccion',
  'datosContacto.direccion': 'direccion',
  // El validador del backend reporta errores de la regla común de contraseña
  // sobre el campo "contrasena"; en este formulario se renderizan junto al
  // input de "Nueva contraseña".
  contrasena: 'nuevaContrasena',
  nuevaContrasena: 'nuevaContrasena',
  confirmacionContrasena: 'confirmacionContrasena'
}

function formatearFechaInput(valor: string | null | undefined): string {
  if (!valor) return ''
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return ''
  const yyyy = fecha.getUTCFullYear()
  const mm = `${fecha.getUTCMonth() + 1}`.padStart(2, '0')
  const dd = `${fecha.getUTCDate()}`.padStart(2, '0')
  return `${yyyy}-${mm}-${dd}`
}

function estadoInicial(usuario: UsuarioDetalle): Campos {
  return {
    nombreUsuario: usuario.nombreUsuario ?? '',
    correo: usuario.correo ?? '',
    nombre: usuario.nombre ?? '',
    apellido: usuario.apellido ?? '',
    sexo: usuario.sexo ?? 'Indefinido',
    fechaNacimiento: formatearFechaInput(usuario.fechaNacimiento),
    direccion: usuario.datosContacto?.direccion ?? '',
    telefono: usuario.datosContacto?.telefono ?? '',
    // Sección Seguridad — los inputs arrancan vacíos. La contraseña actual
    // jamás se trae desde el backend ni se muestra.
    nuevaContrasena: '',
    confirmacionContrasena: ''
  }
}

function validarLocal(campos: Campos, original: Campos): Errores {
  const e: Errores = {}

  if (campos.nombreUsuario !== original.nombreUsuario) {
    const valor = campos.nombreUsuario.trim()
    if (!valor) e.nombreUsuario = 'El nombre de usuario es obligatorio.'
    else if (!/^[a-zA-Z0-9._]{4,30}$/.test(valor))
      e.nombreUsuario =
        'Debe tener entre 4 y 30 caracteres y solo letras, números, punto o guion bajo.'
  }

  if (campos.correo !== original.correo) {
    const valor = campos.correo.trim()
    if (!valor) e.correo = 'El correo es obligatorio.'
    else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(valor))
      e.correo = 'El correo no tiene un formato válido.'
  }

  if (campos.nombre !== original.nombre) {
    const valor = campos.nombre.trim()
    if (!valor) e.nombre = 'El nombre es obligatorio.'
    else if (!/^[A-Za-zÁÉÍÓÚáéíóúÑñÜü\s]{2,50}$/.test(valor))
      e.nombre = 'El nombre solo puede contener letras y espacios (2 a 50 caracteres).'
  }

  if (campos.apellido !== original.apellido) {
    const valor = campos.apellido.trim()
    if (!valor) e.apellido = 'El apellido es obligatorio.'
    else if (!/^[A-Za-zÁÉÍÓÚáéíóúÑñÜü\s]{2,50}$/.test(valor))
      e.apellido = 'El apellido solo puede contener letras y espacios (2 a 50 caracteres).'
  }

  if (campos.telefono !== original.telefono) {
    const tel = campos.telefono.replace(/[\s-]/g, '')
    if (!tel) e.telefono = 'El teléfono es obligatorio.'
    else if (!/^\d+$/.test(tel)) e.telefono = 'El teléfono debe contener solo números.'
    else if (tel.length !== 11) e.telefono = 'El teléfono debe tener 11 dígitos.'
    else if (!CODIGOS_TELEFONO.some((c) => tel.startsWith(c)))
      e.telefono = 'Debe comenzar con un código válido (0414, 0412, 0424, 0416, 0426 o 0212).'
  }

  if (campos.direccion !== original.direccion) {
    const valor = campos.direccion.trim()
    if (!valor) e.direccion = 'La dirección es obligatoria.'
    else if (valor.length < 5) e.direccion = 'La dirección debe tener al menos 5 caracteres.'
  }

  // HU09 — sección Seguridad. Se valida solo si el Administrador rellenó al
  // menos uno de los dos inputs. Reglas alineadas con
  // ReglasValidacionUsuario.ValidarContrasena en el backend (longitud 5-10,
  // al menos un dígito, al menos un carácter especial), y exigencia de que
  // ambos campos coincidan exactamente.
  const tocaContrasena = campos.nuevaContrasena !== '' || campos.confirmacionContrasena !== ''
  if (tocaContrasena) {
    const valor = campos.nuevaContrasena
    if (!valor) {
      e.nuevaContrasena = 'La contraseña es obligatoria.'
    } else if (valor.length < 5 || valor.length > 10) {
      e.nuevaContrasena = 'La contraseña debe tener entre 5 y 10 caracteres.'
    } else if (!/\d/.test(valor)) {
      e.nuevaContrasena = 'La contraseña debe contener al menos un número.'
    } else if (![...valor].some((c) => CARACTERES_ESPECIALES.includes(c))) {
      e.nuevaContrasena = 'La contraseña debe contener al menos un carácter especial.'
    }
    if (campos.confirmacionContrasena === '') {
      e.confirmacionContrasena = 'Debe confirmar la nueva contraseña.'
    } else if (campos.nuevaContrasena !== campos.confirmacionContrasena) {
      e.confirmacionContrasena = 'La nueva contraseña y la confirmación no coinciden.'
    }
  }

  if (campos.fechaNacimiento !== original.fechaNacimiento) {
    if (!campos.fechaNacimiento) e.fechaNacimiento = 'La fecha de nacimiento es obligatoria.'
    else {
      const nac = new Date(campos.fechaNacimiento + 'T00:00:00Z')
      if (Number.isNaN(nac.getTime())) e.fechaNacimiento = 'Fecha inválida.'
      else {
        const hoy = new Date()
        let edad = hoy.getFullYear() - nac.getFullYear()
        const mes = hoy.getMonth() - nac.getMonth()
        if (mes < 0 || (mes === 0 && hoy.getDate() < nac.getDate())) edad--
        if (edad < 0) e.fechaNacimiento = 'La fecha de nacimiento no puede ser futura.'
        else if (edad < 18) e.fechaNacimiento = 'El usuario debe tener al menos 18 años.'
        else if (edad > 100) e.fechaNacimiento = 'El usuario no puede tener más de 100 años.'
      }
    }
  }

  return e
}

// Construye el payload PATCH a partir del diff entre campos y original.
// Sólo se incluyen las propiedades realmente modificadas — la regla central
// de HU09: "no sobrescribir campos no enviados".
function construirPayload(campos: Campos, original: Campos): ModificarOperadorPayload {
  const payload: ModificarOperadorPayload = {}
  if (campos.nombreUsuario !== original.nombreUsuario) payload.nombreUsuario = campos.nombreUsuario.trim()
  if (campos.correo !== original.correo) payload.correo = campos.correo.trim()
  if (campos.nombre !== original.nombre) payload.nombre = campos.nombre.trim()
  if (campos.apellido !== original.apellido) payload.apellido = campos.apellido.trim()
  if (campos.sexo !== original.sexo) payload.sexo = campos.sexo
  if (campos.fechaNacimiento !== original.fechaNacimiento)
    payload.fechaNacimiento = new Date(campos.fechaNacimiento + 'T00:00:00Z').toISOString()

  const contacto: Record<string, string> = {}
  if (campos.direccion !== original.direccion) contacto.direccion = campos.direccion.trim()
  if (campos.telefono !== original.telefono) contacto.telefono = campos.telefono.replace(/[\s-]/g, '')
  if (Object.keys(contacto).length > 0) payload.datosContacto = contacto

  // HU09 — sección Seguridad. Si el Administrador escribió contraseña Y
  // confirmación, las dos viajan en el payload. Si no, no se envían: el
  // backend interpreta ausencia como "no cambiar contraseña".
  if (campos.nuevaContrasena !== '' && campos.confirmacionContrasena !== '') {
    payload.nuevaContrasena = campos.nuevaContrasena
    payload.confirmacionContrasena = campos.confirmacionContrasena
  }

  return payload
}

function payloadVacio(payload: ModificarOperadorPayload): boolean {
  const claves = Object.keys(payload) as Array<keyof ModificarOperadorPayload>
  return claves.every((k) => {
    if (k === 'datosContacto') return !payload.datosContacto || Object.keys(payload.datosContacto).length === 0
    return payload[k] === undefined
  })
}

export function FormularioEditarOperador({ usuario, alCancelar, alGuardado }: Props) {
  const { token } = usarAutenticacion()
  const original = useMemo(() => estadoInicial(usuario), [usuario])
  const [campos, setCampos] = useState<Campos>(original)
  const [errores, setErrores] = useState<Errores>({})
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const [aviso, setAviso] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  const actualizar = <K extends keyof Campos>(clave: K, valor: Campos[K]) => {
    setCampos((c) => ({ ...c, [clave]: valor }))
    setErrores((prev) => ({ ...prev, [clave]: undefined }))
  }

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault()
    setErrorGeneral(null)
    setAviso(null)

    if (!token) {
      setErrorGeneral('Debe iniciar sesión como administrador.')
      return
    }

    const erroresLocales = validarLocal(campos, original)
    if (Object.values(erroresLocales).some((v) => v)) {
      setErrores(erroresLocales)
      setErrorGeneral('Revise los campos marcados.')
      return
    }

    const payload = construirPayload(campos, original)
    if (payloadVacio(payload)) {
      setAviso('No hay cambios para guardar.')
      return
    }

    setEnviando(true)
    try {
      const respuesta = await modificarOperador(usuario.id, payload, token)
      // Limpiar la contraseña en memoria del navegador inmediatamente tras
      // recibir respuesta exitosa. La contraseña no debe permanecer en
      // estado React más tiempo del estrictamente necesario.
      setCampos((c) => ({ ...c, nuevaContrasena: '', confirmacionContrasena: '' }))
      const mensaje = respuesta.huboCambios
        ? 'Operador actualizado correctamente.'
        : 'No había cambios para aplicar.'
      alGuardado(respuesta, mensaje)
    } catch (e) {
      // En error también limpiamos la contraseña: el Administrador deberá
      // volver a escribirla si quiere reintentar, evitando que el valor
      // sobreviva en el formulario.
      setCampos((c) => ({ ...c, nuevaContrasena: '', confirmacionContrasena: '' }))
      if (e instanceof ErrorValidacionRegistro && e.errores.length > 0) {
        const nuevos: Errores = {}
        for (const err of e.errores) {
          const clave = MAPA_CAMPOS_BACKEND[err.campo]
          if (clave) nuevos[clave] = err.mensaje
        }
        setErrores(nuevos)
        setErrorGeneral('No fue posible guardar los cambios. Revise los campos marcados.')
      } else if (e instanceof Error && e.message) {
        setErrorGeneral(e.message)
      } else {
        setErrorGeneral('No fue posible guardar los cambios.')
      }
    } finally {
      setEnviando(false)
    }
  }

  return (
    <section className="seccion">
      {errorGeneral && <Alerta tono="error">{errorGeneral}</Alerta>}
      {aviso && <Alerta tono="informacion">{aviso}</Alerta>}

      <form onSubmit={enviar} className="formulario-usuario" noValidate>
        <CampoFormulario etiqueta="Nombre de usuario" htmlFor="nombreUsuario" error={errores.nombreUsuario}>
          <input
            id="nombreUsuario"
            value={campos.nombreUsuario}
            onChange={(e) => actualizar('nombreUsuario', e.target.value)}
            required
          />
        </CampoFormulario>

        <CampoFormulario etiqueta="Correo" htmlFor="correo" error={errores.correo}>
          <input
            id="correo"
            type="email"
            value={campos.correo}
            onChange={(e) => actualizar('correo', e.target.value)}
            required
          />
        </CampoFormulario>

        <CampoFormulario etiqueta="Nombre" htmlFor="nombre" error={errores.nombre}>
          <input
            id="nombre"
            value={campos.nombre}
            onChange={(e) => actualizar('nombre', e.target.value)}
            required
          />
        </CampoFormulario>

        <CampoFormulario etiqueta="Apellido" htmlFor="apellido" error={errores.apellido}>
          <input
            id="apellido"
            value={campos.apellido}
            onChange={(e) => actualizar('apellido', e.target.value)}
            required
          />
        </CampoFormulario>

        <CampoFormulario etiqueta="Sexo" htmlFor="sexo">
          <select
            id="sexo"
            value={campos.sexo}
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
            value={campos.fechaNacimiento}
            onChange={(e) => actualizar('fechaNacimiento', e.target.value)}
            required
          />
        </CampoFormulario>

        <CampoFormulario etiqueta="Dirección" htmlFor="direccion" error={errores.direccion}>
          <input
            id="direccion"
            value={campos.direccion}
            onChange={(e) => actualizar('direccion', e.target.value)}
            required
          />
        </CampoFormulario>

        <CampoFormulario etiqueta="Teléfono" htmlFor="telefono" error={errores.telefono}>
          <input
            id="telefono"
            value={campos.telefono}
            placeholder="04141356230"
            onChange={(e) => actualizar('telefono', e.target.value)}
            required
          />
        </CampoFormulario>

        {/* HU09 — Sección Seguridad. Ambos campos son opcionales: si el
            Administrador no los rellena, la contraseña del Operador no se
            modifica. La contraseña actual NUNCA se muestra y NUNCA viaja
            desde el backend. */}
        <fieldset className="seccion-formulario seccion-seguridad">
          <legend>Seguridad</legend>
          <p style={{ margin: '0 0 12px', color: 'var(--color-texto-tenue)' }}>
            Deje ambos campos vacíos si no desea cambiar la contraseña.
          </p>

          <CampoFormulario
            etiqueta="Nueva contraseña"
            htmlFor="nuevaContrasena"
            opcional="opcional"
            error={errores.nuevaContrasena}
          >
            <input
              id="nuevaContrasena"
              type="password"
              value={campos.nuevaContrasena}
              maxLength={10}
              autoComplete="new-password"
              onChange={(e) => actualizar('nuevaContrasena', e.target.value)}
            />
          </CampoFormulario>

          <CampoFormulario
            etiqueta="Confirmar contraseña"
            htmlFor="confirmacionContrasena"
            opcional="opcional"
            error={errores.confirmacionContrasena}
          >
            <input
              id="confirmacionContrasena"
              type="password"
              value={campos.confirmacionContrasena}
              maxLength={10}
              autoComplete="new-password"
              onChange={(e) => actualizar('confirmacionContrasena', e.target.value)}
            />
          </CampoFormulario>
        </fieldset>

        <div className="acciones-formulario">
          <Boton variante="secundario" type="button" onClick={alCancelar} disabled={enviando}>
            Cancelar
          </Boton>
          <Boton variante="primario" type="submit" disabled={enviando}>
            {enviando ? 'Guardando…' : 'Confirmar cambios'}
          </Boton>
        </div>
      </form>
    </section>
  )
}
