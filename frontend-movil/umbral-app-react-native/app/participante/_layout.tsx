import { Stack } from 'expo-router'
import RutaProtegidaMovil from '../../autenticacion/RutaProtegidaMovil'
import AvisosTiempoRealParticipante from '../../componentes/AvisosTiempoRealParticipante'

export default function LayoutParticipante() {
  // Los avisos dirigidos de expulsión se suscriben una sola vez, dentro de la
  // zona autenticada (RutaProtegidaMovil garantiza sesión de Participante).
  return (
    <RutaProtegidaMovil>
      <AvisosTiempoRealParticipante />
      <Stack screenOptions={{ headerShown: false }} />
    </RutaProtegidaMovil>
  )
}
