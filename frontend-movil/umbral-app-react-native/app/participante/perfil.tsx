import { useCallback, useEffect, useState, type ReactNode } from 'react'
import {
  ActivityIndicator,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View
} from 'react-native'
import { useRouter } from 'expo-router'
import {
  ErrorConsultaPerfil,
  ErrorEliminarCuenta,
  eliminarCuentaParticipanteApi,
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
  const { cerrarSesion, usuario } = useAutenticacion()
  const [perfil, setPerfil] = useState<PerfilParticipante | null>(null)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // HU11 — estado del menú de engranaje, del modal de confirmación de
  // eliminación de cuenta y de la eliminación en curso. El botón "Eliminar
  // cuenta" debe desactivarse mientras `eliminandoCuenta` esté en true para
  // evitar doble click; los errores se muestran dentro del modal sin
  // cerrar la sesión salvo que el backend confirme la eliminación.
  const [menuAbierto, setMenuAbierto] = useState(false)
  const [confirmacionAbierta, setConfirmacionAbierta] = useState(false)
  const [eliminandoCuenta, setEliminandoCuenta] = useState(false)
  const [errorEliminar, setErrorEliminar] = useState<string | null>(null)

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

  // HU11 — apertura/cierre del menú flotante de engranaje. Por seguridad,
  // el menú sólo aparece para Participantes (el contexto sólo guarda sesiones
  // de Participante, pero verificamos defensivamente).
  const esParticipante = usuario?.rol === 'Participante'
  const abrirMenu = () => setMenuAbierto(true)
  const cerrarMenu = () => setMenuAbierto(false)

  // HU11 — Cancelar cierra el modal sin llamar al backend; ningún estado
  // remoto se altera.
  const cancelarEliminacion = () => {
    if (eliminandoCuenta) return
    setConfirmacionAbierta(false)
    setErrorEliminar(null)
  }

  // HU11 — confirmación: llama al endpoint DELETE. Si el backend confirma la
  // eliminación, limpiamos token y sesión local y redirigimos al login. Si
  // falla, mostramos error claro y mantenemos la sesión salvo que el error
  // indique que la cuenta ya fue eliminada.
  const confirmarEliminacion = useCallback(async () => {
    if (eliminandoCuenta) return
    setEliminandoCuenta(true)
    setErrorEliminar(null)
    try {
      const token = await obtenerToken()
      if (!token) {
        await cerrarSesion()
        enrutador.replace('/')
        return
      }
      await eliminarCuentaParticipanteApi(token)
      // Éxito: borrar token local, limpiar estado de sesión, redirigir.
      await cerrarSesion()
      setConfirmacionAbierta(false)
      enrutador.replace('/')
    } catch (e) {
      if (e instanceof ErrorEliminarCuenta) {
        // Si el backend reporta que la cuenta ya no existe (NO_AUTORIZADO o
        // PARTICIPANTE_NO_ENCONTRADO) la sesión actual ya no es utilizable.
        if (
          e.codigo === 'NO_AUTORIZADO' ||
          e.codigo === 'PARTICIPANTE_NO_ENCONTRADO' ||
          e.cuentaEliminada
        ) {
          await cerrarSesion()
          setConfirmacionAbierta(false)
          enrutador.replace('/')
          return
        }
        setErrorEliminar(e.message)
      } else {
        setErrorEliminar(
          e instanceof Error
            ? e.message
            : 'No fue posible eliminar la cuenta.'
        )
      }
    } finally {
      setEliminandoCuenta(false)
    }
  }, [cerrarSesion, enrutador, eliminandoCuenta])

  // HU11 — al tocar "Eliminar cuenta" en el menú, cerramos el menú y
  // abrimos el modal de confirmación.
  const solicitarEliminacion = () => {
    setMenuAbierto(false)
    setErrorEliminar(null)
    setConfirmacionAbierta(true)
  }

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
      <View style={estilos.encabezado}>
        <View style={estilos.encabezadoCentro}>
          <Text style={estilos.titulo}>UMBRAL</Text>
          <Text style={estilos.subtitulo}>Mi perfil</Text>
        </View>
        {/* HU11 — icono de engranaje. Sólo se muestra al Participante: la
            app es exclusiva de ese rol, pero defendemos contra cualquier
            sesión incorrecta. */}
        {esParticipante && (
          <TouchableOpacity
            accessibilityLabel="Abrir menú de cuenta"
            accessibilityRole="button"
            onPress={abrirMenu}
            style={estilos.botonEngranaje}
            testID="boton-engranaje-perfil"
          >
            <Text style={estilos.botonEngranajeTexto}>⚙</Text>
          </TouchableOpacity>
        )}
      </View>

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

      {/* HU11 — menú flotante al tocar el engranaje. Hoy sólo incluye
          "Eliminar cuenta"; al crecer la app se sumarán más opciones. */}
      <Modal
        visible={menuAbierto}
        transparent
        animationType="fade"
        onRequestClose={cerrarMenu}
      >
        <Pressable style={estilos.menuOverlay} onPress={cerrarMenu}>
          <Pressable style={estilos.menuCaja} onPress={() => {}}>
            <Text style={estilos.menuTitulo}>Cuenta</Text>
            <TouchableOpacity
              accessibilityRole="button"
              onPress={solicitarEliminacion}
              style={estilos.menuItem}
              testID="menu-item-eliminar-cuenta"
            >
              <Text style={estilos.menuItemTextoPeligro}>Eliminar cuenta</Text>
            </TouchableOpacity>
            <TouchableOpacity
              accessibilityRole="button"
              onPress={cerrarMenu}
              style={estilos.menuItem}
            >
              <Text style={estilos.menuItemTexto}>Cancelar</Text>
            </TouchableOpacity>
          </Pressable>
        </Pressable>
      </Modal>

      {/* HU11 — modal de confirmación de eliminación de cuenta. Texto claro
          de que la acción es permanente e irreversible. El botón
          destructivo se desactiva mientras se procesa para evitar doble
          click. Cancelar no llama al backend. */}
      <Modal
        visible={confirmacionAbierta}
        transparent
        animationType="fade"
        onRequestClose={cancelarEliminacion}
      >
        <View style={estilos.modalOverlay}>
          <View style={estilos.modalCaja}>
            <Text style={estilos.modalTitulo}>Eliminar cuenta</Text>
            <Text style={estilos.modalTexto}>
              ¿Estás seguro de que deseas eliminar tu cuenta? Esta acción es
              permanente e irreversible. Perderás todos tus datos y no podrás
              recuperar tu cuenta.
            </Text>

            {errorEliminar && (
              <View style={estilos.cuadroError}>
                <Text style={estilos.cuadroErrorTexto}>{errorEliminar}</Text>
              </View>
            )}

            <TouchableOpacity
              accessibilityRole="button"
              onPress={confirmarEliminacion}
              disabled={eliminandoCuenta}
              style={[
                estilos.botonPeligro,
                eliminandoCuenta && estilos.botonDeshabilitado
              ]}
              testID="boton-confirmar-eliminar-cuenta"
            >
              {eliminandoCuenta ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={estilos.botonPeligroTexto}>Eliminar cuenta</Text>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              accessibilityRole="button"
              onPress={cancelarEliminacion}
              disabled={eliminandoCuenta}
              style={[
                estilos.botonSecundario,
                eliminandoCuenta && estilos.botonDeshabilitado
              ]}
              testID="boton-cancelar-eliminar-cuenta"
            >
              <Text style={estilos.botonSecundarioTexto}>Cancelar</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
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
  },
  // HU11 — encabezado con título centrado y botón de engranaje a la derecha.
  encabezado: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: tema.espacios.lg
  },
  encabezadoCentro: { flex: 1, alignItems: 'center' },
  botonEngranaje: {
    position: 'absolute',
    right: 0,
    top: 0,
    width: 40,
    height: 40,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondoTarjeta,
    alignItems: 'center',
    justifyContent: 'center'
  },
  botonEngranajeTexto: {
    color: tema.colores.texto,
    fontSize: 18,
    fontWeight: '700'
  },
  // HU11 — menú flotante con opciones de cuenta.
  menuOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end'
  },
  menuCaja: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderTopLeftRadius: tema.radios.tarjeta,
    borderTopRightRadius: tema.radios.tarjeta,
    padding: tema.espacios.lg
  },
  menuTitulo: {
    color: tema.colores.textoTenue,
    fontSize: 11,
    letterSpacing: 2,
    textTransform: 'uppercase',
    marginBottom: tema.espacios.md
  },
  menuItem: {
    paddingVertical: tema.espacios.md,
    borderBottomWidth: 1,
    borderBottomColor: tema.colores.bordeTarjeta
  },
  menuItemTexto: {
    color: tema.colores.texto,
    fontSize: 15,
    fontWeight: '600'
  },
  menuItemTextoPeligro: {
    color: tema.colores.error,
    fontSize: 15,
    fontWeight: '700'
  },
  // HU11 — modal de confirmación de eliminación.
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.6)',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tema.espacios.lg
  },
  modalCaja: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    width: '100%'
  },
  modalTitulo: {
    color: tema.colores.texto,
    fontSize: 18,
    fontWeight: '800',
    marginBottom: tema.espacios.sm
  },
  modalTexto: {
    color: tema.colores.textoTenue,
    fontSize: 14,
    lineHeight: 20,
    marginBottom: tema.espacios.md
  },
  botonPeligro: {
    backgroundColor: tema.colores.error,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: 'center',
    marginTop: tema.espacios.sm
  },
  botonPeligroTexto: {
    color: '#fff',
    fontWeight: '800',
    fontSize: 15,
    letterSpacing: 1
  },
  botonDeshabilitado: {
    opacity: 0.6
  }
})
