import { useMemo, useState } from 'react'
import {
  Alert,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View
} from 'react-native'
import { useRouter } from 'expo-router'
import {
  registrarParticipanteApi,
  ErrorValidacionRegistro,
  type DatosRegistroParticipante
} from '../autenticacion/clienteApi'
import { tema } from '../estilos/tema'

// HU03 — formulario de registro público de Participante desde la app móvil.
// Envía los datos al endpoint /api/usuarios/participantes/registro, que crea
// el usuario en Keycloak con rol Participante y lo persiste en la base de
// datos. El backend nunca permite crear Administrador u Operador por esta vía.

type Sexo = DatosRegistroParticipante['sexo']
const SEXOS_PERMITIDOS: Sexo[] = ['Masculino', 'Femenino', 'Indefinido', 'Otro']

interface EstadoFormulario {
  alias: string
  nombreUsuario: string
  correo: string
  contrasena: string
  confirmarContrasena: string
  nombre: string
  apellido: string
  sexo: Sexo | ''
  fechaNacimiento: string
  direccion: string
  telefono: string
}

const FORMULARIO_INICIAL: EstadoFormulario = {
  alias: '',
  nombreUsuario: '',
  correo: '',
  contrasena: '',
  confirmarContrasena: '',
  nombre: '',
  apellido: '',
  sexo: '',
  fechaNacimiento: '',
  direccion: '',
  telefono: ''
}

// Mapeo entre campos del formulario y los nombres que devuelve el backend en
// los errores por campo (notación punteada para los anidados).
const CAMPO_BACKEND: Record<keyof EstadoFormulario | 'datosContacto.telefono' | 'datosContacto.direccion', string> = {
  alias: 'alias',
  nombreUsuario: 'nombreUsuario',
  correo: 'correo',
  contrasena: 'contrasena',
  confirmarContrasena: 'confirmarContrasena',
  nombre: 'nombre',
  apellido: 'apellido',
  sexo: 'sexo',
  fechaNacimiento: 'fechaNacimiento',
  direccion: 'datosContacto.direccion',
  telefono: 'datosContacto.telefono',
  'datosContacto.telefono': 'datosContacto.telefono',
  'datosContacto.direccion': 'datosContacto.direccion'
}

const REGEX_CORREO = /^[^@\s]+@[^@\s]+\.[^@\s]+$/
const REGEX_FECHA = /^\d{4}-\d{2}-\d{2}$/

export default function PantallaRegistro() {
  const [formulario, setFormulario] = useState<EstadoFormulario>(FORMULARIO_INICIAL)
  const [errores, setErrores] = useState<Record<string, string>>({})
  const [cargando, setCargando] = useState(false)
  const enrutador = useRouter()

  const actualizar = (campo: keyof EstadoFormulario, valor: string) => {
    setFormulario((prev) => ({ ...prev, [campo]: valor }))
    if (errores[CAMPO_BACKEND[campo]]) {
      setErrores((prev) => {
        const nuevo = { ...prev }
        delete nuevo[CAMPO_BACKEND[campo]]
        return nuevo
      })
    }
  }

  const seleccionarSexo = (valor: Sexo) => actualizar('sexo', valor)

  // Validaciones locales mínimas: el backend repite todas las reglas (es la
  // fuente de verdad) y devuelve errores por campo si algo se cuela.
  const validarLocalmente = (): Record<string, string> => {
    const errs: Record<string, string> = {}
    const aliasTrim = formulario.alias.trim()
    if (!aliasTrim) {
      errs.alias = 'El alias es obligatorio.'
    } else if (aliasTrim.length < 6 || aliasTrim.length > 15) {
      errs.alias = 'El alias debe tener entre 6 y 15 caracteres.'
    } else if (!/^[a-zA-Z0-9_]{6,15}$/.test(aliasTrim)) {
      errs.alias = 'El alias solo puede contener letras, números y guion bajo.'
    }
    if (!formulario.nombreUsuario.trim())
      errs.nombreUsuario = 'El nombre de usuario es obligatorio.'
    if (!formulario.correo.trim()) errs.correo = 'El correo es obligatorio.'
    else if (!REGEX_CORREO.test(formulario.correo.trim()))
      errs.correo = 'El correo no tiene un formato válido.'
    if (!formulario.contrasena) errs.contrasena = 'La contraseña es obligatoria.'
    if (!formulario.confirmarContrasena)
      errs.confirmarContrasena = 'Debe confirmar la contraseña.'
    else if (formulario.contrasena !== formulario.confirmarContrasena)
      errs.confirmarContrasena = 'Las contraseñas no coinciden.'
    if (!formulario.nombre.trim()) errs.nombre = 'El nombre es obligatorio.'
    if (!formulario.apellido.trim()) errs.apellido = 'El apellido es obligatorio.'
    if (!formulario.sexo) errs.sexo = 'Seleccione el sexo.'
    if (!formulario.fechaNacimiento.trim())
      errs.fechaNacimiento = 'La fecha de nacimiento es obligatoria.'
    else if (!REGEX_FECHA.test(formulario.fechaNacimiento.trim()))
      errs.fechaNacimiento = 'Formato esperado: YYYY-MM-DD.'
    if (!formulario.direccion.trim())
      errs['datosContacto.direccion'] = 'La dirección es obligatoria.'
    if (!formulario.telefono.trim())
      errs['datosContacto.telefono'] = 'El teléfono es obligatorio.'
    return errs
  }

  const enviar = async () => {
    const erroresLocales = validarLocalmente()
    if (Object.keys(erroresLocales).length > 0) {
      setErrores(erroresLocales)
      return
    }
    setErrores({})
    setCargando(true)
    try {
      const datos: DatosRegistroParticipante = {
        alias: formulario.alias.trim(),
        nombreUsuario: formulario.nombreUsuario.trim(),
        correo: formulario.correo.trim(),
        contrasena: formulario.contrasena,
        nombre: formulario.nombre.trim(),
        apellido: formulario.apellido.trim(),
        sexo: formulario.sexo as Sexo,
        fechaNacimiento: formulario.fechaNacimiento.trim(),
        direccion: formulario.direccion.trim(),
        telefono: formulario.telefono.trim()
      }
      await registrarParticipanteApi(datos)
      Alert.alert(
        'Cuenta creada',
        'Tu cuenta de Participante fue registrada. Ya puedes iniciar sesión.',
        [{ text: 'OK', onPress: () => enrutador.replace('/') }]
      )
    } catch (e) {
      if (e instanceof ErrorValidacionRegistro) {
        const mapa: Record<string, string> = {}
        for (const err of e.errores) mapa[err.campo] = err.mensaje
        setErrores(mapa)
        Alert.alert('Revisa los campos', e.message)
      } else {
        Alert.alert(
          'Error',
          e instanceof Error ? e.message : 'No fue posible registrar la cuenta.'
        )
      }
    } finally {
      setCargando(false)
    }
  }

  const obtenerError = (campo: keyof EstadoFormulario) =>
    errores[CAMPO_BACKEND[campo]] ?? null

  const sexos = useMemo(() => SEXOS_PERMITIDOS, [])

  return (
    <ScrollView
      style={estilos.contenedor}
      contentContainerStyle={estilos.contenido}
      keyboardShouldPersistTaps="handled"
    >
      <View style={estilos.tarjeta}>
        <Text style={estilos.titulo}>Crea tu cuenta</Text>
        <Text style={estilos.descripcion}>
          Crea tu cuenta para participar en las experiencias de UMBRAL.
          Tu alias será visible en las experiencias del juego.
        </Text>

        <Campo
          etiqueta="Alias (6-15 letras, números y guion bajo)"
          valor={formulario.alias}
          onCambio={(v) => actualizar('alias', v)}
          error={obtenerError('alias')}
          autoCapitalize="none"
        />
        <Campo
          etiqueta="Nombre de usuario"
          valor={formulario.nombreUsuario}
          onCambio={(v) => actualizar('nombreUsuario', v)}
          error={obtenerError('nombreUsuario')}
          autoCapitalize="none"
        />
        <Campo
          etiqueta="Correo"
          valor={formulario.correo}
          onCambio={(v) => actualizar('correo', v)}
          error={obtenerError('correo')}
          autoCapitalize="none"
          keyboardType="email-address"
        />
        <Campo
          etiqueta="Contraseña"
          valor={formulario.contrasena}
          onCambio={(v) => actualizar('contrasena', v)}
          error={obtenerError('contrasena')}
          secureTextEntry
        />
        <Campo
          etiqueta="Confirmar contraseña"
          valor={formulario.confirmarContrasena}
          onCambio={(v) => actualizar('confirmarContrasena', v)}
          error={obtenerError('confirmarContrasena')}
          secureTextEntry
        />
        <Campo
          etiqueta="Nombre"
          valor={formulario.nombre}
          onCambio={(v) => actualizar('nombre', v)}
          error={obtenerError('nombre')}
        />
        <Campo
          etiqueta="Apellido"
          valor={formulario.apellido}
          onCambio={(v) => actualizar('apellido', v)}
          error={obtenerError('apellido')}
        />

        <Text style={estilos.etiqueta}>Sexo</Text>
        <View style={estilos.opciones}>
          {sexos.map((s) => {
            const seleccionado = formulario.sexo === s
            return (
              <TouchableOpacity
                key={s}
                style={[estilos.opcion, seleccionado && estilos.opcionSeleccionada]}
                onPress={() => seleccionarSexo(s)}
              >
                <Text
                  style={[
                    estilos.opcionTexto,
                    seleccionado && estilos.opcionTextoSeleccionado
                  ]}
                >
                  {s}
                </Text>
              </TouchableOpacity>
            )
          })}
        </View>
        {obtenerError('sexo') && (
          <Text style={estilos.errorTexto}>{obtenerError('sexo')}</Text>
        )}

        <Campo
          etiqueta="Fecha de nacimiento (YYYY-MM-DD)"
          valor={formulario.fechaNacimiento}
          onCambio={(v) => actualizar('fechaNacimiento', v)}
          error={obtenerError('fechaNacimiento')}
          autoCapitalize="none"
        />
        <Campo
          etiqueta="Dirección"
          valor={formulario.direccion}
          onCambio={(v) => actualizar('direccion', v)}
          error={obtenerError('direccion')}
        />
        <Campo
          etiqueta="Teléfono"
          valor={formulario.telefono}
          onCambio={(v) => actualizar('telefono', v)}
          error={obtenerError('telefono')}
          keyboardType="phone-pad"
        />

        <TouchableOpacity
          style={[estilos.boton, cargando && estilos.botonDeshabilitado]}
          onPress={enviar}
          disabled={cargando}
        >
          <Text style={estilos.textoBoton}>
            {cargando ? 'Creando cuenta…' : 'Crear cuenta'}
          </Text>
        </TouchableOpacity>

        <View style={estilos.pieEnlace}>
          <TouchableOpacity onPress={() => enrutador.replace('/')}>
            <Text style={estilos.enlace}>Ya tengo cuenta</Text>
          </TouchableOpacity>
        </View>
      </View>
    </ScrollView>
  )
}

// Componente local de campo de texto con etiqueta y mensaje de error inline.
// Se mantiene en este archivo para no fragmentar la pantalla en piezas que
// solo se usan aquí (HU03).
interface PropsCampo {
  etiqueta: string
  valor: string
  onCambio: (v: string) => void
  error: string | null
  secureTextEntry?: boolean
  autoCapitalize?: 'none' | 'sentences' | 'words' | 'characters'
  keyboardType?: 'default' | 'email-address' | 'phone-pad'
}

function Campo({
  etiqueta,
  valor,
  onCambio,
  error,
  secureTextEntry,
  autoCapitalize,
  keyboardType
}: PropsCampo) {
  return (
    <View style={estilos.grupoCampo}>
      <Text style={estilos.etiqueta}>{etiqueta}</Text>
      <TextInput
        style={[estilos.entrada, error ? estilos.entradaConError : null]}
        value={valor}
        onChangeText={onCambio}
        secureTextEntry={secureTextEntry}
        autoCapitalize={autoCapitalize}
        keyboardType={keyboardType ?? 'default'}
        placeholderTextColor={tema.colores.textoTenue}
      />
      {error && <Text style={estilos.errorTexto}>{error}</Text>}
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: { flex: 1, backgroundColor: tema.colores.fondo },
  contenido: { padding: tema.espacios.lg, paddingBottom: tema.espacios.xl * 2 },
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg
  },
  titulo: {
    fontSize: 22,
    fontWeight: '800',
    color: tema.colores.texto,
    textAlign: 'center'
  },
  descripcion: {
    fontSize: 13,
    textAlign: 'center',
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
    marginBottom: tema.espacios.lg
  },
  grupoCampo: { marginBottom: tema.espacios.md },
  etiqueta: {
    color: tema.colores.texto,
    fontWeight: '600',
    fontSize: 13,
    marginBottom: tema.espacios.xs,
    marginTop: tema.espacios.xs
  },
  entrada: {
    backgroundColor: tema.colores.entradaFondo,
    borderWidth: 1,
    borderColor: tema.colores.entradaBorde,
    borderRadius: tema.radios.entrada,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.md,
    color: tema.colores.texto
  },
  entradaConError: { borderColor: tema.colores.error },
  errorTexto: {
    color: tema.colores.error,
    fontSize: 12,
    marginTop: tema.espacios.xs
  },
  opciones: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: tema.espacios.sm,
    marginBottom: tema.espacios.sm
  },
  opcion: {
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.sm,
    borderRadius: tema.radios.boton,
    borderWidth: 1,
    borderColor: tema.colores.entradaBorde,
    backgroundColor: tema.colores.entradaFondo
  },
  opcionSeleccionada: {
    backgroundColor: tema.colores.primario,
    borderColor: tema.colores.primario
  },
  opcionTexto: { color: tema.colores.textoTenue, fontWeight: '600' },
  opcionTextoSeleccionado: { color: '#fff' },
  boton: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: 'center',
    marginTop: tema.espacios.lg
  },
  botonDeshabilitado: { backgroundColor: tema.colores.primarioDeshabilitado },
  textoBoton: { color: '#fff', fontWeight: '700', fontSize: 15 },
  pieEnlace: {
    flexDirection: 'row',
    justifyContent: 'center',
    marginTop: tema.espacios.lg
  },
  enlace: { color: tema.colores.enlace, fontWeight: '700', fontSize: 13 }
})
