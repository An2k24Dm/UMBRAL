import { useEffect, useState } from "react";
import { Alert, AppState } from "react-native";
import * as signalR from "@microsoft/signalr";
import { useRouter } from "expo-router";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  mapearEtapaPorComenzar,
  obtenerEstadoEvento,
  obtenerSesionIdEvento,
  type EstadoBannerEtapaPorComenzar,
  type EventoEtapaPorComenzar,
  type EventoSesionTiempoReal,
  registrarErrorConexionTiempoRealDev,
  registrarEventoConexionSesionesTiempoReal,
} from "../servicios/sesionesTiempoReal";
import {
  listarSesionesDisponiblesApi,
  obtenerDetalleSesionDisponibleApi,
  obtenerProgresoSecuencialSesionApi,
} from "../servicios/sesionesApi";
import { intentarMarcarFinalizacionNotificada } from "../servicios/notificacionesFinalizacionSesion";
import { suscribirMembresiaTiempoReal } from "../servicios/membresiaTiempoReal";
import type { ProgresoSecuencialSesionDto } from "../tipos/sesiones";

const FASE_PREPARACION = "Preparacion";

function bannerDesdeProgreso(
  sesionId: string,
  progreso: ProgresoSecuencialSesionDto,
): EstadoBannerEtapaPorComenzar | null {
  if (progreso.faseEtapaActual !== FASE_PREPARACION) return null;
  const esNuevaMision = progreso.esNuevaMision === true;
  const numeroMision = progreso.numeroMisionActual ?? 0;
  const numeroEtapa = progreso.numeroEtapaActual ?? 0;
  const duracionPreparacionSegundos = progreso.duracionPreparacionSegundos ?? 10;
  const fechaInicioProgramadaUtc =
    progreso.fechaInicioProgramadaEtapaUtc ??
    new Date(
      Date.now() + (progreso.segundosRestantesPreparacion ?? duracionPreparacionSegundos) * 1000,
    ).toISOString();

  return {
    sesionId,
    mensaje: esNuevaMision
      ? `Misión ${numeroMision} está por comenzar`
      : `Etapa ${numeroEtapa} está por comenzar`,
    numeroMision,
    numeroEtapa,
    esNuevaMision,
    fechaInicioProgramadaUtc,
    duracionPreparacionSegundos,
  };
}

// Destino de juego (Trivia/Tesoro) a partir del progreso autoritativo, o null si
// no procede navegar (ya completó, sin etapa jugable, etc.).
function construirDestinoJuego(
  sesionId: string,
  progreso: ProgresoSecuencialSesionDto,
): string | null {
  if (
    progreso.jugadorActualCompletoEtapaActual === true ||
    !progreso.misionActualId ||
    !progreso.etapaActualId ||
    !progreso.modoDeJuegoId ||
    !progreso.tipoEtapaActual
  ) {
    return null;
  }
  return progreso.tipoEtapaActual === "Trivia"
    ? `/participante/sesiones/jugar?sesionId=${sesionId}` +
        `&misionId=${progreso.misionActualId}` +
        `&etapaId=${progreso.etapaActualId}` +
        `&triviaId=${progreso.modoDeJuegoId}`
    : `/participante/sesiones/tesoro?sesionId=${sesionId}` +
        `&misionId=${progreso.misionActualId}` +
        `&etapaId=${progreso.etapaActualId}` +
        `&busquedaId=${progreso.modoDeJuegoId}`;
}

export function useAvisosSesionTiempoReal() {
  const {
    sesion,
    cargandoSesion,
    estaAutenticado,
    cerrarSesion,
  } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;
  const enrutador = useRouter();
  // Banner global de preparación (EtapaPorComenzar). null ⇒ oculto.
  const [bannerEtapaPorComenzar, setBannerEtapaPorComenzar] =
    useState<EstadoBannerEtapaPorComenzar | null>(null);

  useEffect(() => {
    if (cargandoSesion || !token || !estaAutenticado) return;

    let desmontado = false;
    let invalidandoSesion = false;
    let cerrando = false;
    let cierreRegistrado = false;
    let inicioPromise: Promise<void> | null = null;
    let sesionesSuscritas: Array<{ sesionId: string; equipoId: string | null }> = [];
    const conexion = crearConexionSesionesTiempoReal(token, "Avisos");

    const logDev = (mensaje: string) => {
      registrarEventoConexionSesionesTiempoReal(conexion, mensaje);
    };

    const registrarCerrado = () => {
      if (cierreRegistrado) return;
      cierreRegistrado = true;
      logDev("cerrado");
    };

    const manejarErrorConexion = async (error: unknown, contexto?: string) => {
      if (desmontado || invalidandoSesion) return;

      // Solo un 401 real invalida la sesión y redirige. Un timeout/transporte
      // transitorio se registra como diagnóstico (dev) sin modal ni logout: la
      // reconexión automática lo resuelve y el usuario no ve nada brusco.
      if (!esErrorNoAutenticadoTiempoReal(error)) {
        registrarErrorConexionTiempoRealDev(error, contexto);
        return;
      }

      invalidandoSesion = true;
      await cerrarSesion();
      if (!desmontado) enrutador.replace("/");
    };

    const cargarSesionesInscritas = async () => {
      const sesiones = await listarSesionesDisponiblesApi(token, {});
      const candidatas = sesiones.filter((s) =>
        s.estado === "Activa" ||
        s.estado === "Pausada" ||
        s.estado === "EnPreparacion",
      );
      const detalles = await Promise.all(
        candidatas.map((s) =>
          obtenerDetalleSesionDisponibleApi(token, s.id).catch(() => null),
        ),
      );

      sesionesSuscritas = detalles
        .filter((d) => d?.participacionActual?.estaInscrito === true)
        .map((d) => ({
          sesionId: d!.id,
          equipoId: d!.participacionActual?.equipoId ?? null,
        }));
    };

    const unirseASesionesInscritas = async () => {
      await cargarSesionesInscritas();
      logDev(`sesiones inscritas encontradas: ${sesionesSuscritas.length}`);
      for (const item of sesionesSuscritas) {
        await conexion.invoke("UnirseASesion", item.sesionId)
          .then(() => logDev(`unido a sesión ${item.sesionId}`))
          .catch(manejarErrorConexion);
        if (item.equipoId) {
          await conexion.invoke("UnirseAEquipo", item.equipoId)
            .catch(manejarErrorConexion);
        }
      }
    };

    // Oculta el banner solo si corresponde a esa sesión (no pisa el de otra).
    const limpiarBanner = (sesionId: string) => {
      if (desmontado) return;
      setBannerEtapaPorComenzar((actual) =>
        actual?.sesionId === sesionId ? null : actual);
    };

    const navegarAEjecucionActual = async (sesionId: string) => {
      if (!sesionId) return;

      // Best-effort: cualquier fallo (no inscrito, red, expulsado) se ignora
      // silenciosamente; nunca debe propagarse como "uncaught in promise".
      try {
        // Primero el detalle: si la sesión no está Activa o el usuario NO está
        // inscrito, NO se consulta el progreso secuencial (ese endpoint rechaza
        // a los no inscritos) ni se redirige.
        const detalle = await obtenerDetalleSesionDisponibleApi(token, sesionId);
        if (
          detalle.estado !== "Activa" ||
          detalle.participacionActual?.estaInscrito !== true
        ) {
          limpiarBanner(sesionId);
          return;
        }

        const progreso = await obtenerProgresoSecuencialSesionApi(token, sesionId);

        // #16/#24: durante la preparación la etapa NO es jugable. NO navegar; se
        // reconstruye/mantiene el banner con la cuenta regresiva autoritativa.
        if (progreso.faseEtapaActual === FASE_PREPARACION) {
          const banner = bannerDesdeProgreso(sesionId, progreso);
          if (banner && !desmontado) setBannerEtapaPorComenzar(banner);
          return;
        }

        // Etapa Activa (o sin etapa jugable aún): se oculta el banner de esa sesión.
        limpiarBanner(sesionId);

        if (
          progreso.jugadorActualCompletoEtapaActual === true ||
          !progreso.misionActualId ||
          !progreso.etapaActualId ||
          !progreso.modoDeJuegoId ||
          !progreso.tipoEtapaActual
        ) {
          return;
        }

        const destino =
          progreso.tipoEtapaActual === "Trivia"
            ? `/participante/sesiones/jugar?sesionId=${sesionId}` +
              `&misionId=${progreso.misionActualId}` +
              `&etapaId=${progreso.etapaActualId}` +
              `&triviaId=${progreso.modoDeJuegoId}`
            : `/participante/sesiones/tesoro?sesionId=${sesionId}` +
              `&misionId=${progreso.misionActualId}` +
              `&etapaId=${progreso.etapaActualId}` +
              `&busquedaId=${progreso.modoDeJuegoId}`;

        enrutador.replace(destino);
      } catch {
        // Silencioso a propósito (best-effort).
      }
    };

    const manejarParticipanteExpulsado = () => {
      Alert.alert("Sesion", "Fuiste expulsado de esta sesion.");
      enrutador.replace("/participante/sesiones");
    };

    const manejarEquipoExpulsado = () => {
      Alert.alert("Equipo expulsado", "Tu equipo fue expulsado de la sesion.");
      enrutador.replace("/participante/sesiones");
    };

    // #24: EtapaPorComenzar solo AVISA (banner). No navega a una etapa jugable.
    const manejarEtapaPorComenzar = (evento: EventoEtapaPorComenzar) => {
      if (desmontado) return;
      logDev("EtapaPorComenzar recibida");
      const banner = mapearEtapaPorComenzar(evento);
      if (banner) {
        logDev(`banner creado: ${banner.mensaje} (sesion ${banner.sesionId})`);
        setBannerEtapaPorComenzar(banner);
      } else {
        logDev("EtapaPorComenzar: mapping devolvió null (payload incompleto)");
      }
    };

    // #4/#10: fallback si se perdió EtapaPorComenzar (reconexión, unión tardía al
    // grupo, background). Al cerrar la etapa, se reconstruye el banner por HTTP.
    // Reintento breve y acotado por si el GET llega antes de ver el estado
    // persistido (carrera EtapaCompletada/EtapaPorComenzar). Máximo 3 intentos.
    const manejarEtapaCompletada = (evento: EventoSesionTiempoReal) => {
      const sesionId = obtenerSesionIdEvento(evento);
      if (!sesionId || desmontado) return;
      logDev(`EtapaCompletada recibida ${sesionId}`);
      let intentos = 0;
      const intentar = () => {
        if (desmontado || intentos >= 3) return;
        intentos += 1;
        void navegarAEjecucionActual(sesionId).catch(() => undefined);
        if (intentos < 3) setTimeout(intentar, 400);
      };
      intentar();
    };

    // #22: EtapaIniciada oculta el banner y recién entonces navega al juego.
    const manejarEtapaIniciada = (evento: EventoSesionTiempoReal) => {
      const sesionId = obtenerSesionIdEvento(evento);
      logDev("EtapaIniciada recibida; banner ocultado");
      limpiarBanner(sesionId);
      void navegarAEjecucionActual(sesionId).catch(() => undefined);
    };

    // Abre el resultado/historial final de la sesión conservando el modo REAL
    // (Grupal/Individual): el error ocurría precisamente en grupal, así que no se
    // asume Individual si el detalle estaba disponible.
    const irAlResultadoFinalizacion = (
      sesionId: string,
      nombre: string,
      modo: string,
    ) => {
      enrutador.push({
        pathname: "/participante/historial/[id]",
        params: { id: sesionId, nombre, modo },
      });
    };

    // Aviso global de finalización: garantiza que TODO participante inscrito
    // (incluidos los grupales que terminaron antes y están esperando en otra
    // pantalla) reciba el aviso, sin refrescar manualmente. NO navega a una nueva
    // etapa (la sesión ya terminó).
    const manejarSesionFinalizada = async (sesionId: string) => {
      limpiarBanner(sesionId);

      // Modo/nombre reales para abrir el resultado correcto (NO asumir Individual).
      let nombre = "";
      let modo = "Individual";
      try {
        const detalle = await obtenerDetalleSesionDisponibleApi(token, sesionId);
        nombre = detalle.nombre ?? "";
        modo = detalle.modo ?? "Individual";
      } catch {
        // Detalle no disponible: se muestra el aviso igualmente (no se pierde la
        // notificación), con el fallback mínimo de modo.
      }
      if (desmontado) return;

      // Dedup compartida con Detalle/Trivia/Tesoro: una sola alerta por sesión.
      // Se marca DESPUÉS de resolver el detalle para ceder ante una pantalla que
      // ya mostró el aviso de forma síncrona con el modo real (p. ej. el Detalle).
      if (!intentarMarcarFinalizacionNotificada(sesionId)) {
        logDev(`SesionActualizada Estado=Finalizada SesionId=${sesionId}: aviso omitido por duplicado`);
        return;
      }

      logDev(`SesionActualizada Estado=Finalizada SesionId=${sesionId}: aviso de finalización mostrado (modo ${modo})`);
      Alert.alert(
        "La sesión finalizó",
        "Puedes revisar tus resultados y el ranking final.",
        [
          {
            text: "Ver resultado",
            onPress: () => irAlResultadoFinalizacion(sesionId, nombre, modo),
          },
        ],
      );
    };

    const manejarSesionActualizada = (evento: EventoSesionTiempoReal) => {
      const sesionId = obtenerSesionIdEvento(evento);
      if (!sesionId) return;
      const estado = obtenerEstadoEvento(evento);

      // Solo Finalizada dispara el aviso final; nunca se infiere por otro evento.
      if (estado === "Finalizada") {
        void manejarSesionFinalizada(sesionId);
        return;
      }

      // Activa/Pausada/EnPreparacion (y demás): comportamiento actual intacto.
      void cargarSesionesInscritas()
        .then(() => navegarAEjecucionActual(sesionId))
        .catch(() => undefined);
    };

    const cerrarConexion = async () => {
      if (cerrando) return;
      cerrando = true;
      logDev("cerrando");

      await inicioPromise?.catch(() => undefined);

      if (conexion.state === signalR.HubConnectionState.Connected) {
        await Promise.all(sesionesSuscritas.flatMap((item) => [
          conexion.invoke("SalirDeSesion", item.sesionId).catch(() => undefined),
          item.equipoId
            ? conexion.invoke("SalirDeEquipo", item.equipoId).catch(() => undefined)
            : Promise.resolve(),
        ]));
      }

      if (conexion.state !== signalR.HubConnectionState.Disconnected) {
        await conexion.stop().catch(() => undefined);
      }
      if (conexion.state === signalR.HubConnectionState.Disconnected) {
        registrarCerrado();
      }
    };

    conexion.on("ParticipanteExpulsadoSesion", manejarParticipanteExpulsado);
    conexion.on("EquipoExpulsadoSesion", manejarEquipoExpulsado);
    conexion.on("EtapaPorComenzar", manejarEtapaPorComenzar);
    conexion.on("EtapaCompletada", manejarEtapaCompletada);
    conexion.on("EtapaIniciada", manejarEtapaIniciada);
    conexion.on("SesionActualizada", manejarSesionActualizada);

    // #4/#30-9: al volver del background, reconstruir el banner/estado real por
    // HTTP (no depende de un evento perdido mientras la app estuvo suspendida).
    const suscripcionAppState = AppState.addEventListener("change", (estado) => {
      if (estado !== "active" || desmontado) return;
      logDev("AppState active; reconstruyendo estado autoritativo");
      for (const item of sesionesSuscritas) {
        void navegarAEjecucionActual(item.sesionId).catch(() => undefined);
      }
    });

    // #8: si el participante ingresa a una sesión/equipo DESPUÉS de que la
    // conexión global ya arrancó, ésta no estaría en su GrupoSesion (perdería
    // EtapaPorComenzar/EtapaIniciada). Al notificarse una nueva membresía, la
    // MISMA conexión re-sincroniza sus grupos (UnirseASesion/UnirseAEquipo) y
    // reconstruye el estado autoritativo. No se crea otra HubConnection.
    const desuscribirMembresia = suscribirMembresiaTiempoReal(() => {
      if (desmontado) return;
      logDev("membresía actualizada; re-sincronizando grupos");
      void unirseASesionesInscritas()
        .then(() => {
          for (const item of sesionesSuscritas) {
            void navegarAEjecucionActual(item.sesionId).catch(() => undefined);
          }
        })
        .catch(() => undefined);
    });

    conexion.onreconnecting((error) => {
      logDev("reconectando");
      void manejarErrorConexion(error, "reconectando");
    });

    conexion.onreconnected(() => {
      logDev("reconectado");
      void unirseASesionesInscritas()
        .then(() => {
          const primeraSesion = sesionesSuscritas[0]?.sesionId;
          if (primeraSesion) void navegarAEjecucionActual(primeraSesion);
        })
        .catch(() => undefined);
    });

    conexion.onclose((error) => {
      registrarCerrado();
      void manejarErrorConexion(error, "onclose");
    });

    inicioPromise = conexion.start();
    void inicioPromise
      .then(async () => {
        logDev("conectado");
        await unirseASesionesInscritas();
        if (desmontado) await cerrarConexion();
      })
      .catch((error: unknown) => {
        if (desmontado) return;
        void manejarErrorConexion(error, "start");
      });

    return () => {
      desmontado = true;
      suscripcionAppState.remove();
      desuscribirMembresia();
      conexion.off("ParticipanteExpulsadoSesion", manejarParticipanteExpulsado);
      conexion.off("EquipoExpulsadoSesion", manejarEquipoExpulsado);
      conexion.off("EtapaPorComenzar", manejarEtapaPorComenzar);
      conexion.off("EtapaCompletada", manejarEtapaCompletada);
      conexion.off("EtapaIniciada", manejarEtapaIniciada);
      conexion.off("SesionActualizada", manejarSesionActualizada);
      void cerrarConexion();
    };
  }, [
    token,
    cargandoSesion,
    estaAutenticado,
    cerrarSesion,
    enrutador,
  ]);

  // #13: fallback si se pierde EtapaIniciada. Mientras hay banner, al llegar la
  // fecha programada (+ margen técnico) se consulta el progreso HTTP; si la fase
  // ya es "Activa", se navega igualmente (backend sigue siendo la autoridad, no
  // el reloj local). Reintentos limitados. Al llegar EtapaIniciada se limpia el
  // banner → este efecto se re-ejecuta con banner null y cancela su timer (evita
  // doble navegación).
  useEffect(() => {
    const banner = bannerEtapaPorComenzar;
    if (!banner || !token) return;
    const objetivoMs = Date.parse(banner.fechaInicioProgramadaUtc);
    if (Number.isNaN(objetivoMs)) return;

    let cancelado = false;
    let intentos = 0;
    const MAX_INTENTOS = 5;
    let timer: ReturnType<typeof setTimeout>;

    const chequear = async () => {
      if (cancelado) return;
      intentos += 1;
      try {
        const progreso = await obtenerProgresoSecuencialSesionApi(token, banner.sesionId);
        if (cancelado) return;
        if (progreso.faseEtapaActual === "Activa") {
          setBannerEtapaPorComenzar((actual) =>
            actual?.sesionId === banner.sesionId ? null : actual);
          const destino = construirDestinoJuego(banner.sesionId, progreso);
          if (destino) enrutador.replace(destino);
          return;
        }
      } catch {
        // Se reintenta abajo (con límite).
      }
      if (!cancelado && intentos < MAX_INTENTOS) {
        timer = setTimeout(() => void chequear(), 700);
      }
    };

    // Empezar un pequeño margen DESPUÉS de la fecha objetivo (no navegar por reloj).
    const margenMs = Math.max(0, objetivoMs - Date.now()) + 400;
    timer = setTimeout(() => void chequear(), margenMs);
    return () => {
      cancelado = true;
      clearTimeout(timer);
    };
  }, [bannerEtapaPorComenzar, token, enrutador]);

  return { bannerEtapaPorComenzar };
}
