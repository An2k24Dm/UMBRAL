import { useEffect, type ReactNode } from 'react'
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native'
import { useRouter } from 'expo-router'
import { useAutenticacion } from './ContextoAutenticacion'
import { tema } from '../estilos/tema'

export default function RutaProtegidaMovil({ children }: { children: ReactNode }) {
  const { cargandoSesion, estaAutenticado, usuario, cerrarSesion } = useAutenticacion()
  const enrutador = useRouter()

  useEffect(() => {
    if (cargandoSesion) return
    if (!estaAutenticado) {
      enrutador.replace('/')
      return
    }
    if (usuario && usuario.rol !== 'Participante') {
      cerrarSesion().finally(() => enrutador.replace('/'))
    }
  }, [cargandoSesion, estaAutenticado, usuario, cerrarSesion, enrutador])

  if (cargandoSesion || !estaAutenticado || usuario?.rol !== 'Participante') {
    return (
      <View style={estilos.contenedorCarga}>
        <ActivityIndicator color={tema.colores.primario} size="large" />
        <Text style={estilos.textoCarga}>Cargando sesión…</Text>
      </View>
    )
  }

  return <>{children}</>
}

const estilos = StyleSheet.create({
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
  }
})
