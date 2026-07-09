import { useCallback, useEffect, useRef, useState } from "react";
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
import { useListadoSesionesTiempoReal } from "../../../hooks/useListadoSesionesTiempoReal";
import { useNavegacionSegura } from "../../../hooks/useNavegacionSegura";
import { useRefrescarAlEnfocar } from "../../../hooks/useRefrescarAlEnfocar";
import { useSesionesTiempoReal } from "../../../hooks/useSesionesTiempoReal";
import type {
  ProgresoSecuencialSesionDto,
  ProgresoSesionParticipanteDto,
  SesionDetalleMovilDto,
} from "../../../tipos/sesiones";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";
import {
  obtenerDetalleSesionDisponibleApi,
  obtenerProgresoSecuencialSesionApi,
  obtenerProgresoSesionParticipanteApi,
} from "../../../servicios/sesionesApi";

export default function PantallaDetalleSesionParticipante() {
  return (
    <RutaProtegidaMovil>
      <ContenidoDetalle />
    </RutaProtegidaMovil>
  );
}

function ContenidoDetalle() {
  const enrutador = useRouter();
  const { sesion: sesionAuth, cerrarSesion } = useAutenticacion();
  const token = sesionAuth?.tokenAcceso ?? null;
  const parametros = useLocalSearchParams<{ id?: string | string[] }>();
  const sesionId = Array.isArray(parametros.id) ? parametros.id[0] : parametros.id;

  const {
    detalle,
    cargando,
    error,
    sesionNoDisponible,
    sesionExpirada,
    refrescar,
    actualizarEstadoLocal,
  } = useDetalleSesionDisponible(sesionId ?? null);

  const [progresoSecuencial, setProgresoSecuencial] =
    useState<ProgresoSecuencialSesionDto | null>(null);
  const [progreso, setProgreso] = useState<ProgresoSesionParticipanteDto[] | null>(null);
  const [cargandoProgreso, setCargandoProgreso] = useState(false);
  const [versionProgreso, setVersionProgreso] = useState(0);
  const estaInscritoEnSesion = detalle?.participacionActual?.estaInscrito === true;
  const equipoIdActual = estaInscritoEnSesion
    ? detalle?.participacionActual?.equipoId ?? null
    : null;

  const cargarProgresoSecuencial = useCallback(async () => {
    if (!token || !sesionId || !detalle || !estaInscritoEnSesion) {
      setProgresoSecuencial(null);
      return;
    }

    try {
      const data = await obtenerProgresoSecuencialSesionApi(token, sesionId);
      setProgresoSecuencial(data);
    } catch {
      setProgresoSecuencial(null);
    }
  }, [token, sesionId, detalle, estaInscritoEnSesion]);

  useEffect(() => {
    void cargarProgresoSecuencial();
  }, [cargarProgresoSecuencial, versionProgreso]);

  const refrescarTodo = useCallback(async () => {
    await refrescar();
    await cargarProgresoSecuencial();
    setVersionProgreso((v) => v + 1);
  }, [refrescar, cargarProgresoSecuencial]);

  useEffect(() => {
    if (!token || !sesionId || !detalle) return;
    const estados = ["Activa", "Pausada", "Finalizada"];
    if (!estados.includes(detalle.estado)) return;
    let cancelado = false;
    setCargandoProgreso(true);
    obtenerProgresoSesionParticipanteApi(token, sesionId)
      .then((data) => { if (!cancelado) setProgreso(data); })
      .catch(() => { if (!cancelado) setProgreso(null); })
      .finally(() => { if (!cancelado) setCargandoProgreso(false); });
    return () => { cancelado = true; };
  }, [token, sesionId, detalle?.estado, versionProgreso]);

  const navegarSeguro = useNavegacionSegura();
  useRefrescarAlEnfocar(refrescarTodo);

  const navegarAEjecucionActual = useCallback(async () => {
    if (!token || !sesionId) return;

    // Best-effort: cualquier fallo (no inscrito, red, expulsado) se ignora
    // silenciosamente; nunca debe propagarse como "uncaught in promise".
    try {
      // Primero el detalle: si la sesión no está Activa o el usuario NO está
      // inscrito, no se consulta el progreso secuencial (ese endpoint rechaza a
      // los no inscritos) ni se redirige.
      const detalleActualizado = await obtenerDetalleSesionDisponibleApi(token, sesionId);
      if (
        detalleActualizado.estado !== "Activa" ||
        detalleActualizado.participacionActual?.estaInscrito !== true
      ) {
        return;
      }

      const progresoActualizado = await obtenerProgresoSecuencialSesionApi(token, sesionId);
      // Mantiene el progreso del detalle fresco (estados COMPLETADA de
      // etapas/misiones) aunque se pierda el evento SignalR EtapaCompletada.
      setProgresoSecuencial(progresoActualizado);
      // #16: durante la preparación (Preparacion) o el cierre pendiente
      // (CierrePendiente) la etapa NO es jugable todavía. El participante
      // permanece en el detalle viendo el banner global; la navegación al juego
      // solo ocurre en fase Activa o al llegar EtapaIniciada.
      if (
        progresoActualizado.faseEtapaActual === "Preparacion" ||
        progresoActualizado.faseEtapaActual === "CierrePendiente" ||
        progresoActualizado.jugadorActualCompletoEtapaActual === true ||
        !progresoActualizado.misionActualId ||
        !progresoActualizado.etapaActualId ||
        !progresoActualizado.modoDeJuegoId ||
        !progresoActualizado.tipoEtapaActual
      ) {
        return;
      }

      const destino =
        progresoActualizado.tipoEtapaActual === "Trivia"
          ? `/participante/sesiones/jugar?sesionId=${sesionId}` +
            `&misionId=${progresoActualizado.misionActualId}` +
            `&etapaId=${progresoActualizado.etapaActualId}` +
            `&triviaId=${progresoActualizado.modoDeJuegoId}`
          : `/participante/sesiones/tesoro?sesionId=${sesionId}` +
            `&misionId=${progresoActualizado.misionActualId}` +
            `&etapaId=${progresoActualizado.etapaActualId}` +
            `&busquedaId=${progresoActualizado.modoDeJuegoId}`;

      enrutador.replace(destino);
    } catch {
      // Silencioso a propósito (best-effort).
    }
  }, [token, sesionId, enrutador]);

  // Escenario de reapertura / entrada directa al detalle durante una etapa
  // activa: si la sesión está Activa, el participante está inscrito y NO
  // completó la etapa actual, se le envía automáticamente a la ejecución actual.
  // navegarAEjecucionActual es best-effort y tiene guardas internas (no redirige
  // si ya completó su parte o si no procede), evitando loops de navegación.
  useEffect(() => {
    void navegarAEjecucionActual();
  }, [navegarAEjecucionActual]);

  // Red de seguridad mientras el participante ESPERA en el detalle (etapa que él
  // ya completó, cierre pendiente o preparación de la siguiente): consulta el
  // progreso cada ~2,5 s. Aunque se pierda el evento SignalR EtapaIniciada/
  // EtapaCompletada (p. ej. app en background), se refrescan los estados
  // COMPLETADA y se redirige en cuanto el backend activa la siguiente etapa
  // (fase Activa). El backend sigue siendo la única autoridad del tiempo/fase; el
  // reloj local no decide nada, solo consulta. Solo corre en el detalle de una
  // sesión Activa en la que el participante está inscrito; al navegar al juego la
  // pantalla se desmonta y el intervalo se limpia.
  useEffect(() => {
    if (detalle?.estado !== "Activa" || !estaInscritoEnSesion) return;
    const id = setInterval(() => {
      void navegarAEjecucionActual();
    }, 2500);
    return () => clearInterval(id);
  }, [detalle?.estado, estaInscritoEnSesion, navegarAEjecucionActual]);

  // HU52 — cambio de estado en vivo. Si el operador cancela, avisamos y
  // volvemos al listado. Si finaliza, avisamos pero el participante se queda
  // en la pantalla para ver sus resultados.
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
      if (estado === "Finalizada") {
        Alert.alert(
          "Sesión finalizada",
          "La sesión ha concluido. Puedes revisar tus resultados antes de salir.",
          [{ text: "Ver resultados", style: "default" }],
        );
        // Actualización optimista: marca la sesión como finalizada de inmediato
        // para que el participante vea sus resultados aunque el refetch falle.
        actualizarEstadoLocal("Finalizada");
        setVersionProgreso((v) => v + 1);
        void refrescar();
        return;
      }
      void refrescar();
      setVersionProgreso((v) => v + 1);
      if (estado === "Activa") {
        void navegarAEjecucionActual();
      }
    },
    [refrescar, enrutador, actualizarEstadoLocal, navegarAEjecucionActual],
  );

  const actualizarProgreso = useCallback(
    () => setVersionProgreso((v) => v + 1),
    [],
  );

  useListadoSesionesTiempoReal({
    onListadoActualizado: refrescar,
    activo: !estaInscritoEnSesion,
  });

  useSesionesTiempoReal({
    origen: "Detalle",
    sesionId: estaInscritoEnSesion ? sesionId : null,
    equipoId: equipoIdActual,
    onParticipantesSesionActualizados: refrescar,
    onEquiposSesionActualizados: refrescar,
    onEquipoActualizado: refrescar,
    onSesionActualizada: manejarCambioEstado,
    onRespuestaRegistrada: actualizarProgreso,
    onEtapaCompletada: actualizarProgreso,
    onEtapaIniciada: navegarAEjecucionActual,
    onProgresoSecuencialActualizado: actualizarProgreso,
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

          {detalle.estado === "Finalizada" && (
            <View style={estilos.bannerFinalizada}>
              <Text style={estilos.bannerFinalizadaTitulo}>
                Sesión finalizada
              </Text>
              <Text style={estilos.bannerFinalizadaTexto}>
                La sesión ha concluido. Estos son tus resultados.
              </Text>
            </View>
          )}

          {detalle.estado === "Activa" &&
          detalle.fechaInicioUtc &&
          detalle.duracionSegundosLimite !== null &&
          detalle.duracionSegundosLimite > 0 ? (
            <CuentaRegresiva
              fechaInicioUtc={detalle.fechaInicioUtc}
              duracionSegundos={detalle.duracionSegundosLimite}
            />
          ) : null}

          <Text style={estilos.tituloSeccion}>MISIONES</Text>
          <ListaMisionesSesionMovil
            misiones={detalle.misiones}
            etapasCompletadasGlobalmenteIds={
              progresoSecuencial?.etapasCompletadasGlobalmenteIds ?? []
            }
          />

          {(detalle.estado === "Activa" ||
            detalle.estado === "Pausada" ||
            detalle.estado === "Finalizada") && (
            <PanelProgreso
              progreso={progreso}
              cargando={cargandoProgreso}
            />
          )}

          <SeccionParticipacion
            detalle={detalle}
            sesionId={sesionId}
            enrutador={enrutador}
            navegarSeguro={navegarSeguro}
            refrescar={refrescarTodo}
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
}: {
  detalle: SesionDetalleMovilDto;
  sesionId: string;
  enrutador: ReturnType<typeof useRouter>;
  navegarSeguro: (accion: () => void) => void;
  refrescar: () => Promise<void>;
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
  // El estado de las etapas/misiones (COMPLETADA) se muestra únicamente en el
  // listado principal (ListaMisionesSesionMovil). Aquí no se repite: la entrada
  // al juego es automática (EjecucionActualSesion + SignalR EtapaIniciada).
  if (participacion?.estaInscrito) {
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

function PanelProgreso({
  progreso,
  cargando,
}: {
  progreso: ProgresoSesionParticipanteDto[] | null;
  cargando: boolean;
}) {
  const ordenado = progreso
    ? [...progreso].sort((a, b) => b.totalPuntosGanados - a.totalPuntosGanados)
    : [];

  return (
    <View style={estilos.panelProgreso}>
      <Text style={estilos.tituloSeccion}>TABLA DE POSICIONES</Text>
      {cargando && (
        <ActivityIndicator color={tema.colores.primario} style={{ marginVertical: tema.espacios.sm }} />
      )}
      {!cargando && progreso !== null && ordenado.length === 0 && (
        <Text style={estilos.progresoVacio}>Aún no hay actividad registrada.</Text>
      )}
      {!cargando && ordenado.map((p, idx) => (
        <View key={p.participanteIdentidadId} style={estilos.filaProgreso}>
          <View style={estilos.filaProgresoPuesto}>
            <Text style={[estilos.puesto, idx === 0 && estilos.puestoOro]}>
              #{idx + 1}
            </Text>
          </View>
          <View style={estilos.filaProgresoDetalle}>
            <Text style={estilos.progresoTotal}>{p.totalPuntosGanados} pts</Text>
            <Text style={estilos.progresoDesglose}>
              Trivia: {p.triviaPuntosGanados} · Tesoro: {p.tesoroPuntosGanados}
            </Text>
          </View>
          <View style={estilos.filaProgresoExtra}>
            {p.triviaRespondidas > 0 && (
              <Text style={estilos.progresoChip}>
                {p.triviaCorrectas}/{p.triviaRespondidas} ✓
              </Text>
            )}
            {p.tesoroEtapasCompletadas > 0 && (
              <Text style={estilos.progresoChipTesoro}>
                {p.tesoroEtapasCompletadas} etapa{p.tesoroEtapasCompletadas !== 1 ? "s" : ""} tesoro
              </Text>
            )}
          </View>
        </View>
      ))}
    </View>
  );
}

function CuentaRegresiva({
  fechaInicioUtc,
  duracionSegundos,
}: {
  fechaInicioUtc: string;
  duracionSegundos: number;
}) {
  const calcularSegundosRestantes = () => {
    const fin = new Date(fechaInicioUtc).getTime() + duracionSegundos * 1000;
    return Math.max(0, Math.floor((fin - Date.now()) / 1000));
  };

  const [segundos, setSegundos] = useState(calcularSegundosRestantes);
  const intervaloRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    intervaloRef.current = setInterval(() => {
      setSegundos(calcularSegundosRestantes());
    }, 1000);
    return () => {
      if (intervaloRef.current) clearInterval(intervaloRef.current);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fechaInicioUtc, duracionSegundos]);

  const horas = Math.floor(segundos / 3600);
  const minutos = Math.floor((segundos % 3600) / 60);
  const segs = segundos % 60;
  const texto =
    horas > 0
      ? `${horas}:${String(minutos).padStart(2, "0")}:${String(segs).padStart(2, "0")}`
      : `${minutos}:${String(segs).padStart(2, "0")}`;
  const urgente = segundos <= 60;

  return (
    <View style={estilos.bannerCuentaRegresiva}>
      <Text style={estilos.bannerCuentaRegresivaEtiqueta}>TIEMPO RESTANTE</Text>
      <Text
        style={[
          estilos.bannerCuentaRegresivaValor,
          urgente && estilos.bannerCuentaRegresivaUrgente,
        ]}
      >
        {segundos <= 0 ? "Tiempo agotado" : texto}
      </Text>
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
  panelProgreso: {
    marginBottom: tema.espacios.md,
  },
  progresoVacio: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
    paddingVertical: tema.espacios.sm,
  },
  filaProgreso: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.sm,
    marginBottom: tema.espacios.xs,
    gap: tema.espacios.sm,
  },
  filaProgresoPuesto: {
    width: 36,
    alignItems: "center",
  },
  puesto: {
    color: tema.colores.textoTenue,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  puestoOro: {
    color: "#f59e0b",
    fontSize: tema.tipografia.tamanos.lg,
  },
  filaProgresoDetalle: {
    flex: 1,
  },
  progresoTotal: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.extrabold,
    fontSize: tema.tipografia.tamanos.md,
  },
  progresoDesglose: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    marginTop: 2,
  },
  filaProgresoExtra: {
    gap: 4,
    alignItems: "flex-end",
  },
  progresoChip: {
    color: "#15803d",
    backgroundColor: "#dcfce7",
    fontSize: tema.tipografia.tamanos.xs,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  progresoChipTesoro: {
    color: "#92400e",
    backgroundColor: "#fef3c7",
    fontSize: tema.tipografia.tamanos.xs,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  bannerFinalizada: {
    backgroundColor: "#f0fdf4",
    borderColor: tema.colores.exito,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  bannerFinalizadaTitulo: {
    color: "#15803d",
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
  },
  bannerFinalizadaTexto: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
    lineHeight: 18,
  },
  bannerCuentaRegresiva: {
    backgroundColor: "#1e293b",
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
    alignItems: "center",
  },
  bannerCuentaRegresivaEtiqueta: {
    color: "#94a3b8",
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.bold,
    letterSpacing: 1,
    marginBottom: tema.espacios.xs,
  },
  bannerCuentaRegresivaValor: {
    color: "#f8fafc",
    fontSize: 32,
    fontWeight: tema.tipografia.pesos.bold,
    fontVariant: ["tabular-nums"],
  },
  bannerCuentaRegresivaUrgente: {
    color: tema.colores.error,
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
});
