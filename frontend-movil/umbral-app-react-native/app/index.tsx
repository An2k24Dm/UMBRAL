import { useEffect, useState } from 'react'
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native'
import { useRouter } from 'expo-router'
import { ErrorInicioSesion } from '../autenticacion/clienteApi'
import { useAutenticacion } from '../autenticacion/ContextoAutenticacion'
import { BotonMovil } from '../componentes/BotonMovil'
import { CampoTextoMovil } from '../componentes/CampoTextoMovil'
import { MensajeEstado } from '../componentes/MensajeEstado'
import { PantallaBase } from '../componentes/PantallaBase'
import { TarjetaMovil } from '../componentes/TarjetaMovil'
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

  if (cargandoSesion) {
    return (
      <View style={estilos.contenedorCarga}>
        <ActivityIndicator color={tema.colores.primario} size="large" />
        <Text style={estilos.textoCarga}>Cargando sesión…</Text>
      </View>
    )
  }

  return (
    <PantallaBase scrollable={false} padding={false}>
      <View style={estilos.centrado}>
        <View style={estilos.marca}>
          <Text style={estilos.marcaTitulo}>UMBRAL</Text>
          <Text style={estilos.marcaSubtitulo}>Plataforma de juegos y misiones</Text>
        </View>

        <TarjetaMovil style={estilos.sinMargenBottom}>
          <Text style={estilos.tarjetaTitulo}>Iniciar sesión</Text>
          <Text style={estilos.tarjetaSubtitulo}>Accede como Participante</Text>

          {mensajeError && (
            <MensajeEstado tipo="error" mensaje={mensajeError} />
          )}

          <CampoTextoMovil
            etiqueta="Nombre de usuario"
            valor={nombreUsuario}
            onCambio={(v) => {
              setNombreUsuario(v)
              if (errorNombre) setErrorNombre(null)
              if (mensajeError) setMensajeError(null)
            }}
            error={errorNombre}
            placeholder="tu.usuario"
            autoCapitalize="none"
            editable={!enviando}
          />

          <CampoTextoMovil
            etiqueta="Contraseña"
            valor={contrasena}
            onCambio={(v) => {
              setContrasena(v)
              if (errorContrasena) setErrorContrasena(null)
              if (mensajeError) setMensajeError(null)
            }}
            error={errorContrasena}
            placeholder="••••••••"
            secureTextEntry
            editable={!enviando}
          />

          <BotonMovil
            titulo={enviando ? 'Ingresando…' : 'Iniciar sesión'}
            onPress={enviar}
            cargando={enviando}
          />

          <View style={estilos.pieEnlace}>
            <Text style={estilos.textoTenue}>¿No tienes cuenta? </Text>
            <TouchableOpacity
              onPress={() => enrutador.push('/registro')}
              disabled={enviando}
            >
              <Text style={estilos.enlace}>Regístrate</Text>
            </TouchableOpacity>
          </View>
        </TarjetaMovil>
      </View>
    </PantallaBase>
  )
}

const estilos = StyleSheet.create({
  contenedorCarga: {
    flex: 1,
    backgroundColor: tema.colores.fondo,
    alignItems: 'center',
    justifyContent: 'center',
  },
  textoCarga: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
    fontSize: tema.tipografia.tamanos.md,
  },
  centrado: {
    flex: 1,
    justifyContent: 'center',
    padding: tema.espacios.lg,
  },
  marca: {
    alignItems: 'center',
    marginBottom: tema.espacios.xl,
  },
  marcaTitulo: {
    fontSize: tema.tipografia.tamanos.h1,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
    letterSpacing: tema.tipografia.espaciadoLetra.md,
  },
  marcaSubtitulo: {
    fontSize: tema.tipografia.tamanos.sm,
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.xs,
    textTransform: 'uppercase',
  },
  tarjetaTitulo: {
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
    marginBottom: tema.espacios.xs,
  },
  tarjetaSubtitulo: {
    fontSize: tema.tipografia.tamanos.base,
    color: tema.colores.textoTenue,
    marginBottom: tema.espacios.lg,
  },
  sinMargenBottom: { marginBottom: 0 },
  pieEnlace: {
    flexDirection: 'row',
    justifyContent: 'center',
    marginTop: tema.espacios.lg,
  },
  textoTenue: { color: tema.colores.textoTenue, fontSize: tema.tipografia.tamanos.base },
  enlace: {
    color: tema.colores.enlace,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.base,
  },
})
