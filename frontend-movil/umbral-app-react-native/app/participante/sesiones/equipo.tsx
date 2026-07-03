import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  Animated,
  Modal,
  PanResponder,
  RefreshControl,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { ModalContrasenaEquipo } from "../../../componentes/sesiones/ModalContrasenaEquipo";
import { tema } from "../../../estilos/tema";
import { useDetalleEquipoSesion } from "../../../hooks/useDetalleEquipoSesion";
import { useEliminarEquipo } from "../../../hooks/useEliminarEquipo";
import { useExpulsarParticipanteEquipo } from "../../../hooks/useExpulsarParticipanteEquipo";
import { useIngresarEquipo } from "../../../hooks/useIngresarEquipo";
import { useNavegacionSegura } from "../../../hooks/useNavegacionSegura";
import { useRefrescarAlEnfocar } from "../../../hooks/useRefrescarAlEnfocar";
import { useSesionesTiempoReal } from "../../../hooks/useSesionesTiempoReal";
import type { IntegranteEquipo } from "../../../tipos/equipos";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";

// HU43/HU47 — Detalle real de un equipo de la sesión, con ingreso al equipo
// (directo si es público, con contraseña si es privado).
export default function PantallaDetalleEquipo() {
  return (
    <RutaProtegidaMovil>
      <Contenido />
    </RutaProtegidaMovil>
  );
}

function Contenido() {
  const enrutador = useRouter();
  const { cerrarSesion } = useAutenticacion();
  const parametros = useLocalSearchParams<{ sesionId?: string; equipoId?: string }>();
  const sesionId = parametros.sesionId ?? "";
  const equipoId = parametros.equipoId ?? "";

  const { equipo, cargando, error, sesionExpirada, refrescar } =
    useDetalleEquipoSesion(sesionId, equipoId);

  const navegarSeguro = useNavegacionSegura();
  useRefrescarAlEnfocar(refrescar);
  // El aviso dirigido de expulsión de equipo lo maneja de forma global
  // useAvisosSesionTiempoReal; aquí solo refrescamos por eventos de grupo.
  useSesionesTiempoReal({
    sesionId,
    equipoId,
    onEquiposSesionActualizados: refrescar,
    onEquipoActualizado: refrescar,
  });

  const [refrescando, setRefrescando] = useState(false);
  const alRefrescar = useCallback(async () => {
    setRefrescando(true);
    try {
      await refrescar();
    } finally {
      setRefrescando(false);
    }
  }, [refrescar]);

  const [mostrarConfirmacion, setMostrarConfirmacion] = useState(false);
  const {
    eliminando,
    error: errorEliminar,
    noExiste,
    sesionExpirada: sesionExpiradaEliminar,
    eliminar,
  } = useEliminarEquipo(sesionId, equipoId);

  // HU47 — Ingreso real al equipo.
  const {
    ingresando,
    error: errorIngreso,
    sesionExpirada: sesionExpiradaIngreso,
    ingresarEquipo,
    limpiarError,
  } = useIngresarEquipo();
  const [mostrarContrasena, setMostrarContrasena] = useState(false);

  // HU45 — El líder expulsa integrantes deslizando su tarjeta a la izquierda.
  const {
    expulsando: expulsandoParticipante,
    error: errorExpulsarParticipante,
    sesionExpirada: sesionExpiradaExpulsar,
    expulsarParticipanteEquipo,
    limpiarError: limpiarErrorExpulsar,
  } = useExpulsarParticipanteEquipo();

  useEffect(() => {
    if (
      sesionExpirada ||
      sesionExpiradaEliminar ||
      sesionExpiradaIngreso ||
      sesionExpiradaExpulsar
    )
      cerrarSesion().finally(() => enrutador.replace("/"));
  }, [
    sesionExpirada,
    sesionExpiradaEliminar,
    sesionExpiradaIngreso,
    sesionExpiradaExpulsar,
    cerrarSesion,
    enrutador,
  ]);

  const confirmarExpulsion = useCallback(
    async (participanteSesionId: string) => {
      const ok = await expulsarParticipanteEquipo(
        sesionId, equipoId, participanteSesionId);
      if (ok) {
        Alert.alert(
          "Participante expulsado",
          "El participante fue expulsado del equipo.",
        );
        await refrescar();
      }
      // Los errores (409/403/404) quedan en errorExpulsarParticipante.
    },
    [expulsarParticipanteEquipo, sesionId, equipoId, refrescar],
  );

  const solicitarExpulsion = useCallback(
    (participante: IntegranteEquipo) => {
      if (expulsandoParticipante) return;
      limpiarErrorExpulsar();
      Alert.alert(
        "Expulsar participante",
        "¿Seguro que deseas expulsar a este participante del equipo?",
        [
          { text: "Cancelar", style: "cancel" },
          {
            text: "Expulsar",
            style: "destructive",
            onPress: () => void confirmarExpulsion(participante.participanteSesionId),
          },
        ],
      );
    },
    [expulsandoParticipante, limpiarErrorExpulsar, confirmarExpulsion],
  );

  const volverASesion = () =>
    enrutador.replace(`/participante/sesiones/${sesionId}`);

  const confirmarEliminar = async () => {
    const ok = await eliminar();
    setMostrarConfirmacion(false);
    if (ok) {
      Alert.alert("Equipo eliminado", "Equipo eliminado correctamente.");
      volverASesion();
    } else if (noExiste) {
      // El equipo ya no existe: volvemos al detalle de la sesión.
      volverASesion();
    }
    // Otros errores (403/409) quedan en errorEliminar y se muestran en línea.
  };

  // HU47 — Ingreso real: directo si el equipo es público; con contraseña si
  // es privado. Tras el éxito el usuario permanece en el detalle para ver
  // toda la información asociada al equipo.
  const alIngresoExitoso = async () => {
    setMostrarContrasena(false);
    Alert.alert("Equipo", "Ingresaste al equipo correctamente.");
    await refrescar();
  };

  const ingresarAlEquipo = async () => {
    if (!equipo) return;
    limpiarError();
    if (equipo.tipo === "Privado") {
      setMostrarContrasena(true);
      return;
    }
    const respuesta = await ingresarEquipo(sesionId, equipoId);
    if (respuesta) await alIngresoExitoso();
  };

  const ingresarConContrasena = async (contrasena: string) => {
    const respuesta = await ingresarEquipo(sesionId, equipoId, contrasena);
    if (respuesta) await alIngresoExitoso();
    // Si falló, el modal queda abierto mostrando el error.
  };

  // HU41: solo el líder puede editar el equipo.
  const editarEquipo = () => {
    if (!equipo) return;
    navegarSeguro(() =>
      enrutador.push(
        `/participante/sesiones/editar-equipo?sesionId=${sesionId}` +
          `&equipoId=${equipoId}` +
          `&nombre=${encodeURIComponent(equipo.nombre)}` +
          `&tipo=${equipo.tipo}`,
      ),
    );
  };

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
        <Text style={estilos.titulo}>Detalle de equipo</Text>
      </View>

      {cargando && (
        <View style={estilos.contenedorEstado}>
          <ActivityIndicator color={tema.colores.primario} size="large" />
          <Text style={estilos.textoEstado}>Cargando equipo…</Text>
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

      {!cargando && !error && equipo && (
        <View>
          <View style={estilos.tarjeta}>
            <View style={estilos.filaTitulo}>
              <Text style={estilos.nombreEquipo}>{equipo.nombre}</Text>
              <View style={estilos.badges}>
                {equipo.esMiEquipo && (
                  <Text style={[estilos.badge, estilos.badgePrimario]}>Tu equipo</Text>
                )}
                {equipo.soyLider && (
                  <Text style={[estilos.badge, estilos.badgePrimario]}>Eres líder</Text>
                )}
              </View>
            </View>
            <Text style={estilos.metaLinea}>
              {equipo.tipo === "Privado" ? "Privado" : "Público"}
            </Text>
            <Text style={estilos.metaLinea}>Puntaje: {equipo.puntaje}</Text>
            <Text style={estilos.metaLinea}>
              Integrantes: {equipo.cantidadParticipantes} / {equipo.capacidadMaxima}
            </Text>
          </View>

          <Text style={estilos.tituloSeccion}>PARTICIPANTES</Text>
          {errorExpulsarParticipante ? (
            <View style={estilos.cuadroError}>
              <Text style={estilos.cuadroErrorTexto}>
                {errorExpulsarParticipante}
              </Text>
            </View>
          ) : null}
          {equipo.participantes.length === 0 ? (
            <View style={estilos.tarjeta}>
              <Text style={estilos.metaLinea}>Este equipo no tiene integrantes.</Text>
            </View>
          ) : (
            equipo.participantes.map((p) => (
              <FilaParticipante
                key={p.participanteSesionId}
                participante={p}
                // HU45 — Solo el líder desliza, nunca sobre el líder (él mismo).
                expulsable={equipo.soyLider && !p.esLider && !expulsandoParticipante}
                onExpulsar={() => solicitarExpulsion(p)}
              />
            ))
          )}

          {equipo.soyLider && (
            <TouchableOpacity
              style={estilos.botonPrimario}
              onPress={editarEquipo}
              accessibilityRole="button"
            >
              <Text style={estilos.botonPrimarioTexto}>Editar equipo</Text>
            </TouchableOpacity>
          )}

          {equipo.soyLider && (
            <TouchableOpacity
              style={estilos.botonPeligro}
              onPress={() => setMostrarConfirmacion(true)}
              accessibilityRole="button"
            >
              <Text style={estilos.botonPeligroTexto}>Eliminar equipo</Text>
            </TouchableOpacity>
          )}

          {errorEliminar && !noExiste ? (
            <View style={estilos.cuadroError}>
              <Text style={estilos.cuadroErrorTexto}>{errorEliminar}</Text>
            </View>
          ) : null}

          {/* HU47 — Ingresar solo si no es mi equipo y hay cupos. */}
          {!equipo.esMiEquipo && equipo.estaLleno && (
            <View style={estilos.tarjeta}>
              <Text style={estilos.metaLinea}>
                El equipo no tiene cupos disponibles.
              </Text>
            </View>
          )}

          {!equipo.esMiEquipo && !equipo.estaLleno && (
            <View>
              {errorIngreso && !mostrarContrasena ? (
                <View style={estilos.cuadroError}>
                  <Text style={estilos.cuadroErrorTexto}>{errorIngreso}</Text>
                </View>
              ) : null}
              <TouchableOpacity
                style={[
                  estilos.botonPrimario,
                  ingresando && estilos.botonDeshabilitado,
                ]}
                onPress={() => void ingresarAlEquipo()}
                disabled={ingresando}
                accessibilityRole="button"
              >
                {ingresando && !mostrarContrasena ? (
                  <ActivityIndicator color={tema.colores.textoBlanco} />
                ) : (
                  <Text style={estilos.botonPrimarioTexto}>Ingresar al equipo</Text>
                )}
              </TouchableOpacity>
            </View>
          )}
        </View>
      )}

      {/* HU47 — Contraseña para equipos privados. */}
      <ModalContrasenaEquipo
        visible={mostrarContrasena}
        nombreEquipo={equipo?.nombre ?? ""}
        procesando={ingresando}
        error={errorIngreso}
        onIngresar={(contrasena) => void ingresarConContrasena(contrasena)}
        onCancelar={() => {
          setMostrarContrasena(false);
          limpiarError();
        }}
      />

      <TouchableOpacity
        style={estilos.botonSecundario}
        onPress={() => enrutador.back()}
        accessibilityRole="button"
      >
        <Text style={estilos.botonSecundarioTexto}>Volver</Text>
      </TouchableOpacity>

      <Modal
        visible={mostrarConfirmacion}
        transparent
        animationType="fade"
        onRequestClose={() => !eliminando && setMostrarConfirmacion(false)}
      >
        <View style={estilos.modalFondo}>
          <View style={estilos.modalTarjeta}>
            <Text style={estilos.modalTitulo}>Eliminar equipo</Text>
            <Text style={estilos.modalMensaje}>
              ¿Seguro que deseas eliminar este equipo? Todos los integrantes
              saldrán del equipo y podrán volver a ingresar a la sesión.
            </Text>
            <TouchableOpacity
              style={[estilos.botonPeligro, eliminando && estilos.botonDeshabilitado]}
              onPress={confirmarEliminar}
              disabled={eliminando}
              accessibilityRole="button"
            >
              {eliminando ? (
                <ActivityIndicator color={tema.colores.textoBlanco} />
              ) : (
                <Text style={estilos.botonPeligroTexto}>Eliminar equipo</Text>
              )}
            </TouchableOpacity>
            <TouchableOpacity
              style={estilos.botonSecundario}
              onPress={() => setMostrarConfirmacion(false)}
              disabled={eliminando}
              accessibilityRole="button"
            >
              <Text style={estilos.botonSecundarioTexto}>Cancelar</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </PantallaBase>
  );
}

// HU45 — Tarjeta de integrante deslizable hacia la izquierda para expulsar.
// El swipe solo se habilita cuando el usuario es líder y la fila no es la del
// líder; al superar el umbral se pide confirmación y la tarjeta regresa.
const UMBRAL_SWIPE_EXPULSAR = -80;
const DESPLAZAMIENTO_MAXIMO = -120;

function FilaParticipante({
  participante,
  expulsable,
  onExpulsar,
}: {
  participante: IntegranteEquipo;
  expulsable: boolean;
  onExpulsar: () => void;
}) {
  const nombreCompleto = `${participante.nombre} ${participante.apellido}`.trim();
  const desplazamiento = useRef(new Animated.Value(0)).current;

  const regresar = useCallback(() => {
    Animated.spring(desplazamiento, {
      toValue: 0,
      useNativeDriver: true,
    }).start();
  }, [desplazamiento]);

  const panResponder = useMemo(
    () =>
      PanResponder.create({
        // Solo capturar gestos horizontales hacia la izquierda cuando la
        // fila es expulsable; deja pasar el scroll vertical.
        onMoveShouldSetPanResponder: (_evento, gesto) =>
          expulsable &&
          gesto.dx < -10 &&
          Math.abs(gesto.dx) > Math.abs(gesto.dy),
        onPanResponderMove: (_evento, gesto) => {
          if (gesto.dx < 0)
            desplazamiento.setValue(Math.max(gesto.dx, DESPLAZAMIENTO_MAXIMO));
        },
        onPanResponderRelease: (_evento, gesto) => {
          if (gesto.dx < UMBRAL_SWIPE_EXPULSAR) onExpulsar();
          regresar();
        },
        onPanResponderTerminate: regresar,
      }),
    [expulsable, onExpulsar, desplazamiento, regresar],
  );

  return (
    <View style={estilos.contenedorDeslizable}>
      {expulsable && (
        <View style={estilos.fondoExpulsar}>
          <Text style={estilos.fondoExpulsarTexto}>Expulsar</Text>
        </View>
      )}
      <Animated.View
        style={{ transform: [{ translateX: desplazamiento }] }}
        {...panResponder.panHandlers}
      >
        <View style={[estilos.tarjeta, estilos.tarjetaSinMargen]}>
          <View style={estilos.filaTitulo}>
            <Text style={estilos.nombreParticipante}>{nombreCompleto}</Text>
            {participante.esLider && (
              <Text style={[estilos.badge, estilos.badgePrimario]}>Líder</Text>
            )}
          </View>
          <Text style={estilos.metaLinea}>@{participante.alias}</Text>
          <Text style={estilos.metaLinea}>Puntaje: {participante.puntaje}</Text>
          <Text style={estilos.metaLinea}>
            Fecha de unión: {formatearFechaHora(participante.fechaUnion)}
          </Text>
        </View>
      </Animated.View>
    </View>
  );
}

const estilos = StyleSheet.create({
  encabezado: { marginBottom: tema.espacios.md, paddingTop: tema.espacios.md },
  titulo: {
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
  },
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.md,
  },
  tarjetaSinMargen: { marginBottom: 0 },
  contenedorDeslizable: {
    marginBottom: tema.espacios.md,
    position: "relative",
  },
  fondoExpulsar: {
    position: "absolute",
    top: 0,
    bottom: 0,
    left: 0,
    right: 0,
    backgroundColor: tema.colores.error,
    borderRadius: tema.radios.tarjeta,
    alignItems: "flex-end",
    justifyContent: "center",
    paddingRight: tema.espacios.lg,
  },
  fondoExpulsarTexto: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  filaTitulo: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    flexWrap: "wrap",
  },
  nombreEquipo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
  },
  nombreParticipante: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
  },
  badges: { flexDirection: "row", gap: tema.espacios.xs, flexWrap: "wrap" },
  badge: {
    fontSize: tema.tipografia.tamanos.xs,
    paddingHorizontal: tema.espacios.sm,
    paddingVertical: 2,
    borderRadius: tema.radios.entrada,
    overflow: "hidden",
    fontWeight: tema.tipografia.pesos.bold,
  },
  badgePrimario: {
    backgroundColor: tema.colores.primario,
    color: tema.colores.textoBlanco,
  },
  metaLinea: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
  },
  tituloSeccion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: "uppercase",
    marginBottom: tema.espacios.sm,
    marginLeft: tema.espacios.xs,
    fontWeight: tema.tipografia.pesos.bold,
  },
  contenedorEstado: { alignItems: "center", padding: tema.espacios.xl },
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
  botonPeligro: {
    backgroundColor: tema.colores.error,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
  },
  botonPeligroTexto: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  botonDeshabilitado: { opacity: 0.7 },
  modalFondo: {
    flex: 1,
    backgroundColor: "rgba(0,0,0,0.6)",
    justifyContent: "center",
    padding: tema.espacios.lg,
  },
  modalTarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
  },
  modalTitulo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
    marginBottom: tema.espacios.sm,
  },
  modalMensaje: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
    lineHeight: 20,
    marginBottom: tema.espacios.sm,
  },
});
