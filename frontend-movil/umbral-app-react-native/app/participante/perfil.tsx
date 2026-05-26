import { useCallback, useEffect, useState, type ReactNode } from 'react'
import {
  ActivityIndicator,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View
} from 'react-native'
import { useRouter } from 'expo-router'
import {
  ErrorConsultaPerfil,
  obtenerPerfilActualApi,
  type PerfilParticipante
} from '../../autenticacion/clienteApi'
import { obtenerToken } from '../../autenticacion/almacenamientoSeguro'
import { useAutenticacion } from '../../autenticacion/ContextoAutenticacion'
import RutaProtegidaMovil from '../../componentes/RutaProtegidaMovil'
import { tema } from '../../estilos/tema'

// HU04 — Consultar perfil del Participante. Consume el endpoint protegido
// GET /api/autenticacion/perfil-actual, que toma el id del usuario desde el
// token. El Participante solo puede ver su propio perfil (no expone id).
export default function PantallaPerfilParticipante() {
  return (
    <RutaProtegidaMovil>
      <ContenidoPerfil />
    </RutaProtegidaMovil>
  )
}

function ContenidoPerfil() {
  const enrutador = useRouter()
  const { cerrarSesion } = useAutenticacion()
  const [perfil, setPerfil] = useState<PerfilParticipante | null>(null)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const cargarPerfil = useCallback(async () => {
    setCargando(true)
    setError(null)
    try {
      const token = await obtenerToken()
      if (!token) {
        enrutador.replace('/')
        return
      }
      const datos = await obtenerPerfilActualApi(token)
      // Defensa visual: la app móvil es exclusiva del Participante. Si por
      // cualquier motivo el backend respondiera otro rol, cerramos sesión
      // para no mostrar contenido de un panel administrativo.
      if (datos.rol !== 'Participante') {
        await cerrarSesion()
        enrutador.replace('/')
        return
      }
      setPerfil(datos)
    } catch (e) {
      if (e instanceof ErrorConsultaPerfil && e.codigo === 'NO_AUTORIZADO') {
        await cerrarSesion()
        enrutador.replace('/')
        return
      }
      setError(
        e instanceof Error ? e.message : 'No fue posible consultar tu perfil.'
      )
    } finally {
      setCargando(false)
    }
  }, [enrutador, cerrarSesion])

  useEffect(() => {
    cargarPerfil()
  }, [cargarPerfil])

  const volverAlMenu = () => enrutador.replace('/participante/menu')

  if (cargando) {
    return (
      <View style={estilos.contenedorEstado}>
        <ActivityIndicator color={tema.colores.primario} size="large" />
        <Text style={estilos.textoEstado}>Cargando tu perfil…</Text>
      </View>
    )
  }

  if (error) {
    return (
      <View style={estilos.contenedorEstado}>
        <View style={estilos.cuadroError}>
          <Text style={estilos.cuadroErrorTexto}>{error}</Text>
        </View>
        <TouchableOpacity style={estilos.botonPrimario} onPress={cargarPerfil}>
          <Text style={estilos.botonPrimarioTexto}>Reintentar</Text>
        </TouchableOpacity>
        <TouchableOpacity style={estilos.botonSecundario} onPress={volverAlMenu}>
          <Text style={estilos.botonSecundarioTexto}>Volver al menú</Text>
        </TouchableOpacity>
      </View>
    )
  }

  if (!perfil) return null

  const estadoActivo = perfil.estado?.toLowerCase() === 'activo'

  return (
    <ScrollView
      style={estilos.contenedor}
      contentContainerStyle={estilos.contenido}
    >
      <Text style={estilos.titulo}>UMBRAL</Text>
      <Text style={estilos.subtitulo}>Mi perfil</Text>

      <View style={estilos.tarjetaPrincipal}>
        <Text style={estilos.aliasEtiqueta}>Alias</Text>
        <Text style={estilos.aliasValor}>
          {valorOMarcador(perfil.alias)}
        </Text>
        <Text style={estilos.nombreUsuario}>@{perfil.nombreUsuario}</Text>
        <View
          style={[
            estilos.badgeEstado,
            estadoActivo ? estilos.badgeActivo : estilos.badgeInactivo
          ]}
        >
          <Text
            style={[
              estilos.badgeEstadoTexto,
              estadoActivo
                ? estilos.badgeEstadoTextoActivo
                : estilos.badgeEstadoTextoInactivo
            ]}
          >
            {perfil.estado?.toUpperCase() || 'NO DISPONIBLE'}
          </Text>
        </View>
      </View>

      <Seccion titulo="Datos personales">
        <Fila etiqueta="Nombre" valor={perfil.nombre} />
        <Fila etiqueta="Apellido" valor={perfil.apellido} />
        <Fila etiqueta="Correo" valor={perfil.correo} />
        <Fila etiqueta="Sexo" valor={perfil.sexo} />
        <Fila
          etiqueta="Fecha de nacimiento"
          valor={formatearFecha(perfil.fechaNacimiento)}
        />
      </Seccion>

      <Seccion titulo="Contacto">
        <Fila etiqueta="Teléfono" valor={perfil.datosContacto?.telefono} />
        <Fila etiqueta="Dirección" valor={perfil.datosContacto?.direccion} />
      </Seccion>

      <Seccion titulo="Cuenta">
        <Fila
          etiqueta="Fecha de registro"
          valor={formatearFecha(perfil.fechaRegistro)}
        />
      </Seccion>

      {/* HU10 — atajo a la pantalla de edición del propio perfil. */}
      <TouchableOpacity
        style={estilos.botonPrimario}
        onPress={() => enrutador.push('/participante/editar-perfil')}
      >
        <Text style={estilos.botonPrimarioTexto}>Editar perfil</Text>
      </TouchableOpacity>

      <TouchableOpacity style={estilos.botonSecundario} onPress={volverAlMenu}>
        <Text style={estilos.botonSecundarioTexto}>Volver al menú</Text>
      </TouchableOpacity>
    </ScrollView>
  )
}

// Marcador uniforme para cualquier campo nulo, indefinido o vacío.
function valorOMarcador(valor?: string | null): string {
  if (valor === null || valor === undefined) return 'No disponible'
  const limpio = valor.trim()
  return limpio.length === 0 ? 'No disponible' : limpio
}

// Las fechas del backend llegan como ISO 8601 (DateTime serializado).
// Mostramos sólo la parte de fecha para mantener el perfil legible en móvil.
function formatearFecha(valor?: string | null): string {
  if (!valor) return 'No disponible'
  const fecha = new Date(valor)
  if (isNaN(fecha.getTime())) return valorOMarcador(valor)
  const anio = fecha.getFullYear()
  const mes = String(fecha.getMonth() + 1).padStart(2, '0')
  const dia = String(fecha.getDate()).padStart(2, '0')
  return `${anio}-${mes}-${dia}`
}

interface PropsSeccion {
  titulo: string
  children: ReactNode
}

function Seccion({ titulo, children }: PropsSeccion) {
  return (
    <View style={estilos.seccion}>
      <Text style={estilos.seccionTitulo}>{titulo}</Text>
      <View style={estilos.seccionTarjeta}>{children}</View>
    </View>
  )
}

function Fila({
  etiqueta,
  valor
}: {
  etiqueta: string
  valor?: string | null
}) {
  return (
    <View style={estilos.fila}>
      <Text style={estilos.filaEtiqueta}>{etiqueta}</Text>
      <Text style={estilos.filaValor}>{valorOMarcador(valor)}</Text>
    </View>
  )
}

const estilos = StyleSheet.create({
  contenedor: { flex: 1, backgroundColor: tema.colores.fondo },
  contenido: {
    padding: tema.espacios.lg,
    paddingTop: tema.espacios.xl * 2,
    paddingBottom: tema.espacios.xl * 2
  },
  contenedorEstado: {
    flex: 1,
    backgroundColor: tema.colores.fondo,
    alignItems: 'center',
    justifyContent: 'center',
    padding: tema.espacios.lg
  },
  textoEstado: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
    fontSize: 14
  },
  titulo: {
    fontSize: 24,
    fontWeight: '800',
    textAlign: 'center',
    color: tema.colores.texto,
    letterSpacing: 3
  },
  subtitulo: {
    fontSize: 12,
    textAlign: 'center',
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
    marginBottom: tema.espacios.lg,
    letterSpacing: 2,
    textTransform: 'uppercase'
  },
  tarjetaPrincipal: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    alignItems: 'center',
    marginBottom: tema.espacios.lg
  },
  aliasEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: 11,
    letterSpacing: 2,
    textTransform: 'uppercase'
  },
  aliasValor: {
    color: tema.colores.texto,
    fontSize: 28,
    fontWeight: '800',
    marginTop: tema.espacios.xs,
    textAlign: 'center'
  },
  nombreUsuario: {
    color: tema.colores.enlace,
    fontSize: 14,
    marginTop: tema.espacios.xs,
    fontWeight: '600'
  },
  badgeEstado: {
    marginTop: tema.espacios.md,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: 4,
    borderRadius: 999,
    borderWidth: 1
  },
  badgeActivo: {
    backgroundColor: 'rgba(124,92,255,0.18)',
    borderColor: tema.colores.primario
  },
  badgeInactivo: {
    backgroundColor: 'rgba(255,107,107,0.15)',
    borderColor: tema.colores.error
  },
  badgeEstadoTexto: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 2
  },
  badgeEstadoTextoActivo: { color: tema.colores.enlace },
  badgeEstadoTextoInactivo: { color: tema.colores.error },
  seccion: { marginBottom: tema.espacios.lg },
  seccionTitulo: {
    color: tema.colores.textoTenue,
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 2,
    textTransform: 'uppercase',
    marginBottom: tema.espacios.sm,
    marginLeft: tema.espacios.xs
  },
  seccionTarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.sm
  },
  fila: {
    paddingVertical: tema.espacios.sm,
    borderBottomWidth: 1,
    borderBottomColor: tema.colores.bordeTarjeta
  },
  filaEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: 11,
    letterSpacing: 1,
    textTransform: 'uppercase'
  },
  filaValor: {
    color: tema.colores.texto,
    fontSize: 14,
    marginTop: 2,
    fontWeight: '500'
  },
  cuadroError: {
    backgroundColor: 'rgba(255,107,107,0.12)',
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.lg,
    width: '100%'
  },
  cuadroErrorTexto: {
    color: tema.colores.error,
    fontSize: 13,
    textAlign: 'center'
  },
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: 'center',
    marginTop: tema.espacios.sm
  },
  botonPrimarioTexto: {
    color: '#fff',
    fontWeight: '700',
    fontSize: 15
  },
  botonSecundario: {
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: 'center',
    marginTop: tema.espacios.sm,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondoTarjeta,
    width: '100%'
  },
  botonSecundarioTexto: {
    color: tema.colores.texto,
    fontWeight: '700',
    fontSize: 14
  }
})
