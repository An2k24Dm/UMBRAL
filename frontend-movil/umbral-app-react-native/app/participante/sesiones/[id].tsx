import { useCallback, useEffect, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  RefreshControl,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import * as SecureStore from "expo-secure-store";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { BadgeEstadoSesionMovil } from "../../../componentes/sesiones/BadgeEstadoSesionMovil";
import { BadgeModoSesionMovil } from "../../../componentes/sesiones/BadgeModoSesionMovil";
import { ListaMisionesSesionMovil } from "../../../componentes/sesiones/ListaMisionesSesionMovil";
import { tema } from "../../../estilos/tema";
import { useAbandonarSesion } from "../../../hooks/useAbandonarSesion";
import { useDetalleSesionDisponible } from "../../../hooks/useDetalleSesionDisponible";
import { useIngresoSesion } from "../../../hooks/useIngresoSesion";
import { useNavegacionSegura } from "../../../hooks/useNavegacionSegura";
import { useRefrescarAlEnfocar } from "../../../hooks/useRefrescarAlEnfocar";
import { useSesionesTiempoReal } from "../../../hooks/useSesionesTiempoReal";
import type { SesionDetalleMovilDto } from "../../../tipos/sesiones";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";
import { claveEtapaCompletada } from "./tesoro";

export default function PantallaDetalleSesionParticipante() {
  return (
    <RutaProtegidaMovil>
      <ContenidoDetalle />
    </RutaProtegidaMovil>
  );
}

function ContenidoDetalle() {
  const enrutador = useRouter();
  const { cerrarSesion } = useAutenticacion();
  const parametros = useLocalSearchParams<{ id?: string | string[] }>();
  const sesionId = Array.isArray(parametros.id) ? parametros.id[0] : parametros.id;

  const {
    detalle,
    cargando,
    error,
    sesionNoDisponible,
    sesionExpirada,
    refrescar,
  } = useDetalleSesionDisponible(sesionId ?? null);

  const [etapasCompletadas, setEtapasCompletadas] = useState<Set<string>>(new Set());

  // Carga qué etapas ya completó el participante (persistido en SecureStore).
  const cargarProgreso = useCallback(async () => {
    if (!sesionId || !detalle) return;
    const todasEtapas = detalle.misiones.flatMap((m) => m.etapas.map((e) => e.id));
    const resultados = await Promise.all(
      todasEtapas.map((etapaId) =>
        SecureStore.getItemAsync(claveEtapaCompletada(sesionId, etapaId))
          .then((v) => ({ etapaId, completada: v === "1" }))
          .catch(() => ({ etapaId, completada: false })),
      ),
    );
    setEtapasCompletadas(
      new Set(resultados.filter((r) => r.completada).map((r) => r.etapaId)),
    );
  }, [sesionId, detalle]);

  useEffect(() => {
    void cargarProgreso();
  }, [cargarProgreso]);

  const refrescarTodo = useCallback(async () => {
    await refrescar();
    await cargarProgreso();
  }, [refrescar, cargarProgreso]);

  const navegarSeguro = useNavegacionSegura();
  useRefrescarAlEnfocar(refrescarTodo);

  // HU52 — cambio de estado en vivo. Si el operador cancela, avisamos y
  // volvemos al listado; para pausa/reanudación recargamos el detalle para
  // que el banner y las acciones reflejen el nuevo estado.
  const manejarCambioEstado = useCallback(
    (estado: string | undefined) => {
      if (estado === "Cancelada") {
        Alert.alert(
          "Sesión cancelada",
          "La sesión fue cancelada por el operador.",
          [
            {
              text: "Volver al listado",
              onPress: () => enrutador.replace("/participante/sesiones"),
            },
          ],
        );
        return;
      }
      void refrescar();
    },
    [refrescar, enrutador],
  );

  useSesionesTiempoReal({
    sesionId,
    onParticipantesSesionActualizados: refrescar,
    onEquiposSesionActualizados: refrescar,
    onEquipoActualizado: refrescar,
    onSesionActualizada: manejarCambioEstado,
  });

  const [refrescando, setRefrescando] = useState(false);
  const alRefrescar = useCallback(async () => {
    setRefrescando(true);
    try {
      await refrescarTodo();
    } finally {
      setRefrescando(false);
    }
  }, [refrescarTodo]);

  useEffect(() => {
    if (sesionExpirada) {
      cerrarSesion().finally(() => enrutador.replace("/"));
    }
  }, [sesionExpirada, cerrarSesion, enrutador]);

  const volverAlListado = () => enrutador.replace("/participante/sesiones");

  if (!sesionId) {
    // Defensa por si la ruta llegó sin id (no debería ocurrir con expo-router).
    return (
      <PantallaBase>
        <View style={estilos.cuadroError}>
          <Text style={estilos.cuadroErrorTexto}>
            No se indicó qué sesión consultar.
          </Text>
        </View>
        <TouchableOpacity
          style={estilos.botonSecundario}
          onPress={volverAlListado}
        >
          <Text style={estilos.botonSecundarioTexto}>Volver al listado</Text>
        </TouchableOpacity>
      </PantallaBase>
    );
  }

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
        <Text style={estilos.subtitulo}>Detalle de la sesión</Text>
      </View>

      {cargando && (
        <View style={estilos.contenedorEstado}>
          <ActivityIndicator color={tema.colores.primario} size="large" />
          <Text style={estilos.textoEstado}>Cargando detalle…</Text>
        </View>
      )}

      {!cargando && sesionNoDisponible && (
        <View>
          <View style={estilos.cuadroError}>
            <Text style={estilos.cuadroErrorTexto}>
              Esta sesión ya no está disponible para consulta.
            </Text>
          </View>
          <TouchableOpacity
            style={estilos.botonPrimario}
            onPress={volverAlListado}
          >
            <Text style={estilos.botonPrimarioTexto}>Volver al listado</Text>
          </TouchableOpacity>
        </View>
      )}

      {!cargando &&
        error &&
        !sesionExpirada &&
        !sesionNoDisponible && (
          <View>
            <View style={estilos.cuadroError}>
              <Text style={estilos.cuadroErrorTexto}>{error}</Text>
            </View>
            <TouchableOpacity style={estilos.botonPrimario} onPress={refrescar}>
              <Text style={estilos.botonPrimarioTexto}>Reintentar</Text>
            </TouchableOpacity>
          </View>
        )}

      {!cargando && !error && detalle && (
        <>
          <View style={estilos.tarjetaCabecera}>
            <View style={estilos.filaBadges}>
              <BadgeModoSesionMovil modo={detalle.modo} />
              <BadgeEstadoSesionMovil estado={detalle.estado} />
            </View>
            <Text style={estilos.nombre}>{detalle.nombre}</Text>
            {detalle.descripcion?.trim().length > 0 && (
              <Text style={estilos.descripcion}>{detalle.descripcion}</Text>
            )}
            <View style={estilos.bloqueMeta}>
              <Text style={estilos.metaEtiqueta}>FECHA PROGRAMADA</Text>
              <Text style={estilos.metaValor}>
                {formatearFechaHora(detalle.fechaProgramada)}
              </Text>
            </View>
          </View>

          {detalle.estado === "Pausada" && (
            <View style={estilos.bannerPausa}>
              <Text style={estilos.bannerPausaTitulo}>
                Sesión pausada por el operador.
              </Text>
              <Text style={estilos.bannerPausaTexto}>
                Puedes seguir viendo la información de la sesión, pero el juego
                está detenido hasta que el operador la reanude.
              </Text>
            </View>
          )}

          {detalle.estado === "Cancelada" && (
            <View style={estilos.bannerCancelada}>
              <Text style={estilos.bannerCanceladaTitulo}>
                La sesión fue cancelada por el operador.
              </Text>
              <Text style={estilos.bannerCanceladaTexto}>
                Ya no es posible jugar en esta sesión.
              </Text>
            </View>
          )}

          <Text style={estilos.tituloSeccion}>MISIONES</Text>
          <ListaMisionesSesionMovil misiones={detalle.misiones} />

          <SeccionParticipacion
            detalle={detalle}
            sesionId={sesionId}
            enrutador={enrutador}
            navegarSeguro={navegarSeguro}
            refrescar={refrescarTodo}
            etapasCompletadas={etapasCompletadas}
          />
        </>
      )}

      <TouchableOpacity
        style={estilos.botonSecundario}
        onPress={volverAlListado}
        accessibilityRole="button"
      >
        <Text style={estilos.botonSecundarioTexto}>Volver al listado</Text>
      </TouchableOpacity>
    </PantallaBase>
  );
}

// HU40 — Render según el estado de participación que devuelve el backend.
// Nunca se muestran dos acciones contradictorias (Unirse + Ver equipo).
function SeccionParticipacion({
  detalle,
  sesionId,
  enrutador,
  navegarSeguro,
  refrescar,
  etapasCompletadas,
}: {
  detalle: SesionDetalleMovilDto;
  sesionId: string;
  enrutador: ReturnType<typeof useRouter>;
  navegarSeguro: (accion: () => void) => void;
  refrescar: () => Promise<void>;
  etapasCompletadas: Set<string>;
}) {
  const { cerrarSesion } = useAutenticacion();
  const {
    ingresando,
    error: errorIngreso,
    sesionExpirada,
    ingresarIndividual,
  } = useIngresoSesion();
  // HU48 — Abandono voluntario de la sesión individual.
  const {
    abandonando,
    error: errorAbandonar,
    sesionExpirada: sesionExpiradaAbandonar,
    abandonarSesion,
    limpiarError: limpiarErrorAbandonar,
  } = useAbandonarSesion();
  const participacion = detalle.participacionActual;
  const esGrupal = detalle.modo === "Grupal";
  const enPreparacion = detalle.estado === "EnPreparacion";

  useEffect(() => {
    if (sesionExpirada || sesionExpiradaAbandonar)
      cerrarSesion().finally(() => enrutador.replace("/"));
  }, [sesionExpirada, sesionExpiradaAbandonar, cerrarSesion, enrutador]);

  const confirmarAbandono = useCallback(async () => {
    const ok = await abandonarSesion(sesionId);
    if (ok) {
      Alert.alert("Sesión", "Abandonaste la sesión correctamente.");
      enrutador.replace("/participante/sesiones");
    }
    // El 409 del backend (no EnPreparacion) queda en errorAbandonar.
  }, [abandonarSesion, sesionId, enrutador]);

  const solicitarAbandono = useCallback(() => {
    if (abandonando) return;
    limpiarErrorAbandonar();
    Alert.alert(
      "Abandonar sesión",
      "¿Seguro que deseas abandonar esta sesión? Se liberará tu cupo y " +
        "podrás ingresar a otra sesión si lo deseas.",
      [
        { text: "Cancelar", style: "cancel" },
        {
          text: "Abandonar",
          style: "destructive",
          onPress: () => void confirmarAbandono(),
        },
      ],
    );
  }, [abandonando, limpiarErrorAbandonar, confirmarAbandono]);

  // Casos B y D: el participante ya pertenece a la sesión.
  if (participacion?.estaInscrito) {
    const sesionActiva = detalle.estado === "Activa";

    // Para cada misión, renderiza botones de etapas con orden secuencial:
    // solo se puede jugar la etapa N si se completó la etapa N-1.
    const botonesEtapasPorMision = detalle.misiones.map((mision) => {
      const etapasOrdenadas = [...mision.etapas].sort((a, b) => a.orden - b.orden);

      return etapasOrdenadas.map((etapa, idx) => {
        const tipo = etapa.tipoModoDeJuego;
        if (tipo !== "Trivia" && tipo !== "BusquedaTesoro") return null;

        const yaCompletada = etapasCompletadas.has(etapa.id);
        const etapaAnterior = idx > 0 ? etapasOrdenadas[idx - 1] : null;
        const anteriorCompletada = etapaAnterior ? etapasCompletadas.has(etapaAnterior.id) : true;
        const bloqueada = !anteriorCompletada;

        if (yaCompletada) {
          // Etapa ya completada: mostrar badge informativo (no botón)
          return (
            <View key={etapa.id} style={estilos.etapaCompletada}>
              <Text style={estilos.etapaCompletadaTexto}>
                ✓ {etapa.nombreModoDeJuego} — completada
              </Text>
            </View>
          );
        }

        if (bloqueada) {
          // Etapa bloqueada: mostrar pero deshabilitada
          return (
            <View key={etapa.id} style={estilos.etapaBloqueada}>
              <Text style={estilos.etapaBloqueadaTexto}>
                🔒 {etapa.nombreModoDeJuego} — completa la etapa anterior
              </Text>
            </View>
          );
        }

        // Etapa disponible para jugar
        if (tipo === "Trivia") {
          return (
            <TouchableOpacity
              key={etapa.id}
              style={estilos.botonJugar}
              onPress={() =>
                navegarSeguro(() =>
                  enrutador.push(
                    `/participante/sesiones/jugar?sesionId=${sesionId}` +
                      `&misionId=${mision.id}&etapaId=${etapa.id}` +
                      `&triviaId=${etapa.modoDeJuegoId}`,
                  ),
                )
              }
              accessibilityRole="button"
            >
              <Text style={estilos.botonJugarTexto}>
                Trivia: {etapa.nombreModoDeJuego}
              </Text>
            </TouchableOpacity>
          );
        }

        return (
          <TouchableOpacity
            key={etapa.id}
            style={estilos.botonJugarTesoro}
            onPress={() =>
              navegarSeguro(() =>
                enrutador.push(
                  `/participante/sesiones/tesoro?sesionId=${sesionId}` +
                    `&misionId=${mision.id}&etapaId=${etapa.id}` +
                    `&busquedaId=${etapa.modoDeJuegoId}`,
                ),
              )
            }
            accessibilityRole="button"
          >
            <Text style={estilos.botonJugarTexto}>
              Tesoro: {etapa.nombreModoDeJuego}
            </Text>
          </TouchableOpacity>
        );
      });
    });

    if (participacion.tipo === "Equipo") {
      return (
        <View>
          <View style={estilos.tarjetaParticipacion}>
            <Text style={estilos.participacionTexto}>
              Ya perteneces a un equipo en esta sesión.
            </Text>
            {participacion.equipoNombre ? (
              <Text style={estilos.participacionDetalle}>
                Equipo: {participacion.equipoNombre}
              </Text>
            ) : null}
            <TouchableOpacity
              style={estilos.botonPrimario}
              onPress={() =>
                navegarSeguro(() =>
                  enrutador.push(
                    `/participante/sesiones/equipo?sesionId=${sesionId}` +
                      `&equipoId=${participacion.equipoId ?? ""}`,
                  ),
                )
              }
              accessibilityRole="button"
            >
              <Text style={estilos.botonPrimarioTexto}>Ver equipo</Text>
            </TouchableOpacity>
          </View>
          {sesionActiva && botonesEtapasPorMision}
        </View>
      );
    }

    // Sesión individual ya ingresada.
    return (
      <View>
        <View style={estilos.tarjetaParticipacion}>
          <Text style={estilos.participacionTexto}>
            Ya ingresaste a esta sesión.
          </Text>
          {errorAbandonar ? (
            <View style={estilos.cuadroError}>
              <Text style={estilos.cuadroErrorTexto}>{errorAbandonar}</Text>
            </View>
          ) : null}
          {enPreparacion && (
            <TouchableOpacity
              style={[estilos.botonPeligro, abandonando && estilos.botonDeshabilitado]}
              onPress={solicitarAbandono}
              disabled={abandonando}
              accessibilityRole="button"
            >
              {abandonando ? (
                <ActivityIndicator color={tema.colores.textoBlanco} />
              ) : (
                <Text style={estilos.botonPeligroTexto}>Abandonar sesión</Text>
              )}
            </TouchableOpacity>
          )}
        </View>
        {sesionActiva && botonesEtapasPorMision}
      </View>
    );
  }

  // Caso E: no pertenece a esta sesión pero ya está en otra activa.
  if (!detalle.puedeIngresar) {
    return (
      <View style={estilos.tarjetaParticipacion}>
        <Text style={estilos.participacionTexto}>
          Ya estás participando en otra sesión.
        </Text>
        <Text style={estilos.participacionDetalle}>
          Debes esperar a que finalice o sea cancelada para ingresar a una nueva.
        </Text>
        {detalle.sesionActualNombre ? (
          <Text style={estilos.participacionDetalle}>
            Sesión actual: {detalle.sesionActualNombre}
          </Text>
        ) : null}
      </View>
    );
  }

  // No inscrito: solo se puede unir mientras la sesión está En Preparación.
  if (!enPreparacion) {
    return (
      <View style={estilos.tarjetaParticipacion}>
        <Text style={estilos.participacionDetalle}>
          Solo puedes unirte mientras la sesión está en preparación.
        </Text>
      </View>
    );
  }

  const alPresionarUnirse = async () => {
    if (esGrupal) {
      navegarSeguro(() =>
        enrutador.push(
          `/participante/sesiones/unirse?sesionId=${sesionId}` +
            `&nombre=${encodeURIComponent(detalle.nombre)}`,
        ),
      );
      return;
    }

    const resultado = await ingresarIndividual(sesionId);
    if (resultado?.ingresoRegistrado) {
      await refrescar();
      enrutador.replace(`/participante/sesiones/${sesionId}`);
    }
  };

  return (
    <View>
      {esGrupal ? (
        <View style={estilos.tarjetaParticipacion}>
          <Text style={estilos.participacionDetalle}>
            Para ingresar a una sesión grupal debes crear o unirte a un equipo.
          </Text>
        </View>
      ) : null}
      {errorIngreso ? (
        <View style={estilos.cuadroError}>
          <Text style={estilos.cuadroErrorTexto}>{errorIngreso}</Text>
        </View>
      ) : null}
      <TouchableOpacity
        style={[estilos.botonPrimario, ingresando && { opacity: 0.6 }]}
        onPress={alPresionarUnirse}
        disabled={ingresando}
        accessibilityRole="button"
      >
        {ingresando ? (
          <ActivityIndicator color={tema.colores.textoBlanco} />
        ) : (
          <Text style={estilos.botonPrimarioTexto}>
            {esGrupal ? "Opciones de equipo" : "Unirse"}
          </Text>
        )}
      </TouchableOpacity>
    </View>
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
  tarjetaCabecera: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.md,
  },
  filaBadges: {
    flexDirection: "row",
    gap: tema.espacios.sm,
    marginBottom: tema.espacios.sm,
    flexWrap: "wrap",
  },
  nombre: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
  },
  descripcion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
    marginTop: tema.espacios.sm,
    lineHeight: 20,
  },
  bloqueMeta: {
    marginTop: tema.espacios.md,
    paddingTop: tema.espacios.sm,
    borderTopWidth: 1,
    borderTopColor: tema.colores.bordeTarjeta,
  },
  metaEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
  },
  metaValor: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.semibold,
    marginTop: 2,
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
  bannerPausa: {
    backgroundColor: tema.colores.avisoSuave,
    borderColor: tema.colores.aviso,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  bannerPausaTitulo: {
    color: tema.colores.aviso,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
  },
  bannerPausaTexto: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
    lineHeight: 18,
  },
  bannerCancelada: {
    backgroundColor: tema.colores.errorSuave,
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  bannerCanceladaTitulo: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
  },
  bannerCanceladaTexto: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
    lineHeight: 18,
  },
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
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
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
  tarjetaParticipacion: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginTop: tema.espacios.sm,
  },
  participacionTexto: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
    textAlign: "center",
  },
  participacionDetalle: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
    marginTop: tema.espacios.xs,
  },
  botonSecundarioTexto: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  botonJugar: {
    backgroundColor: tema.colores.exito,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
  },
  botonJugarTesoro: {
    backgroundColor: "#d97706",
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
  },
  botonJugarTexto: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  etapaCompletada: {
    backgroundColor: "#d1fae5",
    paddingVertical: tema.espacios.sm,
    paddingHorizontal: tema.espacios.md,
    borderRadius: tema.radios.boton,
    marginTop: tema.espacios.sm,
    borderWidth: 1,
    borderColor: "#34d399",
  },
  etapaCompletadaTexto: {
    color: "#065f46",
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  etapaBloqueada: {
    backgroundColor: "#f3f4f6",
    paddingVertical: tema.espacios.sm,
    paddingHorizontal: tema.espacios.md,
    borderRadius: tema.radios.boton,
    marginTop: tema.espacios.sm,
    borderWidth: 1,
    borderColor: "#d1d5db",
  },
  etapaBloqueadaTexto: {
    color: "#6b7280",
    fontSize: tema.tipografia.tamanos.md,
  },
});
