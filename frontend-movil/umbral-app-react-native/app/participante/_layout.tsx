import { Stack } from 'expo-router'
import RutaProtegidaMovil from '../../autenticacion/RutaProtegidaMovil'
import { useAvisosSesionTiempoReal } from '../../hooks/useAvisosSesionTiempoReal'

export default function LayoutParticipante() {
  useAvisosSesionTiempoReal()

  return (
    <RutaProtegidaMovil>
      <Stack screenOptions={{ headerShown: false }} />
    </RutaProtegidaMovil>
  )
}
