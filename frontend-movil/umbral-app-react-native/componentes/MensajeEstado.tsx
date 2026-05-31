import { StyleSheet, Text, View } from 'react-native'
import { tema } from '../estilos/tema'

type Tipo = 'error' | 'exito' | 'info' | 'aviso'

interface Props {
  tipo: Tipo
  mensaje: string
}

const configuracion: Record<Tipo, { fondo: string; borde: string; color: string }> = {
  error: {
    fondo: tema.colores.errorSuave,
    borde: tema.colores.error,
    color: tema.colores.error,
  },
  exito: {
    fondo: tema.colores.exitoSuave,
    borde: tema.colores.exito,
    color: tema.colores.exito,
  },
  info: {
    fondo: tema.colores.infoSuave,
    borde: tema.colores.info,
    color: tema.colores.info,
  },
  aviso: {
    fondo: tema.colores.avisoSuave,
    borde: tema.colores.aviso,
    color: tema.colores.aviso,
  },
}

export function MensajeEstado({ tipo, mensaje }: Props) {
  const config = configuracion[tipo]
  return (
    <View
      style={[
        estilos.contenedor,
        { backgroundColor: config.fondo, borderColor: config.borde },
      ]}
    >
      <Text style={[estilos.texto, { color: config.color }]}>{mensaje}</Text>
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: {
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  texto: {
    fontSize: tema.tipografia.tamanos.base,
    textAlign: 'center',
    fontWeight: tema.tipografia.pesos.semibold,
    lineHeight: 20,
  },
})
