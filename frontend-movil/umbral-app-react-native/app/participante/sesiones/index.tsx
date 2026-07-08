import { useCallback, useEffect, useState } from "react";
import {
  ActivityIndicator,
  RefreshControl,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useRouter } from "expo-router";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { FiltrosSesionesMovil } from "../../../componentes/sesiones/FiltrosSesionesMovil";
import { TarjetaSesionMovil } from "../../../componentes/sesiones/TarjetaSesionMovil";
import { tema } from "../../../estilos/tema";
import { useNavegacionSegura } from "../../../hooks/useNavegacionSegura";
import { useRefrescarAlEnfocar } from "../../../hooks/useRefrescarAlEnfocar";
import { useSesionesDisponibles } from "../../../hooks/useSesionesDisponibles";
import { useListadoSesionesTiempoReal } from "../../../hooks/useListadoSesionesTiempoReal";
import type { FiltroModoSesion } from "../../../tipos/sesiones";

// HU — Listado de sesiones disponibles para el Participante.
// El backend ya filtra estados (Programada/EnPreparacion/Activa), pero el
// frontend NO añade ni acepta acciones administrativas: solo consulta.
export default function PantallaListadoSesionesParticipante() {
  return (
    <RutaProtegidaMovil>
      <ContenidoListado />
    </RutaProtegidaMovil>
  );
}

function ContenidoListado() {
  const enrutador = useRouter();
  const { cerrarSesion } = useAutenticacion();

  const [busqueda, setBusqueda] = useState<string>("");
  const [modo, setModo] = useState<FiltroModoSesion>("Todas");

  const { sesiones, cargando, error, sesionExpirada, refrescar } =
    useSesionesDisponibles({ busqueda, modo });

  const navegarSeguro = useNavegacionSegura();
  // Refresca al volver a esta pantalla (gesto atrás) con datos frescos.
  useRefrescarAlEnfocar(refrescar);
  // HU52 — refresco en vivo del listado cuando cambia el estado o el conteo de
  // una sesión (SignalR); si no conecta, sigue el pull-to-refresh por HTTP.
  useListadoSesionesTiempoReal({ onListadoActualizado: refrescar });

  const [refrescando, setRefrescando] = useState(false);
  const alRefrescar = useCallback(async () => {
    setRefrescando(true);
    try {
      await refrescar();
    } finally {
      setRefrescando(false);
    }
  }, [refrescar]);

  // Si el backend respondió 401, cerramos sesión y volvemos al login.
  // 403 NO debe cerrar sesión (la app móvil ya filtra por rol al iniciar).
  useEffect(() => {
    if (sesionExpirada) {
      cerrarSesion().finally(() => enrutador.replace("/"));
    }
  }, [sesionExpirada, cerrarSesion, enrutador]);

  const volverAlMenu = () => enrutador.replace("/participante/menu");

  return (
    <PantallaBase
      refreshControl={
        <RefreshControl
          refreshing={refrescando}
          onRefresh={alRefrescar}
          tintColor={tema.colores.primario}
          colors={[tema.colores.primario]}
        />
      }
    >
      <View style={estilos.encabezado}>
        <Text style={estilos.titulo}>UMBRAL</Text>
        <Text style={estilos.subtitulo}>Sesiones disponibles</Text>
      </View>

      <TouchableOpacity
        style={estilos.botonPrimario}
        onPress={() =>
          navegarSeguro(() =>
            enrutador.push("/participante/sesiones/ingresar-codigo"),
          )
        }
        accessibilityRole="button"
      >
        <Text style={estilos.botonPrimarioTexto}>Ingresar con código</Text>
      </TouchableOpacity>

      <FiltrosSesionesMovil
        busqueda={busqueda}
        alCambiarBusqueda={setBusqueda}
        modo={modo}
        alCambiarModo={setModo}
      />

      {cargando && (
        <View style={estilos.contenedorEstado}>
          <ActivityIndicator color={tema.colores.primario} size="large" />
          <Text style={estilos.textoEstado}>Cargando sesiones…</Text>
        </View>
      )}

      {!cargando && error && !sesionExpirada && (
        <View>
          <View style={estilos.cuadroError}>
            <Text style={estilos.cuadroErrorTexto}>{error}</Text>
          </View>
          <TouchableOpacity style={estilos.botonPrimario} onPress={refrescar}>
            <Text style={estilos.botonPrimarioTexto}>Reintentar</Text>
          </TouchableOpacity>
        </View>
      )}

      {!cargando && !error && sesiones.length === 0 && (
        <View style={estilos.cuadroVacio}>
          <Text style={estilos.cuadroVacioTitulo}>
            No hay sesiones disponibles
          </Text>
          <Text style={estilos.cuadroVacioTexto}>
            Por ahora no encontramos sesiones que coincidan con tu búsqueda o
            filtro. Probá ajustarlos o volvé más tarde.
          </Text>
        </View>
      )}

      {!cargando && !error && sesiones.length > 0 && (
        <View style={estilos.lista}>
          {sesiones.map((sesion) => (
            <TarjetaSesionMovil
              key={sesion.id}
              sesion={sesion}
              alPresionar={() =>
                navegarSeguro(() =>
                  enrutador.push(`/participante/sesiones/${sesion.id}`),
                )
              }
            />
          ))}
        </View>
      )}

      <TouchableOpacity
        style={estilos.botonSecundario}
        onPress={volverAlMenu}
        accessibilityRole="button"
      >
        <Text style={estilos.botonSecundarioTexto}>Volver al menú</Text>
      </TouchableOpacity>
    </PantallaBase>
  );
}

const estilos = StyleSheet.create({
  encabezado: {
    alignItems: "center",
    marginBottom: tema.espacios.lg,
    paddingTop: tema.espacios.md,
  },
  titulo: {
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
    letterSpacing: tema.tipografia.espaciadoLetra.md,
  },
  subtitulo: {
    fontSize: tema.tipografia.tamanos.xs,
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: "uppercase",
  },
  lista: { marginTop: tema.espacios.sm },
  contenedorEstado: {
    alignItems: "center",
    justifyContent: "center",
    padding: tema.espacios.xl,
  },
  textoEstado: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
    fontSize: tema.tipografia.tamanos.md,
  },
  cuadroError: {
    backgroundColor: tema.colores.errorSuave,
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  cuadroErrorTexto: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
  },
  cuadroVacio: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    alignItems: "center",
    marginVertical: tema.espacios.md,
  },
  cuadroVacioTitulo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: tema.espacios.sm,
  },
  cuadroVacioTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
    lineHeight: 20,
  },
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
  },
  botonPrimarioTexto: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  botonSecundario: {
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.md,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondoTarjeta,
  },
  botonSecundarioTexto: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
});
