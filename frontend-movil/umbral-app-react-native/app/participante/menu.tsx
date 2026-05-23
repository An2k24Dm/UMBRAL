import { ScrollView, StyleSheet, Text, TouchableOpacity, View } from 'react-native'
import { useRouter } from 'expo-router'
import { useAutenticacion } from '../../autenticacion/ContextoAutenticacion'
import RutaProtegidaMovil from '../../componentes/RutaProtegidaMovil'
import { tema } from '../../estilos/tema'

// Menú principal de la app móvil del Participante. Funciona como hub de
// navegación tras el login. Solo se listan aquí los módulos que cuentan con
// pantalla implementada; los módulos futuros (sesiones, trivias, búsqueda
// del tesoro, ranking) se agregarán cuando dispongan de funcionalidad real.
export default function PantallaMenuParticipante() {
  return (
    <RutaProtegidaMovil>
      <ContenidoMenu />
    </RutaProtegidaMovil>
  )
}

interface OpcionMenu {
  clave: string
  icono: string
  titulo: string
  descripcion: string
  ruta: '/participante/perfil'
}

const OPCIONES: OpcionMenu[] = [
  {
    clave: 'perfil',
    icono: '◆',
    titulo: 'Mi perfil',
    descripcion: 'Consulta tus datos personales de Participante.',
    ruta: '/participante/perfil'
  }
]

function ContenidoMenu() {
  const { usuario, cerrarSesion } = useAutenticacion()
  const enrutador = useRouter()

  const nombreVisible =
    usuario?.nombre?.trim() || usuario?.nombreUsuario || 'Participante'

  const alSeleccionar = (opcion: OpcionMenu) => enrutador.push(opcion.ruta)

  const alCerrarSesion = async () => {
    await cerrarSesion()
    enrutador.replace('/')
  }

  return (
    <ScrollView
      style={estilos.contenedor}
      contentContainerStyle={estilos.contenido}
    >
      <Text style={estilos.titulo}>UMBRAL</Text>
      <Text style={estilos.subtitulo}>Menú del Participante</Text>

      <View style={estilos.tarjetaBienvenida}>
        <Text style={estilos.bienvenidaSaludo}>Bienvenido,</Text>
        <Text style={estilos.bienvenidaNombre}>{nombreVisible}</Text>
        <Text style={estilos.bienvenidaTexto}>
          Prepárate para entrar a sesiones, trivias y búsquedas del tesoro.
        </Text>
      </View>

      {OPCIONES.map((opcion) => (
        <TouchableOpacity
          key={opcion.clave}
          activeOpacity={0.7}
          onPress={() => alSeleccionar(opcion)}
          style={estilos.tarjetaOpcion}
        >
          <View style={estilos.iconoCirculo}>
            <Text style={estilos.iconoTexto}>{opcion.icono}</Text>
          </View>
          <View style={estilos.opcionCuerpo}>
            <Text style={estilos.opcionTitulo}>{opcion.titulo}</Text>
            <Text style={estilos.opcionDescripcion}>{opcion.descripcion}</Text>
          </View>
        </TouchableOpacity>
      ))}

      <TouchableOpacity style={estilos.botonCerrar} onPress={alCerrarSesion}>
        <Text style={estilos.botonCerrarTexto}>Cerrar sesión</Text>
      </TouchableOpacity>
    </ScrollView>
  )
}

const estilos = StyleSheet.create({
  contenedor: { flex: 1, backgroundColor: tema.colores.fondo },
  contenido: {
    padding: tema.espacios.lg,
    paddingTop: tema.espacios.xl * 2,
    paddingBottom: tema.espacios.xl * 2
  },
  titulo: {
    fontSize: 28,
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
  tarjetaBienvenida: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.lg
  },
  bienvenidaSaludo: {
    color: tema.colores.textoTenue,
    fontSize: 13
  },
  bienvenidaNombre: {
    color: tema.colores.texto,
    fontSize: 22,
    fontWeight: '800',
    marginTop: tema.espacios.xs
  },
  bienvenidaTexto: {
    color: tema.colores.textoTenue,
    fontSize: 13,
    marginTop: tema.espacios.sm,
    lineHeight: 18
  },
  tarjetaOpcion: {
    flexDirection: 'row',
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
    alignItems: 'center'
  },
  iconoCirculo: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: tema.colores.entradaFondo,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: tema.espacios.md
  },
  iconoTexto: {
    color: tema.colores.primario,
    fontSize: 18,
    fontWeight: '800'
  },
  opcionCuerpo: { flex: 1 },
  opcionTitulo: {
    color: tema.colores.texto,
    fontWeight: '700',
    fontSize: 15
  },
  opcionDescripcion: {
    color: tema.colores.textoTenue,
    fontSize: 12,
    marginTop: tema.espacios.xs,
    lineHeight: 16
  },
  botonCerrar: {
    marginTop: tema.espacios.lg,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondoTarjeta
  },
  botonCerrarTexto: {
    color: tema.colores.texto,
    fontWeight: '700',
    fontSize: 14
  }
})
