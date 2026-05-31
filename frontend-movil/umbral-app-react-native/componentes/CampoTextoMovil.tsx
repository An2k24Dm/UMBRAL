import { StyleSheet, Text, TextInput, View } from 'react-native'
import type { KeyboardTypeOptions, TextInputProps } from 'react-native'
import { tema } from '../estilos/tema'

interface Props extends Pick<TextInputProps, 'autoCapitalize' | 'keyboardType' | 'textContentType' | 'autoComplete'> {
  etiqueta: string
  valor: string
  onCambio: (valor: string) => void
  error?: string | null
  placeholder?: string
  secureTextEntry?: boolean
  editable?: boolean
  multiline?: boolean
  numberOfLines?: number
  keyboardType?: KeyboardTypeOptions
}

export function CampoTextoMovil({
  etiqueta,
  valor,
  onCambio,
  error,
  placeholder,
  secureTextEntry = false,
  editable = true,
  multiline = false,
  numberOfLines,
  autoCapitalize,
  keyboardType,
  textContentType,
  autoComplete,
}: Props) {
  return (
    <View style={estilos.contenedor}>
      <Text style={estilos.etiqueta}>{etiqueta}</Text>
      <TextInput
        style={[
          estilos.entrada,
          error ? estilos.entradaError : null,
          !editable ? estilos.entradaDeshabilitada : null,
          multiline ? estilos.entradaMultiline : null,
        ]}
        value={valor}
        onChangeText={onCambio}
        placeholder={placeholder}
        placeholderTextColor={tema.colores.textoTenue}
        secureTextEntry={secureTextEntry}
        editable={editable}
        multiline={multiline}
        numberOfLines={numberOfLines}
        autoCapitalize={autoCapitalize}
        keyboardType={keyboardType}
        textContentType={textContentType}
        autoComplete={autoComplete}
      />
      {error ? <Text style={estilos.textoError}>{error}</Text> : null}
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: { marginBottom: tema.espacios.md },
  etiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.semibold,
    textTransform: 'uppercase',
    letterSpacing: tema.tipografia.espaciadoLetra.xs,
    marginBottom: tema.espacios.xs,
  },
  entrada: {
    backgroundColor: tema.colores.entradaFondo,
    borderWidth: 1,
    borderColor: tema.colores.entradaBorde,
    borderRadius: tema.radios.entrada,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.md,
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
  },
  entradaError: { borderColor: tema.colores.error },
  entradaDeshabilitada: { opacity: 0.6 },
  entradaMultiline: { minHeight: 80, textAlignVertical: 'top' },
  textoError: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
  },
})
