import { useEffect } from 'react'
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native'
import { useRouter } from 'expo-router'
import { useAutenticacion } from '../autenticacion/ContextoAutenticacion'
import { useInicioSesion } from '../hooks/useInicioSesion'
import { BotonMovil } from '../componentes/BotonMovil'
import { CampoTextoMovil } from '../componentes/CampoTextoMovil'
import { MensajeEstado } from '../componentes/MensajeEstado'
import { PantallaBase } from '../componentes/PantallaBase'
import { TarjetaMovil } from '../componentes/TarjetaMovil'
import { tema } from '../estilos/tema'

export default function PantallaInicioSesion() {
  const enrutador = useRouter()
  const { cargandoSesion, estaAutenticado } = useAutenticacion()
  const {
    nombreUsuario,
    contrasena,
    errorNombre,
    errorContrasena,
    mensajeError,
    enviando,
    alCambiarNombre,
    alCambiarContrasena,
    enviar,
  } = useInicioSesion()

  useEffect(() => {
    if (!cargandoSesion && estaAutenticado) {
      enrutador.replace('/participante/menu')
    }
  }, [cargandoSesion, estaAutenticado, enrutador])

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
            onCambio={alCambiarNombre}
            error={errorNombre}
            placeholder="tu.usuario"
            autoCapitalize="none"
            editable={!enviando}
          />

          <CampoTextoMovil
            etiqueta="Contraseña"
            valor={contrasena}
            onCambio={alCambiarContrasena}
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
