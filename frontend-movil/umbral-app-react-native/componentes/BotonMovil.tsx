import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from 'react-native'
import { tema } from '../estilos/tema'

type Variante = 'primario' | 'secundario' | 'peligro'

interface Props {
  titulo: string
  onPress: () => void
  variante?: Variante
  deshabilitado?: boolean
  cargando?: boolean
  ancho?: boolean
}

export function BotonMovil({
  titulo,
  onPress,
  variante = 'primario',
  deshabilitado = false,
  cargando = false,
  ancho = true,
}: Props) {
  const estaDeshabilitado = deshabilitado || cargando

  return (
    <TouchableOpacity
      style={[
        estilos.base,
        estilos[variante],
        estaDeshabilitado && estilos.deshabilitado,
        ancho && estilos.ancho,
      ]}
      onPress={onPress}
      disabled={estaDeshabilitado}
      activeOpacity={0.75}
    >
      {cargando ? (
        <View style={estilos.fila}>
          <ActivityIndicator
            size="small"
            color={variante === 'secundario' ? tema.colores.texto : tema.colores.textoBlanco}
          />
          <Text style={[estilos.texto, estilos[`texto_${variante}` as keyof typeof estilos]]}>
            {titulo}
          </Text>
        </View>
      ) : (
        <Text style={[estilos.texto, estilos[`texto_${variante}` as keyof typeof estilos]]}>
          {titulo}
        </Text>
      )}
    </TouchableOpacity>
  )
}

const estilos = StyleSheet.create({
  base: {
    paddingVertical: tema.espacios.md,
    paddingHorizontal: tema.espacios.lg,
    borderRadius: tema.radios.boton,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: tema.espacios.sm,
  },
  ancho: { width: '100%' },
  fila: { flexDirection: 'row', alignItems: 'center', gap: tema.espacios.sm },
  primario: { backgroundColor: tema.colores.primario },
  secundario: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
  },
  peligro: { backgroundColor: tema.colores.error },
  deshabilitado: { opacity: 0.5 },
  texto: {
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  texto_primario: { color: tema.colores.textoBlanco },
  texto_secundario: { color: tema.colores.texto },
  texto_peligro: { color: tema.colores.textoBlanco },
})
