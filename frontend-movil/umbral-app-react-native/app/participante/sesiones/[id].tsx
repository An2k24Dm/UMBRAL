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

  const navegarSeguro = useNavegacionSegura();
  useRefrescarAlEnfocar(refrescar);
  useSesionesTiempoReal({
    sesionId,
    onParticipantesSesionActualizados: refrescar,
    onEquiposSesionActualizados: refrescar,
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

          <Text style={estilos.tituloSeccion}>MISIONES</Text>
          <ListaMisionesSesionMovil misiones={detalle.misiones} />

          <SeccionParticipacion
            detalle={detalle}
            sesionId={sesionId}
            enrutador={enrutador}
            navegarSeguro={navegarSeguro}
            refrescar={refrescar}
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
  const esActiva = detalle.estado === "Activa" || detalle.estado === "Pausada";

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
    if (participacion.tipo === "Equipo") {
      return (
        <View style={estilos.tarjetaParticipacion}>
          <Text style={estilos.participacionTexto}>
            Ya perteneces a un equipo en esta sesión.
          </Text>
          {participacion.equipoNombre ? (
            <Text style={estilos.participacionDetalle}>
              Equipo: {participacion.equipoNombre}
            </Text>
          ) : null}

          {esActiva && (
            <TouchableOpacity
              style={estilos.botonPrimario}
              onPress={() =>
                navegarSeguro(() =>
                  enrutador.push(
                    `/participante/sesiones/jugar?sesionId=${sesionId}`,
                  ),
                )
              }
              accessibilityRole="button"
            >
              <Text style={estilos.botonPrimarioTexto}>¡Jugar ahora!</Text>
            </TouchableOpacity>
          )}

          <TouchableOpacity
            style={esActiva ? estilos.botonSecundario : estilos.botonPrimario}
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
            <Text style={esActiva ? estilos.botonSecundarioTexto : estilos.botonPrimarioTexto}>
              Ver equipo
            </Text>
          </TouchableOpacity>
        </View>
      );
    }

    // Sesión individual ya ingresada.
    return (
      <View style={estilos.tarjetaParticipacion}>
        <Text style={estilos.participacionTexto}>
          Ya ingresaste a esta sesión.
        </Text>

        {esActiva && (
          <TouchableOpacity
            style={estilos.botonPrimario}
            onPress={() =>
              navegarSeguro(() =>
                enrutador.push(
                  `/participante/sesiones/jugar?sesionId=${sesionId}`,
                ),
              )
            }
            accessibilityRole="button"
          >
            <Text style={estilos.botonPrimarioTexto}>¡Jugar ahora!</Text>
          </TouchableOpacity>
        )}

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
