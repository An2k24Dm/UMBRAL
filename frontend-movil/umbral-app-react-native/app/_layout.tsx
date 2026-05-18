import { Stack } from 'expo-router'

export default function DiseñoRaiz() {
  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Screen name="index" />
      <Stack.Screen name="participante/sesiones" />
    </Stack>
  )
}
