import { useState } from 'react'
import {
  Alert,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View
} from 'react-native'
import { useRouter } from 'expo-router'
import { iniciarSesionApi } from '../autenticacion/clienteApi'
import { guardarSesion } from '../autenticacion/almacenamientoSeguro'
import { tema } from '../estilos/tema'

// HU04 (login móvil existente) + acceso a HU03 (registro de Participante).
// La app móvil es exclusiva del Participante: si un Administrador/Operador
// intenta entrar, el backend responde 403 desde /api/autenticacion/login-movil.
export default function PantallaInicioSesion() {
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [cargando, setCargando] = useState(false)
  const enrutador = useRouter()

  const enviar = async () => {
    setCargando(true)
    try {
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
      <View style={estilos.tarjeta}>
        <Text style={estilos.titulo}>UMBRAL</Text>
        <Text style={estilos.subtitulo}>Accede como Participante</Text>
        <Text style={estilos.descripcion}>
          Entra a tus sesiones, trivias y búsquedas del tesoro.
        </Text>

        <TextInput
          style={estilos.entrada}
          placeholder="Nombre de usuario"
          placeholderTextColor={tema.colores.textoTenue}
          autoCapitalize="none"
          value={nombreUsuario}
          onChangeText={setNombreUsuario}
        />
        <TextInput
          style={estilos.entrada}
          placeholder="Contraseña"
          placeholderTextColor={tema.colores.textoTenue}
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

        <View style={estilos.pieEnlace}>
          <Text style={estilos.textoTenue}>¿No tienes cuenta? </Text>
          <TouchableOpacity onPress={() => enrutador.push('/registro')}>
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
