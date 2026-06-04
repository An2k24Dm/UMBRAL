import { StyleSheet, Text, TouchableOpacity, View } from 'react-native'
import { tema } from '../../estilos/tema'

interface Props {
  titulo: string
  descripcion: string
  alPresionar: () => void
}

// Tarjeta reusable para opciones del menú del Participante. Antes el
// JSX vivía repetido dentro de `menu.tsx`; al separarlo, agregar una
// opción nueva solo requiere un `<TarjetaOpcionMenu />` más.
export function TarjetaOpcionMenu({ titulo, descripcion, alPresionar }: Props) {
  return (
    <TouchableOpacity
      activeOpacity={0.7}
      onPress={alPresionar}
      style={estilos.tarjetaOpcion}
    >
      <View style={estilos.opcionIndicador} />
      <View style={estilos.opcionCuerpo}>
        <Text style={estilos.opcionTitulo}>{titulo}</Text>
        <Text style={estilos.opcionDescripcion}>{descripcion}</Text>
      </View>
      <Text style={estilos.opcionFlecha}>›</Text>
    </TouchableOpacity>
  )
}

const estilos = StyleSheet.create({
  tarjetaOpcion: {
    flexDirection: 'row',
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
    alignItems: 'center',
  },
  opcionIndicador: {
    width: 4,
    height: 40,
    borderRadius: tema.radios.pastilla,
    backgroundColor: tema.colores.primario,
    marginRight: tema.espacios.md,
  },
  opcionCuerpo: { flex: 1 },
  opcionTitulo: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  opcionDescripcion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
    lineHeight: 18,
  },
  opcionFlecha: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.h4,
    marginLeft: tema.espacios.sm,
  },
})
