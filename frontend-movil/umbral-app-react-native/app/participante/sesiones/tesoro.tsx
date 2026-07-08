import { useCallback, useEffect, useRef, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  Modal,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { CameraView, useCameraPermissions } from "expo-camera";
import * as SecureStore from "expo-secure-store";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import {
  enviarEvidenciaTesoro,
  obtenerBusquedaConPistasCompleto,
  type BusquedaConPistas,
  type PistaLiberadaSesion,
} from "../../../servicios/tesoroApi";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
} from "../../../servicios/sesionesTiempoReal";

export function claveEtapaCompletada(sesionId: string, etapaId: string) {
  return `umbral_etapa_done_${sesionId}_${etapaId}`;
}

export default function PantallaTesoro() {
  return (
    <RutaProtegidaMovil>
      <ContenidoTesoro />
    </RutaProtegidaMovil>
  );
}

type EstadoEnvio = "esperando" | "enviando" | "valido" | "invalido";

function ContenidoTesoro() {
  const enrutador = useRouter();
  const { sesion: auth, cerrarSesion } = useAutenticacion();
  const token = auth?.tokenAcceso ?? null;

  const params = useLocalSearchParams<{
    sesionId?: string;
    misionId?: string;
    etapaId?: string;
    busquedaId?: string;
  }>();

  const sesionId = params.sesionId ?? "";
  const misionId = params.misionId ?? "";
  const etapaId = params.etapaId ?? "";
  const busquedaId = params.busquedaId ?? "";

  const [busqueda, setBusqueda] = useState<BusquedaConPistas | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [estadoEnvio, setEstadoEnvio] = useState<EstadoEnvio>("esperando");
  const [puntosGanados, setPuntosGanados] = useState(0);
  const [etapaCompletada, setEtapaCompletada] = useState(false);
  const [enviando, setEnviando] = useState(false);

  // QR scanner
  const [mostrandoCamara, setMostrandoCamara] = useState(false);
  // useRef para bloqueo sincrónico: onBarcodeScanned puede disparar varias veces
  // antes de que React aplique el setState, lo que causaría múltiples POSTs simultáneos.
  const yaEscaneadoRef = useRef(false);
  const [codigoEscaneado, setCodigoEscaneado] = useState("");
  const [permisosCamara, solicitarPermisosCamara] = useCameraPermissions();

  const cargarBusqueda = useCallback(async () => {
    if (!token || !sesionId || !busquedaId) return;
    try {
      const datos = await obtenerBusquedaConPistasCompleto(
        sesionId, misionId, etapaId, busquedaId, token,
      );
      setBusqueda(datos);
      if (datos.yaEnvioEvidencia) {
        setEstadoEnvio("valido");
        // Ya completada previamente — persistir por si SecureStore no lo tiene
        await SecureStore.setItemAsync(claveEtapaCompletada(sesionId, etapaId), "1");
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Error al cargar la búsqueda del tesoro.");
    } finally {
      setCargando(false);
    }
  }, [token, sesionId, misionId, etapaId, busquedaId]);

  useEffect(() => {
    void cargarBusqueda();
  }, [cargarBusqueda]);

  // SignalR: pistas en tiempo real + EtapaCompletada + SesionActualizada
  useEffect(() => {
    if (!token || !sesionId) return;

    let desmontado = false;
    const conexion = crearConexionSesionesTiempoReal(token);

    const manejarPistaLiberada = (evento: {
      sesionId?: string; SesionId?: string;
      etapaId?: string; EtapaId?: string;
      contenido?: string; Contenido?: string;
      pistaId?: string | null; PistaId?: string | null;
      fechaEventoUtc?: string; FechaEventoUtc?: string;
    }) => {
      const sid = (evento.sesionId ?? evento.SesionId ?? "").toLowerCase();
      const eid = (evento.etapaId ?? evento.EtapaId ?? "").toLowerCase();
      if (sid !== sesionId.toLowerCase() || eid !== etapaId.toLowerCase()) return;

      const contenido = evento.contenido ?? evento.Contenido ?? "";
      const pistaId = evento.pistaId ?? evento.PistaId ?? null;
      const fecha = evento.fechaEventoUtc ?? evento.FechaEventoUtc ?? new Date().toISOString();

      if (!desmontado) {
        setBusqueda((prev) => {
          if (!prev) return prev;
          const yaExiste = pistaId
            ? prev.pistasLiberadas.some((p) => p.pistaId === pistaId)
            : false;
          if (yaExiste) return prev;
          const nueva: PistaLiberadaSesion = { pistaId, contenido, fechaLiberacionUtc: fecha };
          return { ...prev, pistasLiberadas: [...prev.pistasLiberadas, nueva] };
        });
      }
    };

    const manejarEtapaCompletada = (evento: {
      sesionId?: string; SesionId?: string;
      etapaId?: string; EtapaId?: string;
    }) => {
      const sid = (evento.sesionId ?? evento.SesionId ?? "").toLowerCase();
      const eid = (evento.etapaId ?? evento.EtapaId ?? "").toLowerCase();
      if (sid === sesionId.toLowerCase() && eid === etapaId.toLowerCase()) {
        if (!desmontado) setEtapaCompletada(true);
      }
    };

    const manejarSesionActualizada = (evento: { estado?: string; Estado?: string }) => {
      const estado = evento.estado ?? evento.Estado ?? "";
      if (estado === "Cancelada" || estado === "Finalizada") {
        Alert.alert(
          "Sesión terminada",
          `La sesión ha sido ${estado.toLowerCase()}.`,
          [{ text: "Aceptar", onPress: () => enrutador.replace("/participante/sesiones") }],
        );
      }
    };

    conexion.on("PistaLiberada", manejarPistaLiberada);
    conexion.on("EtapaCompletada", manejarEtapaCompletada);
    conexion.on("SesionActualizada", manejarSesionActualizada);

    conexion
      .start()
      .then(async () => {
        if (desmontado) { await conexion.stop().catch(() => undefined); return; }
        await conexion.invoke("UnirseASesion", sesionId).catch(() => undefined);
      })
      .catch((e: unknown) => {
        if (esErrorNoAutenticadoTiempoReal(e)) void cerrarSesion();
      });

    return () => {
      desmontado = true;
      conexion.off("PistaLiberada", manejarPistaLiberada);
      conexion.off("EtapaCompletada", manejarEtapaCompletada);
      conexion.off("SesionActualizada", manejarSesionActualizada);
      if (conexion.state !== "Disconnected") {
        conexion.invoke("SalirDeSesion", sesionId).catch(() => undefined).finally(() => {
          conexion.stop().catch(() => undefined);
        });
      }
    };
  }, [token, sesionId, etapaId, cerrarSesion, enrutador]);

  const abrirCamara = async () => {
    if (!permisosCamara?.granted) {
      const resultado = await solicitarPermisosCamara();
      if (!resultado.granted) {
        Alert.alert(
          "Permiso necesario",
          "UMBRAL necesita acceso a la cámara para escanear el QR del tesoro.",
        );
        return;
      }
    }
    yaEscaneadoRef.current = false;
    setMostrandoCamara(true);
  };

  const alEscanearQr = ({ data }: { data: string }) => {
    if (yaEscaneadoRef.current) return;
    yaEscaneadoRef.current = true; // bloqueo sincrónico — evita múltiples POSTs
    setCodigoEscaneado(data);
    setMostrandoCamara(false);
    void enviarConCodigo(data);
  };

  const enviarConCodigo = async (codigo: string) => {
    if (!token || !codigo.trim() || enviando) return;

    setEnviando(true);
    try {
      const resultado = await enviarEvidenciaTesoro(
        sesionId, misionId, etapaId, busquedaId,
        codigo.trim(),
        token,
      );

      setPuntosGanados(resultado.puntosGanados);
      setEstadoEnvio(resultado.esValida ? "valido" : "invalido");

      if (resultado.esValida) {
        // Persistir compleción para el orden secuencial de etapas
        await SecureStore.setItemAsync(claveEtapaCompletada(sesionId, etapaId), "1");
      }

      if (resultado.etapaCompletada) {
        setEtapaCompletada(true);
      }
    } catch (e) {
      Alert.alert("Error", e instanceof Error ? e.message : "Error al enviar evidencia.");
      yaEscaneadoRef.current = false;
      setCodigoEscaneado("");
    } finally {
      setEnviando(false);
    }
  };

  // --- Estados de pantalla ---

  if (cargando) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <ActivityIndicator size="large" color={tema.colores.primario} />
          <Text style={estilos.textoCargando}>Cargando búsqueda del tesoro…</Text>
        </View>
      </PantallaBase>
    );
  }

  if (error) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.textoError}>{error}</Text>
          <TouchableOpacity style={estilos.boton} onPress={() => enrutador.back()}>
            <Text style={estilos.textoBoton}>Volver</Text>
          </TouchableOpacity>
        </View>
      </PantallaBase>
    );
  }

  if (etapaCompletada) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.textoExito}>¡Etapa completada!</Text>
          {puntosGanados > 0 && (
            <View style={estilos.cajaResumen}>
              <Text style={estilos.resumenEtiqueta}>PUNTOS GANADOS</Text>
              <Text style={estilos.resumenPuntos}>{puntosGanados} pts</Text>
            </View>
          )}
          <Text style={estilos.textoInfo}>
            Todos los participantes han encontrado el tesoro. Espera la siguiente etapa.
          </Text>
          <TouchableOpacity style={estilos.boton} onPress={() => enrutador.back()}>
            <Text style={estilos.textoBoton}>Ver sesión</Text>
          </TouchableOpacity>
        </View>
      </PantallaBase>
    );
  }

  return (
    <PantallaBase>
      {/* Modal con cámara para escanear QR */}
      <Modal visible={mostrandoCamara} animationType="slide" onRequestClose={() => setMostrandoCamara(false)}>
        <View style={estilos.modalCamara}>
          <CameraView
            style={estilos.camara}
            facing="back"
            onBarcodeScanned={alEscanearQr}
            barcodeScannerSettings={{ barcodeTypes: ["qr"] }}
          />
          <View style={estilos.overlayQr}>
            <View style={estilos.marcadorQr} />
            <Text style={estilos.textoOverlay}>
              Apunta la cámara al código QR del tesoro
            </Text>
          </View>
          <TouchableOpacity
            style={estilos.botonCerrarCamara}
            onPress={() => setMostrandoCamara(false)}
          >
            <Text style={estilos.textoBotonCerrarCamara}>Cancelar</Text>
          </TouchableOpacity>
        </View>
      </Modal>

      <ScrollView contentContainerStyle={estilos.contenedor} showsVerticalScrollIndicator={false}>
        {/* Encabezado */}
        <View style={estilos.encabezado}>
          <Text style={estilos.titulo}>{busqueda?.nombre ?? "Búsqueda del Tesoro"}</Text>
          {busqueda?.descripcion ? (
            <Text style={estilos.descripcion}>{busqueda.descripcion}</Text>
          ) : null}
          <View style={estilos.fila}>
            <Text style={estilos.metaDato}>Puntaje: {busqueda?.puntajeBase ?? 0} pts</Text>
            <Text style={estilos.metaDato}>
              Tiempo: {Math.round((busqueda?.tiempoSegundos ?? 0) / 60)} min
            </Text>
          </View>
        </View>

        {/* Pistas liberadas */}
        <View style={estilos.seccion}>
          <Text style={estilos.tituloSeccion}>
            PISTAS LIBERADAS ({busqueda?.pistasLiberadas.length ?? 0})
          </Text>
          {(!busqueda || busqueda.pistasLiberadas.length === 0) ? (
            <View style={estilos.tarjetaVacia}>
              <Text style={estilos.textoVacio}>
                El operador aún no ha liberado ninguna pista. Mantente atento.
              </Text>
            </View>
          ) : (
            busqueda.pistasLiberadas.map((pista, idx) => (
              <View key={pista.pistaId ?? `custom-${idx}`} style={estilos.tarjetaPista}>
                <Text style={estilos.numeroPista}>Pista {idx + 1}</Text>
                <Text style={estilos.contenidoPista}>{pista.contenido}</Text>
              </View>
            ))
          )}
        </View>

        {/* Panel de envío */}
        <View style={estilos.seccion}>
          <Text style={estilos.tituloSeccion}>CÓDIGO QR DEL TESORO</Text>

          {estadoEnvio === "valido" ? (
            <View style={estilos.tarjetaExito}>
              <Text style={estilos.textoExitoPanel}>¡Código correcto!</Text>
              <Text style={estilos.textoExitoDetalle}>
                Ganaste {puntosGanados} puntos. Espera a que los demás participantes
                también encuentren el tesoro.
              </Text>
            </View>
          ) : enviando ? (
            <View style={estilos.centradoInline}>
              <ActivityIndicator color={tema.colores.primario} />
              <Text style={estilos.textoEnviando}>Verificando código…</Text>
            </View>
          ) : (
            <>
              {estadoEnvio === "invalido" && (
                <View style={estilos.tarjetaError}>
                  <Text style={estilos.textoErrorPanel}>Código incorrecto.</Text>
                  <Text style={estilos.textoErrorDetalle}>
                    El código escaneado no coincide. Sigue buscando el tesoro.
                  </Text>
                </View>
              )}
              <TouchableOpacity
                style={estilos.botonEscanear}
                onPress={() => void abrirCamara()}
              >
                <Text style={estilos.textoBotonEscanear}>
                  {estadoEnvio === "invalido" ? "Escanear de nuevo" : "Escanear código QR"}
                </Text>
              </TouchableOpacity>
              {codigoEscaneado ? (
                <Text style={estilos.codigoLeido}>
                  Último código leído: {codigoEscaneado}
                </Text>
              ) : null}
            </>
          )}
        </View>

        <TouchableOpacity style={estilos.botonVolver} onPress={() => enrutador.back()}>
          <Text style={estilos.textoBotonVolver}>Volver a la sesión</Text>
        </TouchableOpacity>
      </ScrollView>
    </PantallaBase>
  );
}

const estilos = StyleSheet.create({
  contenedor: {
    padding: 16,
    paddingBottom: 32,
  },
  centrado: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    padding: 24,
    gap: 16,
  },
  centradoInline: {
    alignItems: "center",
    paddingVertical: 24,
    gap: 12,
  },
  encabezado: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.md,
  },
  titulo: {
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
    marginBottom: tema.espacios.xs,
  },
  descripcion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
    marginBottom: tema.espacios.sm,
    lineHeight: 20,
  },
  fila: {
    flexDirection: "row",
    gap: tema.espacios.md,
    marginTop: tema.espacios.xs,
  },
  metaDato: {
    color: tema.colores.primario,
    fontSize: tema.tipografia.tamanos.sm,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  seccion: {
    marginBottom: tema.espacios.md,
  },
  tituloSeccion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: 1,
    textTransform: "uppercase",
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: tema.espacios.sm,
  },
  tarjetaPista: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.sm,
  },
  numeroPista: {
    color: tema.colores.primario,
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.bold,
    textTransform: "uppercase",
    letterSpacing: 0.5,
    marginBottom: 4,
  },
  contenidoPista: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    lineHeight: 22,
  },
  tarjetaVacia: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    alignItems: "center",
  },
  textoVacio: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
    lineHeight: 20,
  },
  botonEscanear: {
    backgroundColor: "#d97706",
    paddingVertical: tema.espacios.md + 4,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
  },
  textoBotonEscanear: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  textoEnviando: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
  },
  codigoLeido: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    textAlign: "center",
    marginTop: tema.espacios.xs,
  },
  tarjetaExito: {
    backgroundColor: "#f0fdf4",
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: "#22c55e",
    padding: tema.espacios.lg,
  },
  textoExitoPanel: {
    color: "#16a34a",
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: 4,
  },
  textoExitoDetalle: {
    color: "#15803d",
    fontSize: tema.tipografia.tamanos.sm,
    lineHeight: 20,
  },
  tarjetaError: {
    backgroundColor: tema.colores.errorSuave,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.error,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.sm,
  },
  textoErrorPanel: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: 2,
  },
  textoErrorDetalle: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.sm,
  },
  botonVolver: {
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondoTarjeta,
    marginTop: tema.espacios.sm,
  },
  textoBotonVolver: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  // Cámara / Modal
  modalCamara: {
    flex: 1,
    backgroundColor: "#000",
  },
  camara: {
    flex: 1,
  },
  overlayQr: {
    ...StyleSheet.absoluteFillObject,
    alignItems: "center",
    justifyContent: "center",
    pointerEvents: "none",
  },
  marcadorQr: {
    width: 220,
    height: 220,
    borderWidth: 3,
    borderColor: "#d97706",
    borderRadius: 12,
    backgroundColor: "transparent",
  },
  textoOverlay: {
    color: "#fff",
    fontSize: 14,
    marginTop: 16,
    textAlign: "center",
    paddingHorizontal: 32,
  },
  botonCerrarCamara: {
    position: "absolute",
    bottom: 48,
    alignSelf: "center",
    backgroundColor: "rgba(0,0,0,0.7)",
    paddingHorizontal: 32,
    paddingVertical: 14,
    borderRadius: 30,
    borderWidth: 1,
    borderColor: "#fff",
  },
  textoBotonCerrarCamara: {
    color: "#fff",
    fontSize: 16,
    fontWeight: "600",
  },
  // Estados de resultado
  textoCargando: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
    fontSize: tema.tipografia.tamanos.md,
  },
  textoError: {
    color: tema.colores.error,
    fontSize: 16,
    textAlign: "center",
  },
  textoExito: {
    color: "#22c55e",
    fontSize: 24,
    fontWeight: "700",
    textAlign: "center",
  },
  textoInfo: {
    color: tema.colores.textoTenue,
    fontSize: 15,
    textAlign: "center",
    lineHeight: 22,
  },
  cajaResumen: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    borderRadius: tema.radios.tarjeta,
    padding: 20,
    alignItems: "center",
    marginVertical: 16,
    minWidth: 180,
  },
  resumenEtiqueta: {
    fontSize: 11,
    color: tema.colores.textoTenue,
    letterSpacing: 1,
    textTransform: "uppercase",
  },
  resumenPuntos: {
    fontSize: 40,
    fontWeight: "700",
    color: tema.colores.primario,
    marginTop: 4,
  },
  boton: {
    backgroundColor: tema.colores.primario,
    borderRadius: tema.radios.boton,
    padding: 14,
    minWidth: 140,
    alignItems: "center",
    marginTop: 8,
  },
  textoBoton: {
    color: "#ffffff",
    fontSize: 15,
    fontWeight: "600",
  },
});
