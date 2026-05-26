import { useEffect, useState } from 'react'
import {
  ActivityIndicator,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View
} from 'react-native'
import { useRouter } from 'expo-router'
import {
  ErrorValidacionRegistro,
  modificarPerfilParticipanteApi,
  obtenerPerfilActualApi,
  type ModificarPerfilParticipantePayload,
  type PerfilParticipante
} from '../../autenticacion/clienteApi'
import { obtenerToken } from '../../autenticacion/almacenamientoSeguro'
import { useAutenticacion } from '../../autenticacion/ContextoAutenticacion'
import RutaProtegidaMovil from '../../componentes/RutaProtegidaMovil'
import { tema } from '../../estilos/tema'

// HU10 — pantalla móvil para que el Participante edite su propio perfil.
// El backend identifica al usuario por el sub del token: NO se envía id.
// La contraseña, si se completa, viaja únicamente a Keycloak — no se guarda
// en PostgreSQL, no se devuelve en la respuesta y se borra del estado de
// React Native tras la respuesta del servidor.
export default function PantallaEditarPerfilParticipante() {
  return (
    <RutaProtegidaMovil>
      <ContenidoEditarPerfil />
    </RutaProtegidaMovil>
  )
}

interface CamposFormulario {
  alias: string
  nombreUsuario: string
  correo: string
  nombre: string
  apellido: string
  sexo: string
  fechaNacimiento: string
  direccion: string
  telefono: string
  nuevaContrasena: string
  confirmacionContrasena: string
}

function estadoInicial(perfil: PerfilParticipante): CamposFormulario {
  return {
    alias: perfil.alias ?? '',
    nombreUsuario: perfil.nombreUsuario ?? '',
    correo: perfil.correo ?? '',
    nombre: perfil.nombre ?? '',
    apellido: perfil.apellido ?? '',
    sexo: perfil.sexo ?? 'Indefinido',
    fechaNacimiento: formatearFechaInput(perfil.fechaNacimiento),
    direccion: perfil.datosContacto?.direccion ?? '',
    telefono: perfil.datosContacto?.telefono ?? '',
    // La sección "Seguridad" arranca siempre vacía: la contraseña actual
    // nunca se trae del backend y no se conserva en memoria.
    nuevaContrasena: '',
    confirmacionContrasena: ''
  }
}

function formatearFechaInput(valor?: string | null): string {
  if (!valor) return ''
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return ''
  const y = fecha.getUTCFullYear()
  const m = String(fecha.getUTCMonth() + 1).padStart(2, '0')
  const d = String(fecha.getUTCDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function ContenidoEditarPerfil() {
  const enrutador = useRouter()
  const { cerrarSesion } = useAutenticacion()
  const [perfilOriginal, setPerfilOriginal] = useState<PerfilParticipante | null>(null)
  const [campos, setCampos] = useState<CamposFormulario | null>(null)
  const [cargando, setCargando] = useState(true)
  const [enviando, setEnviando] = useState(false)
  const [erroresCampo, setErroresCampo] = useState<Record<string, string>>({})
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const [aviso, setAviso] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)

  useEffect(() => {
    let cancelado = false
    const cargar = async () => {
      try {
        const token = await obtenerToken()
        if (!token) {
          enrutador.replace('/')
          return
        }
        const perfil = await obtenerPerfilActualApi(token)
        if (cancelado) return
        if (perfil.rol !== 'Participante') {
          // La app es exclusiva del Participante. Defensa de doble check.
          await cerrarSesion()
          enrutador.replace('/')
          return
        }
        setPerfilOriginal(perfil)
        setCampos(estadoInicial(perfil))
      } catch (e) {
        if (cancelado) return
        setErrorGeneral(
          e instanceof Error ? e.message : 'No fue posible cargar tu perfil.'
        )
      } finally {
        if (!cancelado) setCargando(false)
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [enrutador, cerrarSesion])

  const actualizar = <K extends keyof CamposFormulario>(clave: K, valor: string) => {
    setCampos((c) => (c ? { ...c, [clave]: valor } : c))
    setErroresCampo((prev) => ({ ...prev, [clave]: '' }))
  }

  const guardar = async () => {
    if (!campos || !perfilOriginal) return
    setErrorGeneral(null)
    setAviso(null)
    setMensajeExito(null)

    // Validación local mínima — la fuente de verdad es el backend, pero
    // anticipamos los errores obvios (contraseñas y alias).
    const erroresLocales: Record<string, string> = {}

    // Alias: si cambió, debe cumplir 6-15 caracteres y [a-zA-Z0-9_].
    if (campos.alias !== perfilOriginal.alias) {
      const aliasTrim = campos.alias.trim()
      if (!aliasTrim) {
        erroresLocales.alias = 'El alias es obligatorio.'
      } else if (aliasTrim.length < 6 || aliasTrim.length > 15) {
        erroresLocales.alias = 'El alias debe tener entre 6 y 15 caracteres.'
      } else if (!/^[a-zA-Z0-9_]{6,15}$/.test(aliasTrim)) {
        erroresLocales.alias = 'El alias solo puede contener letras, números y guion bajo.'
      }
    }

    const tocaContrasena =
      campos.nuevaContrasena !== '' || campos.confirmacionContrasena !== ''
    if (tocaContrasena) {
      if (campos.nuevaContrasena === '')
        erroresLocales.nuevaContrasena = 'La contraseña es obligatoria.'
      if (campos.confirmacionContrasena === '')
        erroresLocales.confirmacionContrasena = 'Confirme la contraseña.'
      if (
        campos.nuevaContrasena !== '' &&
        campos.confirmacionContrasena !== '' &&
        campos.nuevaContrasena !== campos.confirmacionContrasena
      ) {
        erroresLocales.confirmacionContrasena =
          'La nueva contraseña y la confirmación no coinciden.'
      }
    }
    if (Object.values(erroresLocales).some((m) => m)) {
      setErroresCampo(erroresLocales)
      setErrorGeneral('Revise los campos marcados.')
      return
    }

    const payload = construirPayload(campos, perfilOriginal)
    if (payloadVacio(payload)) {
      setAviso('No hay cambios para guardar.')
      return
    }

    const token = await obtenerToken()
    if (!token) {
      enrutador.replace('/')
      return
    }

    setEnviando(true)
    try {
      const respuesta = await modificarPerfilParticipanteApi(token, payload)
      // Limpiar contraseña inmediatamente — no debe sobrevivir más tiempo
      // del estrictamente necesario en memoria del cliente.
      setCampos((c) => (c ? { ...c, nuevaContrasena: '', confirmacionContrasena: '' } : c))
      setPerfilOriginal(respuesta.participante)
      setCampos(estadoInicial(respuesta.participante))
      setMensajeExito(
        respuesta.huboCambios
          ? 'Perfil actualizado correctamente.'
          : 'No había cambios para aplicar.'
      )
    } catch (e) {
      // Limpieza en error también: la contraseña no debe persistir.
      setCampos((c) => (c ? { ...c, nuevaContrasena: '', confirmacionContrasena: '' } : c))
      if (e instanceof ErrorValidacionRegistro && e.errores.length > 0) {
        const nuevos: Record<string, string> = {}
        for (const err of e.errores) {
          const clave = mapearCampoBackend(err.campo)
          if (clave) nuevos[clave] = err.mensaje
        }
        setErroresCampo(nuevos)
        setErrorGeneral('Revise los campos marcados.')
      } else {
        setErrorGeneral(
          e instanceof Error ? e.message : 'No fue posible guardar los cambios.'
        )
      }
    } finally {
      setEnviando(false)
    }
  }

  if (cargando || !campos) {
    return (
      <View style={estilos.contenedorEstado}>
        <ActivityIndicator size="large" color={tema.colores.primario} />
        <Text style={estilos.textoEstado}>Cargando tu perfil…</Text>
      </View>
    )
  }

  return (
    <ScrollView style={estilos.contenedor} contentContainerStyle={estilos.contenido}>
      <Text style={estilos.titulo}>Editar perfil</Text>

      {errorGeneral && (
        <View style={estilos.cuadroError}>
          <Text style={estilos.cuadroErrorTexto}>{errorGeneral}</Text>
        </View>
      )}
      {aviso && (
        <View style={estilos.cuadroAviso}>
          <Text style={estilos.cuadroAvisoTexto}>{aviso}</Text>
        </View>
      )}
      {mensajeExito && (
        <View style={estilos.cuadroExito}>
          <Text style={estilos.cuadroExitoTexto}>{mensajeExito}</Text>
        </View>
      )}

      <Campo etiqueta="Alias (6-15 letras, números y guion bajo)" valor={campos.alias}
        error={erroresCampo.alias}
        onChange={(v) => actualizar('alias', v)} autoCapitalize="none" />
      <Campo etiqueta="Nombre de usuario" valor={campos.nombreUsuario} error={erroresCampo.nombreUsuario}
        onChange={(v) => actualizar('nombreUsuario', v)} autoCapitalize="none" />
      <Campo etiqueta="Correo" valor={campos.correo} error={erroresCampo.correo}
        onChange={(v) => actualizar('correo', v)} autoCapitalize="none" keyboardType="email-address" />
      <Campo etiqueta="Nombre" valor={campos.nombre} error={erroresCampo.nombre}
        onChange={(v) => actualizar('nombre', v)} />
      <Campo etiqueta="Apellido" valor={campos.apellido} error={erroresCampo.apellido}
        onChange={(v) => actualizar('apellido', v)} />
      <Campo etiqueta="Sexo (Masculino / Femenino / Otro / Indefinido)" valor={campos.sexo}
        error={erroresCampo.sexo} onChange={(v) => actualizar('sexo', v)} />
      <Campo etiqueta="Fecha de nacimiento (YYYY-MM-DD)" valor={campos.fechaNacimiento}
        error={erroresCampo.fechaNacimiento} onChange={(v) => actualizar('fechaNacimiento', v)} />
      <Campo etiqueta="Dirección" valor={campos.direccion} error={erroresCampo.direccion}
        onChange={(v) => actualizar('direccion', v)} />
      <Campo etiqueta="Teléfono" valor={campos.telefono} error={erroresCampo.telefono}
        onChange={(v) => actualizar('telefono', v)} keyboardType="phone-pad" />

      <Text style={estilos.tituloSeccion}>Seguridad</Text>
      <Text style={estilos.descripcionSeccion}>
        Deje ambos campos vacíos si no desea cambiar la contraseña.
      </Text>
      <Campo etiqueta="Nueva contraseña" valor={campos.nuevaContrasena}
        error={erroresCampo.nuevaContrasena}
        onChange={(v) => actualizar('nuevaContrasena', v)}
        secureTextEntry
        autoComplete="password-new" />
      <Campo etiqueta="Confirmar contraseña" valor={campos.confirmacionContrasena}
        error={erroresCampo.confirmacionContrasena}
        onChange={(v) => actualizar('confirmacionContrasena', v)}
        secureTextEntry
        autoComplete="password-new" />

      <TouchableOpacity
        style={[estilos.botonPrimario, enviando && estilos.botonDeshabilitado]}
        onPress={guardar}
        disabled={enviando}
      >
        <Text style={estilos.botonPrimarioTexto}>
          {enviando ? 'Guardando…' : 'Guardar cambios'}
        </Text>
      </TouchableOpacity>

      <TouchableOpacity
        style={estilos.botonSecundario}
        onPress={() => enrutador.replace('/participante/perfil')}
        disabled={enviando}
      >
        <Text style={estilos.botonSecundarioTexto}>Cancelar</Text>
      </TouchableOpacity>
    </ScrollView>
  )
}

function construirPayload(
  campos: CamposFormulario,
  original: PerfilParticipante
): ModificarPerfilParticipantePayload {
  const payload: ModificarPerfilParticipantePayload = {}
  const o = estadoInicial(original)

  if (campos.alias !== o.alias) payload.alias = campos.alias.trim()
  if (campos.nombreUsuario !== o.nombreUsuario) payload.nombreUsuario = campos.nombreUsuario.trim()
  if (campos.correo !== o.correo) payload.correo = campos.correo.trim()
  if (campos.nombre !== o.nombre) payload.nombre = campos.nombre.trim()
  if (campos.apellido !== o.apellido) payload.apellido = campos.apellido.trim()
  if (campos.sexo !== o.sexo) payload.sexo = campos.sexo
  if (campos.fechaNacimiento !== o.fechaNacimiento)
    payload.fechaNacimiento = new Date(campos.fechaNacimiento + 'T00:00:00Z').toISOString()

  const contacto: Record<string, string> = {}
  if (campos.direccion !== o.direccion) contacto.direccion = campos.direccion.trim()
  if (campos.telefono !== o.telefono) contacto.telefono = campos.telefono.replace(/[\s-]/g, '')
  if (Object.keys(contacto).length > 0) payload.datosContacto = contacto

  if (campos.nuevaContrasena !== '' && campos.confirmacionContrasena !== '') {
    payload.nuevaContrasena = campos.nuevaContrasena
    payload.confirmacionContrasena = campos.confirmacionContrasena
  }

  return payload
}

function payloadVacio(payload: ModificarPerfilParticipantePayload): boolean {
  const claves = Object.keys(payload) as Array<keyof ModificarPerfilParticipantePayload>
  return claves.every((k) => {
    if (k === 'datosContacto')
      return !payload.datosContacto || Object.keys(payload.datosContacto).length === 0
    return payload[k] === undefined
  })
}

const MAPA_CAMPOS_BACKEND: Record<string, keyof CamposFormulario> = {
  alias: 'alias',
  nombreUsuario: 'nombreUsuario',
  correo: 'correo',
  nombre: 'nombre',
  apellido: 'apellido',
  sexo: 'sexo',
  fechaNacimiento: 'fechaNacimiento',
  'datosContacto.direccion': 'direccion',
  'datosContacto.telefono': 'telefono',
  contrasena: 'nuevaContrasena',
  nuevaContrasena: 'nuevaContrasena',
  confirmacionContrasena: 'confirmacionContrasena'
}

function mapearCampoBackend(campo: string): keyof CamposFormulario | null {
  return MAPA_CAMPOS_BACKEND[campo] ?? null
}

interface PropsCampo {
  etiqueta: string
  valor: string
  error?: string
  onChange: (v: string) => void
  secureTextEntry?: boolean
  autoCapitalize?: 'none' | 'sentences' | 'words' | 'characters'
  keyboardType?: 'default' | 'email-address' | 'phone-pad'
  autoComplete?: 'password' | 'password-new' | 'off'
}

function Campo({
  etiqueta, valor, error, onChange,
  secureTextEntry, autoCapitalize, keyboardType, autoComplete
}: PropsCampo) {
  return (
    <View style={estilos.campo}>
      <Text style={estilos.etiqueta}>{etiqueta}</Text>
      <TextInput
        style={[estilos.input, error ? estilos.inputError : null]}
        value={valor}
        onChangeText={onChange}
        secureTextEntry={secureTextEntry}
        autoCapitalize={autoCapitalize}
        keyboardType={keyboardType}
        autoComplete={autoComplete}
      />
      {error ? <Text style={estilos.errorCampo}>{error}</Text> : null}
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: { flex: 1, backgroundColor: tema.colores.fondo },
  contenido: {
    padding: tema.espacios.lg,
    paddingTop: tema.espacios.xl * 2,
    paddingBottom: tema.espacios.xl * 2
  },
  contenedorEstado: {
    flex: 1, backgroundColor: tema.colores.fondo,
    alignItems: 'center', justifyContent: 'center', padding: tema.espacios.lg
  },
  textoEstado: {
    color: tema.colores.textoTenue, marginTop: tema.espacios.md, fontSize: 14
  },
  titulo: {
    fontSize: 22, fontWeight: '800', color: tema.colores.texto,
    marginBottom: tema.espacios.lg, textAlign: 'center'
  },
  campo: { marginBottom: tema.espacios.md },
  etiqueta: {
    color: tema.colores.textoTenue, fontSize: 11,
    textTransform: 'uppercase', letterSpacing: 1, marginBottom: 4
  },
  input: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderColor: tema.colores.bordeTarjeta,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.sm,
    color: tema.colores.texto,
    fontSize: 14
  },
  inputError: { borderColor: tema.colores.error },
  errorCampo: { color: tema.colores.error, fontSize: 12, marginTop: 4 },
  tituloSeccion: {
    color: tema.colores.textoTenue, fontSize: 11,
    textTransform: 'uppercase', letterSpacing: 2, marginTop: tema.espacios.lg
  },
  descripcionSeccion: {
    color: tema.colores.textoTenue, fontSize: 12,
    marginTop: 4, marginBottom: tema.espacios.sm
  },
  cuadroError: {
    backgroundColor: 'rgba(255,107,107,0.12)', borderColor: tema.colores.error,
    borderWidth: 1, borderRadius: tema.radios.entrada,
    padding: tema.espacios.md, marginBottom: tema.espacios.md
  },
  cuadroErrorTexto: {
    color: tema.colores.error, fontSize: 13, textAlign: 'center'
  },
  cuadroAviso: {
    backgroundColor: 'rgba(124,92,255,0.10)', borderColor: tema.colores.primario,
    borderWidth: 1, borderRadius: tema.radios.entrada,
    padding: tema.espacios.md, marginBottom: tema.espacios.md
  },
  cuadroAvisoTexto: {
    color: tema.colores.enlace, fontSize: 13, textAlign: 'center'
  },
  cuadroExito: {
    backgroundColor: 'rgba(76,217,100,0.12)', borderColor: '#4cd964',
    borderWidth: 1, borderRadius: tema.radios.entrada,
    padding: tema.espacios.md, marginBottom: tema.espacios.md
  },
  cuadroExitoTexto: { color: '#2ea44f', fontSize: 13, textAlign: 'center' },
  botonPrimario: {
    backgroundColor: tema.colores.primario, paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton, alignItems: 'center', marginTop: tema.espacios.md
  },
  botonPrimarioTexto: { color: '#fff', fontWeight: '700', fontSize: 15 },
  botonDeshabilitado: { opacity: 0.6 },
  botonSecundario: {
    paddingVertical: tema.espacios.md, borderRadius: tema.radios.boton,
    alignItems: 'center', marginTop: tema.espacios.sm,
    borderWidth: 1, borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondoTarjeta
  },
  botonSecundarioTexto: { color: tema.colores.texto, fontWeight: '700', fontSize: 14 }
})
