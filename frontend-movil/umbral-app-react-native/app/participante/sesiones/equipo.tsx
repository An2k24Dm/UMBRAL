import { useCallback, useEffect, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  Modal,
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
import { tema } from "../../../estilos/tema";
import { useDetalleEquipoSesion } from "../../../hooks/useDetalleEquipoSesion";
import { useEliminarEquipo } from "../../../hooks/useEliminarEquipo";
import { useNavegacionSegura } from "../../../hooks/useNavegacionSegura";
import { useRefrescarAlEnfocar } from "../../../hooks/useRefrescarAlEnfocar";
import { useSesionesTiempoReal } from "../../../hooks/useSesionesTiempoReal";
import type { IntegranteEquipo } from "../../../tipos/equipos";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";

// HU43 — Detalle real de un equipo de la sesión. El ingreso a un equipo es HU47.
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

  useEffect(() => {
    if (sesionExpirada || sesionExpiradaEliminar)
      cerrarSesion().finally(() => enrutador.replace("/"));
  }, [sesionExpirada, sesionExpiradaEliminar, cerrarSesion, enrutador]);

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

  const ingresarAlEquipo = () => {
    // HU47: ingreso real (público o con contraseña si es privado).
    Alert.alert(
      "Ingresar al equipo",
      "Ingresar a un equipo se implementará en la HU47.",
    );
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
          {equipo.participantes.length === 0 ? (
            <View style={estilos.tarjeta}>
              <Text style={estilos.metaLinea}>Este equipo no tiene integrantes.</Text>
            </View>
          ) : (
            equipo.participantes.map((p) => (
              <FilaParticipante key={p.participanteSesionId} participante={p} />
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

          {!equipo.esMiEquipo && (
            <TouchableOpacity
              style={estilos.botonPrimario}
              onPress={ingresarAlEquipo}
              accessibilityRole="button"
            >
              <Text style={estilos.botonPrimarioTexto}>Ingresar al equipo</Text>
            </TouchableOpacity>
          )}
        </View>
      )}

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

function FilaParticipante({ participante }: { participante: IntegranteEquipo }) {
  const nombreCompleto = `${participante.nombre} ${participante.apellido}`.trim();
  return (
    <View style={estilos.tarjeta}>
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
