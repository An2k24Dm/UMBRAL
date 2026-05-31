import { StyleSheet, Text, TouchableOpacity, View } from 'react-native'
import { useRouter } from 'expo-router'
import { tema } from '../estilos/tema'

interface Props {
  titulo: string
  mostrarVolver?: boolean
  onVolver?: () => void
}

export function EncabezadoMovil({ titulo, mostrarVolver = false, onVolver }: Props) {
  const enrutador = useRouter()

  const handleVolver = () => {
    if (onVolver) {
      onVolver()
    } else {
      enrutador.back()
    }
  }

  return (
    <View style={estilos.contenedor}>
      {mostrarVolver ? (
        <TouchableOpacity
          onPress={handleVolver}
          style={estilos.botonVolver}
          hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
        >
          <Text style={estilos.flechaVolver}>←</Text>
        </TouchableOpacity>
      ) : (
        <View style={estilos.espaciador} />
      )}
      <Text style={estilos.titulo} numberOfLines={1}>{titulo}</Text>
      <View style={estilos.espaciador} />
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: tema.espacios.md,
    paddingHorizontal: tema.espacios.lg,
    borderBottomWidth: 1,
    borderBottomColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondo,
  },
  titulo: {
    flex: 1,
    textAlign: 'center',
    fontSize: tema.tipografia.tamanos.xl,
    fontWeight: tema.tipografia.pesos.bold,
    color: tema.colores.texto,
  },
  botonVolver: {
    width: 36,
    height: 36,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: tema.radios.boton,
    backgroundColor: tema.colores.fondoTarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
  },
  flechaVolver: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.xl,
    lineHeight: 22,
  },
  espaciador: { width: 36 },
})
