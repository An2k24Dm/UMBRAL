import { Stack } from 'expo-router'
import RutaProtegidaMovil from '../../autenticacion/RutaProtegidaMovil'

export default function LayoutParticipante() {
  return (
    <RutaProtegidaMovil>
      <Stack screenOptions={{ headerShown: false }} />
    </RutaProtegidaMovil>
  )
}
