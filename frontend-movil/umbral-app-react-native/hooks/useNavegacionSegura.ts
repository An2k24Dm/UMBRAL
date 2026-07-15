import { useCallback, useRef } from "react";

// Evita doble navegación por doble tap: ignora taps repetidos durante una
// breve ventana. El guard se reinicia solo, así el botón vuelve a funcionar
// al regresar a la pantalla.
export function useNavegacionSegura(ventanaMs = 700) {
  const navegando = useRef(false);

  return useCallback(
    (accion: () => void) => {
      if (navegando.current) return;
      navegando.current = true;
      accion();
      setTimeout(() => {
        navegando.current = false;
      }, ventanaMs);
    },
    [ventanaMs],
  );
}
