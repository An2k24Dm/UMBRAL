import { useMemo, useState } from 'react'
import {
  Modal,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  TouchableOpacity,
  View
} from 'react-native'
import DateTimePicker, {
  type DateTimePickerEvent
} from '@react-native-community/datetimepicker'
import { tema } from '../estilos/tema'

// Selector reutilizable de fecha de nacimiento para los formularios móviles
// (HU03 registro y HU10 edición de perfil). Mantiene en un único lugar:
//  * El formato amigable de visualización (dd/MM/yyyy).
//  * El formato seguro hacia el backend (yyyy-MM-dd).
//  * Las cotas (no fecha futura; edad mínima/máxima según la regla del
//    backend en ReglasValidacionUsuario.ValidarFechaNacimiento).
//
// El valor se almacena fuera del componente como string yyyy-MM-dd. Si el
// usuario cancela, el valor original se preserva — eso lo garantiza el
// patrón "draft + onConfirmar" interno: el calendario actualiza un draft y
// sólo lo emitimos vía onCambio cuando se confirma.
export interface PropsSelectorFechaNacimiento {
  etiqueta: string
  // Fecha actual en formato yyyy-MM-dd (o cadena vacía si aún no se eligió).
  valor: string
  // Recibe la fecha confirmada en formato yyyy-MM-dd. Sólo se invoca al
  // confirmar (Aceptar) — Cancelar restaura el valor anterior.
  onCambio: (yyyymmdd: string) => void
  error?: string | null
  // Edad mínima/máxima permitidas. Coinciden con la regla de dominio del
  // backend (18 y 100 años). El selector las usa para limitar el rango
  // navegable del calendario y reducir errores de selección.
  edadMinimaAnios?: number
  edadMaximaAnios?: number
}

export function SelectorFechaNacimiento(props: PropsSelectorFechaNacimiento) {
  const {
    etiqueta,
    valor,
    onCambio,
    error,
    edadMinimaAnios = 18,
    edadMaximaAnios = 100
  } = props

  const [abierto, setAbierto] = useState(false)
  // Fecha "borrador" mientras el modal está abierto. Sólo se promueve a
  // onCambio cuando el usuario confirma. Si cancela, se descarta.
  const [borrador, setBorrador] = useState<Date | null>(null)

  const { fechaMaxima, fechaMinima, fechaInicial } = useMemo(() => {
    const hoy = new Date()
    // No permitir fechas futuras: máximo = hace `edadMinimaAnios` años.
    const max = new Date(
      hoy.getFullYear() - edadMinimaAnios,
      hoy.getMonth(),
      hoy.getDate()
    )
    // Mínimo = hace `edadMaximaAnios` años.
    const min = new Date(
      hoy.getFullYear() - edadMaximaAnios,
      hoy.getMonth(),
      hoy.getDate()
    )
    // Inicial = valor actual si existe; si no, hace 25 años (anclaje cómodo
    // dentro del rango permitido).
    const desdeValor = parsearYyyyMmDd(valor)
    const inicial = desdeValor ?? new Date(
      hoy.getFullYear() - 25, hoy.getMonth(), hoy.getDate()
    )
    return { fechaMaxima: max, fechaMinima: min, fechaInicial: inicial }
  }, [valor, edadMinimaAnios, edadMaximaAnios])

  const textoVisible = valor
    ? formatearAmigable(valor)
    : 'Selecciona tu fecha de nacimiento'

  const abrir = () => {
    setBorrador(fechaInicial)
    setAbierto(true)
  }
  const cancelar = () => {
    setBorrador(null)
    setAbierto(false)
  }
  const confirmar = () => {
    if (borrador) onCambio(formatearYyyyMmDd(borrador))
    setAbierto(false)
  }

  // En iOS el picker se monta inline dentro del modal y la confirmación
  // requiere botones explícitos. En Android la API "default" es un diálogo
  // nativo modal donde el botón OK ya cierra el picker y emite el evento
  // type === 'set'; en ese caso emitimos onCambio directo y cerramos.
  const onCambioPicker = (evento: DateTimePickerEvent, fecha?: Date) => {
    if (Platform.OS === 'android') {
      if (evento.type === 'set' && fecha) {
        onCambio(formatearYyyyMmDd(fecha))
      }
      setAbierto(false)
      setBorrador(null)
      return
    }
    if (fecha) setBorrador(fecha)
  }

  return (
    <View style={estilos.contenedor}>
      <Text style={estilos.etiqueta}>{etiqueta}</Text>

      {/* Pressable explícito: NO usamos TextInput para evitar que se abra el
          teclado del sistema. accessibilityRole="button" deja claro a lectores
          de pantalla que es un selector. */}
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={etiqueta}
        onPress={abrir}
        style={[estilos.entrada, error ? estilos.entradaError : null]}
        testID="selector-fecha-nacimiento"
      >
        <Text style={valor ? estilos.entradaTexto : estilos.entradaPlaceholder}>
          {textoVisible}
        </Text>
      </Pressable>
      {error ? <Text style={estilos.errorTexto}>{error}</Text> : null}

      {/* En Android usamos el diálogo nativo (sin modal envolvente). */}
      {abierto && Platform.OS === 'android' && (
        <DateTimePicker
          mode="date"
          value={borrador ?? fechaInicial}
          maximumDate={fechaMaxima}
          minimumDate={fechaMinima}
          onChange={onCambioPicker}
          display="default"
        />
      )}

      {/* En iOS envolvemos el picker en un Modal con botones Cancelar / Aceptar
          para que el valor anterior se preserve si el usuario cancela. */}
      {Platform.OS === 'ios' && (
        <Modal
          visible={abierto}
          transparent
          animationType="slide"
          onRequestClose={cancelar}
        >
          <View style={estilos.modalOverlay}>
            <View style={estilos.modalCaja}>
              <DateTimePicker
                mode="date"
                value={borrador ?? fechaInicial}
                maximumDate={fechaMaxima}
                minimumDate={fechaMinima}
                onChange={onCambioPicker}
                display="spinner"
                themeVariant="dark"
              />
              <View style={estilos.modalAcciones}>
                <TouchableOpacity onPress={cancelar} style={estilos.botonModal}>
                  <Text style={estilos.botonModalTexto}>Cancelar</Text>
                </TouchableOpacity>
                <TouchableOpacity
                  onPress={confirmar}
                  style={[estilos.botonModal, estilos.botonModalPrimario]}
                >
                  <Text style={estilos.botonModalPrimarioTexto}>Aceptar</Text>
                </TouchableOpacity>
              </View>
            </View>
          </View>
        </Modal>
      )}
    </View>
  )
}

// Formato hacia el backend: yyyy-MM-dd. El backend ya tolera tanto
// "yyyy-MM-dd" como ISO 8601 con tiempo, pero usar la versión corta evita
// problemas de zona horaria que desplazarían la fecha por un día.
function formatearYyyyMmDd(fecha: Date): string {
  const y = fecha.getFullYear()
  const m = String(fecha.getMonth() + 1).padStart(2, '0')
  const d = String(fecha.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

// Formato visible al usuario.
function formatearAmigable(yyyymmdd: string): string {
  const fecha = parsearYyyyMmDd(yyyymmdd)
  if (!fecha) return yyyymmdd
  const d = String(fecha.getDate()).padStart(2, '0')
  const m = String(fecha.getMonth() + 1).padStart(2, '0')
  const y = fecha.getFullYear()
  return `${d}/${m}/${y}`
}

function parsearYyyyMmDd(valor: string): Date | null {
  if (!valor) return null
  // Aceptamos "yyyy-MM-dd" y "yyyy-MM-ddT..."
  const corto = valor.length >= 10 ? valor.slice(0, 10) : valor
  if (!/^\d{4}-\d{2}-\d{2}$/.test(corto)) return null
  const [y, m, d] = corto.split('-').map(Number)
  const fecha = new Date(y, m - 1, d)
  // Verificación de validez: una fecha como "2000-12-56" produce NaN o
  // se desborda a otro mes. El selector reconstruido aquí sólo recibe valores
  // emitidos por nosotros mismos, pero defendemos por si llegara basura
  // (p. ej. del backend en un escenario corrupto).
  if (
    fecha.getFullYear() !== y ||
    fecha.getMonth() !== m - 1 ||
    fecha.getDate() !== d
  ) {
    return null
  }
  return fecha
}

const estilos = StyleSheet.create({
  contenedor: { marginBottom: tema.espacios.md },
  etiqueta: {
    color: tema.colores.textoTenue,
    fontSize: 11,
    textTransform: 'uppercase',
    letterSpacing: 1,
    marginBottom: 4
  },
  entrada: {
    backgroundColor: tema.colores.entradaFondo,
    borderColor: tema.colores.entradaBorde,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.md
  },
  entradaError: { borderColor: tema.colores.error },
  entradaTexto: { color: tema.colores.texto, fontSize: 14 },
  entradaPlaceholder: { color: tema.colores.textoTenue, fontSize: 14 },
  errorTexto: {
    color: tema.colores.error,
    fontSize: 12,
    marginTop: 4
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end'
  },
  modalCaja: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderTopLeftRadius: tema.radios.tarjeta,
    borderTopRightRadius: tema.radios.tarjeta,
    paddingBottom: tema.espacios.lg
  },
  modalAcciones: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    paddingHorizontal: tema.espacios.lg,
    paddingTop: tema.espacios.sm
  },
  botonModal: {
    paddingHorizontal: tema.espacios.lg,
    paddingVertical: tema.espacios.sm,
    borderRadius: tema.radios.boton,
    marginLeft: tema.espacios.sm
  },
  botonModalTexto: { color: tema.colores.texto, fontWeight: '600' },
  botonModalPrimario: { backgroundColor: tema.colores.primario },
  botonModalPrimarioTexto: { color: '#fff', fontWeight: '700' }
})
