import type { ComponentProps, ReactNode } from 'react'
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { tema } from '../estilos/tema'

interface Props {
  children: ReactNode
  scrollable?: boolean
  padding?: boolean
  // Pull-to-refresh para pantallas que consultan datos del backend. Solo
  // aplica cuando scrollable es true (el RefreshControl vive en el ScrollView).
  refreshControl?: ComponentProps<typeof ScrollView>['refreshControl']
}

export function PantallaBase({
  children,
  scrollable = true,
  padding = true,
  refreshControl,
}: Props) {
  const contenido = padding ? (
    <View style={estilos.padding}>{children}</View>
  ) : children

  if (!scrollable) {
    return (
      <SafeAreaView style={estilos.safeArea}>
        {contenido}
      </SafeAreaView>
    )
  }

  return (
    <SafeAreaView style={estilos.safeArea}>
      <KeyboardAvoidingView
        style={estilos.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <ScrollView
          style={estilos.scroll}
          contentContainerStyle={estilos.scrollContenido}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
          refreshControl={refreshControl}
        >
          {contenido}
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  )
}

const estilos = StyleSheet.create({
  safeArea: { flex: 1, backgroundColor: tema.colores.fondo },
  flex: { flex: 1 },
  scroll: { flex: 1 },
  scrollContenido: { flexGrow: 1 },
  padding: { padding: tema.espacios.lg },
})
