import { useEffect, useState } from 'react'
import {
  ActivityIndicator,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View
} from 'react-native'
import { useRouter } from 'expo-router'
import { ErrorInicioSesion } from '../autenticacion/clienteApi'
import { useAutenticacion } from '../autenticacion/ContextoAutenticacion'
import { tema } from '../estilos/tema'

// HU04 — pantalla de inicio de sesión móvil del Participante.
// La app móvil es exclusiva del Participante: si un Administrador/Operador
// intenta entrar, el backend responde 403 desde /api/autenticacion/login-movil
// (código ACCESO_NO_PERMITIDO) y aquí se muestra el mensaje correspondiente.
export default function PantallaInicioSesion() {
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [errorNombre, setErrorNombre] = useState<string | null>(null)
  const [errorContrasena, setErrorContrasena] = useState<string | null>(null)
  const enrutador = useRouter()
  const { iniciarSesion, cargandoSesion, estaAutenticado } = useAutenticacion()

  // Si al abrir la app ya había sesión guardada, redirigir al área del
  // Participante en lugar de quedarse en login.
  useEffect(() => {
    if (!cargandoSesion && estaAutenticado) {
      enrutador.replace('/participante/menu')
    }
  }, [cargandoSesion, estaAutenticado, enrutador])

  const traducirError = (e: unknown): string => {
    if (e instanceof ErrorInicioSesion) {
      switch (e.codigo) {
        case 'DATOS_INVALIDOS':
          return 'Usuario o contraseña incorrectos.'
        case 'ACCESO_NO_PERMITIDO':
        case 'ROL_NO_VALIDO':
          return 'Este usuario no puede iniciar sesión desde la app móvil.'
        case 'CUENTA_DESACTIVADA':
          return 'Tu cuenta está desactivada. Contacta a un administrador.'
        default:
          return e.message || 'No fue posible iniciar sesión.'
      }
    }
    if (e instanceof Error) return e.message
    return 'No fue posible iniciar sesión.'
  }

  const enviar = async () => {
    const usuario = nombreUsuario.trim()
    const errorN = !usuario ? 'El nombre de usuario es obligatorio.' : null
    const errorC = !contrasena ? 'La contraseña es obligatoria.' : null
    setErrorNombre(errorN)
    setErrorContrasena(errorC)
    setMensajeError(null)
    if (errorN || errorC) return

    setEnviando(true)
    try {
      await iniciarSesion(usuario, contrasena)
      enrutador.replace('/participante/menu')
    } catch (e) {
      setMensajeError(traducirError(e))
    } finally {
      setEnviando(false)
    }
  }

  // Mientras se consulta SecureStore mostramos un indicador para evitar
  // parpadeos entre login y pantalla del Participante.
  if (cargandoSesion) {
    return (
      <View style={estilos.contenedorCarga}>
        <ActivityIndicator color={tema.colores.primario} size="large" />
        <Text style={estilos.textoCarga}>Cargando sesión…</Text>
      </View>
    )
  }

  return (
    <View style={estilos.contenedor}>
      <View style={estilos.tarjeta}>
        <Text style={estilos.titulo}>UMBRAL</Text>
        <Text style={estilos.subtitulo}>Accede como Participante</Text>
        <Text style={estilos.descripcion}>
          Entra a tus sesiones, trivias y búsquedas del tesoro.
        </Text>

        <TextInput
          style={[estilos.entrada, errorNombre ? estilos.entradaConError : null]}
          placeholder="Nombre de usuario"
          placeholderTextColor={tema.colores.textoTenue}
          autoCapitalize="none"
          value={nombreUsuario}
          onChangeText={(v) => {
            setNombreUsuario(v)
            if (errorNombre) setErrorNombre(null)
            if (mensajeError) setMensajeError(null)
          }}
          editable={!enviando}
        />
        {errorNombre && <Text style={estilos.errorTexto}>{errorNombre}</Text>}

        <TextInput
          style={[estilos.entrada, errorContrasena ? estilos.entradaConError : null]}
          placeholder="Contraseña"
          placeholderTextColor={tema.colores.textoTenue}
          secureTextEntry
          value={contrasena}
          onChangeText={(v) => {
            setContrasena(v)
            if (errorContrasena) setErrorContrasena(null)
            if (mensajeError) setMensajeError(null)
          }}
          editable={!enviando}
        />
        {errorContrasena && (
          <Text style={estilos.errorTexto}>{errorContrasena}</Text>
        )}

        {mensajeError && (
          <View style={estilos.cuadroError}>
            <Text style={estilos.cuadroErrorTexto}>{mensajeError}</Text>
          </View>
        )}

        <TouchableOpacity
          style={[estilos.boton, enviando && estilos.botonDeshabilitado]}
          onPress={enviar}
          disabled={enviando}
        >
          <Text style={estilos.textoBoton}>
            {enviando ? 'Ingresando…' : 'Iniciar sesión'}
          </Text>
        </TouchableOpacity>

        <View style={estilos.pieEnlace}>
          <Text style={estilos.textoTenue}>¿No tienes cuenta? </Text>
          <TouchableOpacity
            onPress={() => enrutador.push('/registro')}
            disabled={enviando}
          >
            <Text style={estilos.enlace}>Regístrate</Text>
          </TouchableOpacity>
        </View>
      </View>
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: {
    flex: 1,
    padding: tema.espacios.xl,
    justifyContent: 'center',
    backgroundColor: tema.colores.fondo
  },
  contenedorCarga: {
    flex: 1,
    backgroundColor: tema.colores.fondo,
    alignItems: 'center',
    justifyContent: 'center'
  },
  textoCarga: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
    fontSize: 14
  },
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.xl
  },
  titulo: {
    fontSize: 32,
    fontWeight: '800',
    textAlign: 'center',
    color: tema.colores.texto,
    letterSpacing: 2
  },
  subtitulo: {
    fontSize: 16,
    fontWeight: '600',
    textAlign: 'center',
    color: tema.colores.texto,
    marginTop: tema.espacios.sm
  },
  descripcion: {
    fontSize: 13,
    textAlign: 'center',
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
    marginBottom: tema.espacios.xl
  },
  entrada: {
    backgroundColor: tema.colores.entradaFondo,
    borderWidth: 1,
    borderColor: tema.colores.entradaBorde,
    borderRadius: tema.radios.entrada,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.md,
    color: tema.colores.texto,
    marginBottom: tema.espacios.md
  },
  entradaConError: { borderColor: tema.colores.error },
  errorTexto: {
    color: tema.colores.error,
    fontSize: 12,
    marginTop: -tema.espacios.sm,
    marginBottom: tema.espacios.sm
  },
  cuadroError: {
    backgroundColor: 'rgba(255,107,107,0.12)',
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.sm
  },
  cuadroErrorTexto: {
    color: tema.colores.error,
    fontSize: 13,
    textAlign: 'center'
  },
  boton: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: 'center',
    marginTop: tema.espacios.sm
  },
  botonDeshabilitado: { backgroundColor: tema.colores.primarioDeshabilitado },
  textoBoton: { color: '#fff', fontWeight: '700', fontSize: 15 },
  pieEnlace: {
    flexDirection: 'row',
    justifyContent: 'center',
    marginTop: tema.espacios.lg
  },
  textoTenue: { color: tema.colores.textoTenue, fontSize: 13 },
  enlace: { color: tema.colores.enlace, fontWeight: '700', fontSize: 13 }
})
