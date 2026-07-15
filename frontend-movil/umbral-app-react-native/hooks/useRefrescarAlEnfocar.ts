import { useCallback, useRef } from "react";
import { useFocusEffect } from "expo-router";

// Vuelve a consultar el backend cada vez que la pantalla recupera el foco
// (por ejemplo al volver con el gesto hacia atrás), evitando datos viejos.
// Omite el primer enfoque porque el hook de datos ya hace la carga inicial,
// y usa un ref para no re-disparar cuando cambia la identidad de `refrescar`.
export function useRefrescarAlEnfocar(refrescar: () => void | Promise<void>) {
  const refrescarRef = useRef(refrescar);
  refrescarRef.current = refrescar;

  const primeraVez = useRef(true);

  useFocusEffect(
    useCallback(() => {
      if (primeraVez.current) {
        primeraVez.current = false;
        return;
      }
      void refrescarRef.current();
    }, []),
  );
}
