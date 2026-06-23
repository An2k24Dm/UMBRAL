import { useEffect } from "react";
import {
  ActivityIndicator,
  Alert,
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
import { useDetalleSesionDisponible } from "../../../hooks/useDetalleSesionDisponible";
import type { SesionDetalleMovilDto } from "../../../tipos/sesiones";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";

// HU — Detalle de una sesión disponible. Solo consulta: el Participante
// puede ver misiones, etapas, modo de juego y dificultad. No hay acciones
// para inscribirse en esta iteración del ERS.
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
    <PantallaBase>
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
}: {
  detalle: SesionDetalleMovilDto;
  sesionId: string;
  enrutador: ReturnType<typeof useRouter>;
}) {
  const participacion = detalle.participacionActual;
  const esGrupal = detalle.modo === "Grupal";
  const enPreparacion = detalle.estado === "EnPreparacion";

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
          <TouchableOpacity
            style={estilos.botonPrimario}
            onPress={() =>
              enrutador.push(
                `/participante/sesiones/equipo?sesionId=${sesionId}` +
                  `&equipoId=${participacion.equipoId ?? ""}`,
              )
            }
            accessibilityRole="button"
          >
            <Text style={estilos.botonPrimarioTexto}>Ver equipo</Text>
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

  const alPresionarUnirse = () => {
    if (esGrupal) {
      enrutador.push(
        `/participante/sesiones/unirse?sesionId=${sesionId}` +
          `&nombre=${encodeURIComponent(detalle.nombre)}`,
      );
      return;
    }
    // El ingreso individual se implementará en una historia futura.
    Alert.alert(
      "Unirse a la sesión",
      "El ingreso a sesiones individuales se implementará próximamente.",
    );
  };

  return (
    <TouchableOpacity
      style={estilos.botonPrimario}
      onPress={alPresionarUnirse}
      accessibilityRole="button"
    >
      <Text style={estilos.botonPrimarioTexto}>Unirse</Text>
    </TouchableOpacity>
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
