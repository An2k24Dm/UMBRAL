import { useEffect, useState } from 'react'
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native'
import { useRouter } from 'expo-router'
import { limpiarSesion, obtenerUsuario } from '../../autenticacion/almacenamientoSeguro'
import type { UsuarioAutenticado } from '../../autenticacion/clienteApi'

export default function PantallaSesionesParticipante() {
  const [usuario, setUsuario] = useState<UsuarioAutenticado | null>(null)
  const enrutador = useRouter()

  useEffect(() => {
    obtenerUsuario<UsuarioAutenticado>().then(setUsuario)
  }, [])

  const cerrarSesion = async () => {
    await limpiarSesion()
    enrutador.replace('/')
  }

  return (
    <View style={estilos.contenedor}>
      <Text style={estilos.titulo}>Panel de Sesiones</Text>
      <Text style={estilos.parrafo}>Bienvenido, {usuario?.nombre ?? '...'}.</Text>
      <Text style={estilos.parrafo}>Rol: {usuario?.rol ?? '...'}</Text>

      <TouchableOpacity style={estilos.boton} onPress={cerrarSesion}>
        <Text style={estilos.textoBoton}>Cerrar sesión</Text>
      </TouchableOpacity>
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: { flex: 1, padding: 24, backgroundColor: '#f4f6fb' },
  titulo: { fontSize: 22, fontWeight: '700', marginBottom: 12, marginTop: 60 },
  parrafo: { fontSize: 16, marginBottom: 8 },
  boton: {
    marginTop: 24,
    backgroundColor: '#1d4ed8',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center'
  },
  textoBoton: { color: '#fff', fontWeight: '600' }
})
