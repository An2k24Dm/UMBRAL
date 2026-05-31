import type { ReactNode } from 'react'
import { StyleSheet, View } from 'react-native'
import type { ViewStyle } from 'react-native'
import { tema } from '../estilos/tema'

interface Props {
  children: ReactNode
  style?: ViewStyle
}

export function TarjetaMovil({ children, style }: Props) {
  return <View style={[estilos.tarjeta, style]}>{children}</View>
}

const estilos = StyleSheet.create({
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.lg,
    ...tema.sombras.tarjeta,
  },
})
