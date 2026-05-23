import { Stack } from 'expo-router'
import { ProveedorAutenticacion } from '../autenticacion/ContextoAutenticacion'

// HU04 — toda la app móvil queda envuelta por el proveedor de autenticación
// para que el login, la redirección automática y las rutas protegidas
// compartan el mismo estado de sesión.
export default function DiseñoRaiz() {
  return (
    <ProveedorAutenticacion>
      <Stack screenOptions={{ headerShown: false }}>
        <Stack.Screen name="index" />
        <Stack.Screen name="registro" />
        <Stack.Screen name="participante/menu" />
        <Stack.Screen name="participante/perfil" />
      </Stack>
    </ProveedorAutenticacion>
  )
}
