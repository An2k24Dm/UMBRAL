import { useCallback, useEffect, useState } from "react";
import {
  ActivityIndicator,
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
import { useEquiposSesion } from "../../../hooks/useEquiposSesion";
import { useIngresarEquipo } from "../../../hooks/useIngresarEquipo";
import { useNavegacionSegura } from "../../../hooks/useNavegacionSegura";
import { useRefrescarAlEnfocar } from "../../../hooks/useRefrescarAlEnfocar";
import { useSesionesTiempoReal } from "../../../hooks/useSesionesTiempoReal";
import type { EquipoSesionListado } from "../../../tipos/equipos";

// HU43/HU47 — Listado de equipos de una sesión grupal. Permite ver el
// detalle de cada equipo e ingresar a un equipo disponible (con contraseña
// si es privado).
export default function PantallaEquiposSesion() {
  return (
    <RutaProtegidaMovil>
      <Contenido />
    </RutaProtegidaMovil>
  );
}

function Contenido() {
  const enrutador = useRouter();
  const { cerrarSesion } = useAutenticacion();
  const parametros = useLocalSearchParams<{ sesionId?: string; nombre?: string }>();
  const sesionId = parametros.sesionId ?? "";
  const nombre = parametros.nombre ?? "";

  const { equipos, cargando, error, sesionExpirada, refrescar } =
    useEquiposSesion(sesionId);

  const navegarSeguro = useNavegacionSegura();
  useRefrescarAlEnfocar(refrescar);
  // El aviso dirigido de expulsión de equipo lo maneja de forma global
  // useAvisosSesionTiempoReal; aquí solo refrescamos por eventos de grupo.
  useSesionesTiempoReal({
    origen: "Equipo",
    sesionId,
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

  // HU47 — Ingreso a equipo desde el listado.
  const {
    ingresando,
    error: errorIngreso,
    sesionExpirada: sesionExpiradaIngreso,
    ingresarEquipo,
    limpiarError,
  } = useIngresarEquipo();
  const [equipoPrivadoSeleccionado, setEquipoPrivadoSeleccionado] =
    useState<EquipoSesionListado | null>(null);

  useEffect(() => {
    if (sesionExpirada || sesionExpiradaIngreso)
      cerrarSesion().finally(() => enrutador.replace("/"));
  }, [sesionExpirada, sesionExpiradaIngreso, cerrarSesion, enrutador]);

  const yaTengoEquipo = equipos.some((e) => e.esMiEquipo);

  const verEquipo = (equipoId: string) =>
    navegarSeguro(() =>
      enrutador.push(
        `/participante/sesiones/equipo?sesionId=${sesionId}&equipoId=${equipoId}`,
      ),
    );

  // Tras ingresar: refresca el listado y lleva al detalle del equipo para
  // que el participante vea toda la información asociada.
  const finalizarIngreso = useCallback(
    async (equipoId: string) => {
      setEquipoPrivadoSeleccionado(null);
      await refrescar();
      navegarSeguro(() =>
        enrutador.replace(
          `/participante/sesiones/equipo?sesionId=${sesionId}&equipoId=${equipoId}`,
        ),
      );
    },
    [refrescar, navegarSeguro, enrutador, sesionId],
  );

  const ingresarAEquipo = async (equipo: EquipoSesionListado) => {
    limpiarError();
    if (equipo.tipo === "Privado") {
      // El modal pide la contraseña y luego llama al API.
      setEquipoPrivadoSeleccionado(equipo);
      return;
    }
    const respuesta = await ingresarEquipo(sesionId, equipo.id);
    if (respuesta) await finalizarIngreso(equipo.id);
  };

  const ingresarConContrasena = async (contrasena: string) => {
    if (!equipoPrivadoSeleccionado) return;
    const respuesta = await ingresarEquipo(
      sesionId,
      equipoPrivadoSeleccionado.id,
      contrasena,
    );
    if (respuesta) await finalizarIngreso(equipoPrivadoSeleccionado.id);
    // Si falló, el modal queda abierto mostrando errorIngreso.
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
        <Text style={estilos.titulo}>Equipos de la sesión</Text>
        <Text style={estilos.subtitulo}>
          Selecciona un equipo para ver su detalle.
        </Text>
        {nombre ? <Text style={estilos.nombreSesion}>{nombre}</Text> : null}
      </View>

      {cargando && (
        <View style={estilos.contenedorEstado}>
          <ActivityIndicator color={tema.colores.primario} size="large" />
          <Text style={estilos.textoEstado}>Cargando equipos…</Text>
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

      {!cargando && !error && (
        <View>
          {/* Errores de ingreso a equipos públicos (los privados se muestran
              dentro del modal de contraseña). */}
          {errorIngreso && !equipoPrivadoSeleccionado ? (
            <View style={estilos.cuadroError}>
              <Text style={estilos.cuadroErrorTexto}>{errorIngreso}</Text>
            </View>
          ) : null}

          {equipos.length === 0 ? (
            <View style={estilos.tarjeta}>
              <Text style={estilos.vacioTexto}>
                No hay equipos creados todavía.
              </Text>
            </View>
          ) : (
            equipos.map((equipo) => (
              <TarjetaEquipo
                key={equipo.id}
                equipo={equipo}
                puedeIngresar={!yaTengoEquipo && !equipo.estaLleno && !equipo.esMiEquipo}
                ingresando={ingresando}
                onVer={() => verEquipo(equipo.id)}
                onIngresar={() => void ingresarAEquipo(equipo)}
              />
            ))
          )}

          {!yaTengoEquipo && (
            <TouchableOpacity
              style={estilos.botonPrimario}
              onPress={() =>
                navegarSeguro(() =>
                  enrutador.push(
                    `/participante/sesiones/crear-equipo?sesionId=${sesionId}` +
                      `&nombre=${encodeURIComponent(nombre)}`,
                  ),
                )
              }
              accessibilityRole="button"
            >
              <Text style={estilos.botonPrimarioTexto}>Crear equipo</Text>
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

      {/* HU47 — Contraseña para equipos privados. */}
      <ModalContrasenaEquipo
        visible={equipoPrivadoSeleccionado !== null}
        nombreEquipo={equipoPrivadoSeleccionado?.nombre ?? ""}
        procesando={ingresando}
        error={errorIngreso}
        onIngresar={(contrasena) => void ingresarConContrasena(contrasena)}
        onCancelar={() => {
          setEquipoPrivadoSeleccionado(null);
          limpiarError();
        }}
      />
    </PantallaBase>
  );
}

function TarjetaEquipo({
  equipo,
  puedeIngresar,
  ingresando,
  onVer,
  onIngresar,
}: {
  equipo: EquipoSesionListado;
  puedeIngresar: boolean;
  ingresando: boolean;
  onVer: () => void;
  onIngresar: () => void;
}) {
  return (
    <View style={estilos.tarjeta}>
      <View style={estilos.filaTitulo}>
        <Text style={estilos.nombreEquipo}>{equipo.nombre}</Text>
        <View style={estilos.badges}>
          {equipo.esMiEquipo && (
            <Text style={[estilos.badge, estilos.badgePrimario]}>Tu equipo</Text>
          )}
          {equipo.soyLider && (
            <Text style={[estilos.badge, estilos.badgePrimario]}>Líder</Text>
          )}
        </View>
      </View>

      <Text style={estilos.metaLinea}>
        {equipo.tipo === "Privado" ? "Privado" : "Público"}
      </Text>
      <Text style={estilos.metaLinea}>
        Integrantes: {equipo.cantidadParticipantes} / {equipo.capacidadMaxima}
      </Text>
      <Text style={estilos.metaLinea}>Puntaje: {equipo.puntaje}</Text>
      <Text
        style={[
          estilos.estado,
          equipo.estaLleno ? estilos.estadoLleno : estilos.estadoDisponible,
        ]}
      >
        {equipo.estaLleno ? "Lleno" : "Disponible"}
      </Text>

      <TouchableOpacity
        style={estilos.botonVer}
        onPress={onVer}
        accessibilityRole="button"
      >
        <Text style={estilos.botonVerTexto}>
          {equipo.esMiEquipo ? "Ver equipo" : "Ver"}
        </Text>
      </TouchableOpacity>

      {/* HU47 — Ingresar solo si no tengo equipo y hay cupos. */}
      {puedeIngresar && (
        <TouchableOpacity
          style={[estilos.botonIngresar, ingresando && estilos.botonDeshabilitado]}
          onPress={onIngresar}
          disabled={ingresando}
          accessibilityRole="button"
        >
          {ingresando ? (
            <ActivityIndicator color={tema.colores.textoBlanco} />
          ) : (
            <Text style={estilos.botonIngresarTexto}>Ingresar</Text>
          )}
        </TouchableOpacity>
      )}
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
  subtitulo: {
    fontSize: tema.tipografia.tamanos.md,
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
  },
  nombreSesion: {
    fontSize: tema.tipografia.tamanos.sm,
    color: tema.colores.primario,
    marginTop: tema.espacios.sm,
    fontWeight: tema.tipografia.pesos.semibold,
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
    fontSize: tema.tipografia.tamanos.lg,
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
  estado: {
    marginTop: tema.espacios.sm,
    fontSize: tema.tipografia.tamanos.sm,
    fontWeight: tema.tipografia.pesos.bold,
  },
  estadoDisponible: { color: tema.colores.primario },
  estadoLleno: { color: tema.colores.error },
  botonVer: {
    marginTop: tema.espacios.md,
    paddingVertical: tema.espacios.sm,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    borderWidth: 1,
    borderColor: tema.colores.primario,
  },
  botonVerTexto: {
    color: tema.colores.primario,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  botonIngresar: {
    marginTop: tema.espacios.sm,
    paddingVertical: tema.espacios.sm,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    backgroundColor: tema.colores.primario,
  },
  botonIngresarTexto: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  botonDeshabilitado: { opacity: 0.7 },
  vacioTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
    textAlign: "center",
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
});
