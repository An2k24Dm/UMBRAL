import { StyleSheet, Text, TouchableOpacity, View } from 'react-native'
import { useRouter } from 'expo-router'
import { useAutenticacion } from '../../autenticacion/ContextoAutenticacion'
import { BotonMovil } from '../../componentes/BotonMovil'
import { PantallaBase } from '../../componentes/PantallaBase'
import RutaProtegidaMovil from '../../componentes/RutaProtegidaMovil'
import { TarjetaMovil } from '../../componentes/TarjetaMovil'
import { tema } from '../../estilos/tema'

// Menú principal de la app móvil del Participante. Hub de navegación tras el login.
export default function PantallaMenuParticipante() {
  return (
    <RutaProtegidaMovil>
      <ContenidoMenu />
    </RutaProtegidaMovil>
  )
}

interface OpcionMenu {
  clave: string
  titulo: string
  descripcion: string
  ruta: '/participante/perfil'
}

const OPCIONES: OpcionMenu[] = [
  {
    clave: 'perfil',
    titulo: 'Mi perfil',
    descripcion: 'Consulta tus datos personales de Participante.',
    ruta: '/participante/perfil',
  },
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
    <PantallaBase>
      <View style={estilos.marca}>
        <Text style={estilos.marcaTitulo}>UMBRAL</Text>
        <Text style={estilos.marcaSubtitulo}>Menú del Participante</Text>
      </View>

      <TarjetaMovil>
        <Text style={estilos.bienvenidaSaludo}>Bienvenido,</Text>
        <Text style={estilos.bienvenidaNombre}>{nombreVisible}</Text>
        <Text style={estilos.bienvenidaTexto}>
          Prepárate para entrar a sesiones, trivias y búsquedas del tesoro.
        </Text>
      </TarjetaMovil>

      {OPCIONES.map((opcion) => (
        <TouchableOpacity
          key={opcion.clave}
          activeOpacity={0.7}
          onPress={() => alSeleccionar(opcion)}
          style={estilos.tarjetaOpcion}
        >
          <View style={estilos.opcionIndicador} />
          <View style={estilos.opcionCuerpo}>
            <Text style={estilos.opcionTitulo}>{opcion.titulo}</Text>
            <Text style={estilos.opcionDescripcion}>{opcion.descripcion}</Text>
          </View>
          <Text style={estilos.opcionFlecha}>›</Text>
        </TouchableOpacity>
      ))}

      <BotonMovil
        titulo="Cerrar sesión"
        onPress={alCerrarSesion}
        variante="secundario"
      />
    </PantallaBase>
  )
}

const estilos = StyleSheet.create({
  marca: {
    alignItems: 'center',
    marginBottom: tema.espacios.lg,
    paddingTop: tema.espacios.md,
  },
  marcaTitulo: {
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
    letterSpacing: tema.tipografia.espaciadoLetra.md,
  },
  marcaSubtitulo: {
    fontSize: tema.tipografia.tamanos.xs,
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: 'uppercase',
  },
  bienvenidaSaludo: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.base,
  },
  bienvenidaNombre: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
    marginTop: tema.espacios.xs,
  },
  bienvenidaTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.base,
    marginTop: tema.espacios.sm,
    lineHeight: 20,
  },
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
