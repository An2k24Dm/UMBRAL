import { StyleSheet, Text, View } from 'react-native'
import { useRouter } from 'expo-router'
import { useAutenticacion } from '../../autenticacion/ContextoAutenticacion'
import { BotonMovil } from '../../componentes/BotonMovil'
import { PantallaBase } from '../../componentes/PantallaBase'
import { TarjetaMovil } from '../../componentes/TarjetaMovil'
import { TarjetaOpcionMenu } from '../../componentes/participante/TarjetaOpcionMenu'
import { tema } from '../../estilos/tema'

interface OpcionMenu {
  clave: string
  titulo: string
  descripcion: string
  ruta: '/participante/perfil' | '/participante/sesiones' | '/participante/sesiones/finalizadas' | '/participante/ranking'
}

const OPCIONES: OpcionMenu[] = [
  {
    clave: 'perfil',
    titulo: 'Mi perfil',
    descripcion: 'Consulta tus datos personales de Participante.',
    ruta: '/participante/perfil',
  },
  {
    clave: 'sesiones',
    titulo: 'Sesiones',
    descripcion: 'Consulta las sesiones disponibles para participar.',
    ruta: '/participante/sesiones',
  },
  {
    clave: 'finalizadas',
    titulo: 'Sesiones finalizadas',
    descripcion: 'Revisa el historial de tus últimas 20 participaciones.',
    ruta: '/participante/sesiones/finalizadas',
  },
  {
    clave: 'ranking-global',
    titulo: 'Ranking global',
    descripcion: 'Consulta la clasificacion global de participantes.',
    ruta: '/participante/ranking',
  },
]

export default function PantallaMenuParticipante() {
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
        <TarjetaOpcionMenu
          key={opcion.clave}
          titulo={opcion.titulo}
          descripcion={opcion.descripcion}
          alPresionar={() => alSeleccionar(opcion)}
        />
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
})

