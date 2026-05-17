import { useState } from 'react'
import { Alert, StyleSheet, Text, TextInput, TouchableOpacity, View } from 'react-native'
import { useRouter } from 'expo-router'
import { iniciarSesionApi } from '../autenticacion/clienteApi'
import { guardarSesion } from '../autenticacion/almacenamientoSeguro'

export default function PantallaInicioSesion() {
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [cargando, setCargando] = useState(false)
  const enrutador = useRouter()

  const enviar = async () => {
    setCargando(true)
    try {
      // El backend valida el origen y rechaza con 403 si el rol no es Participante.
      const respuesta = await iniciarSesionApi(nombreUsuario, contrasena)

      await guardarSesion(respuesta.tokenAcceso, respuesta.usuario)
      enrutador.replace('/participante/sesiones')
    } catch (e) {
      Alert.alert('Error', e instanceof Error ? e.message : 'Error desconocido.')
    } finally {
      setCargando(false)
    }
  }

  return (
    <View style={estilos.contenedor}>
      <Text style={estilos.titulo}>UMBRAL</Text>
      <Text style={estilos.subtitulo}>Iniciar sesión</Text>

      <TextInput
        style={estilos.entrada}
        placeholder="Nombre de usuario"
        autoCapitalize="none"
        value={nombreUsuario}
        onChangeText={setNombreUsuario}
      />
      <TextInput
        style={estilos.entrada}
        placeholder="Contraseña"
        secureTextEntry
        value={contrasena}
        onChangeText={setContrasena}
      />

      <TouchableOpacity
        style={[estilos.boton, cargando && estilos.botonDeshabilitado]}
        onPress={enviar}
        disabled={cargando}
      >
        <Text style={estilos.textoBoton}>
          {cargando ? 'Ingresando…' : 'Iniciar sesión'}
        </Text>
      </TouchableOpacity>
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: { flex: 1, padding: 24, justifyContent: 'center', backgroundColor: '#f4f6fb' },
  titulo: { fontSize: 28, fontWeight: '700', textAlign: 'center', marginBottom: 4 },
  subtitulo: { fontSize: 16, textAlign: 'center', marginBottom: 24, color: '#475569' },
  entrada: {
    backgroundColor: '#fff',
    borderWidth: 1,
    borderColor: '#cbd5e1',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    marginBottom: 12
  },
  boton: {
    backgroundColor: '#1d4ed8',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
    marginTop: 8
  },
  botonDeshabilitado: { backgroundColor: '#94a3b8' },
  textoBoton: { color: '#fff', fontWeight: '600' }
})
