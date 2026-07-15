import { Stack } from 'expo-router'
import RutaProtegidaMovil from '../../autenticacion/RutaProtegidaMovil'
import AvisosTiempoRealParticipante from '../../componentes/AvisosTiempoRealParticipante'

export default function LayoutParticipante() {
  // Los avisos dirigidos de expulsión se suscriben una sola vez, dentro de la
  // zona autenticada (RutaProtegidaMovil garantiza sesión de Participante).
  // El Stack se renderiza PRIMERO y el overlay global DESPUÉS, para que el banner
  // (posición absoluta) quede pintado por encima del NativeStack en toda el área
  // del participante. Sigue usando la misma conexión SignalR (un solo hook global).
  return (
    <RutaProtegidaMovil>
      <Stack screenOptions={{ headerShown: false }} />
      <AvisosTiempoRealParticipante />
    </RutaProtegidaMovil>
  )
}
