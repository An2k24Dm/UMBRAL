import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { tema } from "../../estilos/tema";
import { formatearFechaHora } from "../../utilidades/formatoFechas";
import type { SesionDisponibleMovilDto } from "../../tipos/sesiones";
import { BadgeEstadoSesionMovil } from "./BadgeEstadoSesionMovil";
import { BadgeModoSesionMovil } from "./BadgeModoSesionMovil";

interface Props {
  sesion: SesionDisponibleMovilDto;
  alPresionar: () => void;
}

// Tarjeta de una sesión en el listado. Pensada para tocar y abrir el
// detalle: nada en ella ofrece acciones administrativas (editar,
// finalizar, etc.).
export function TarjetaSesionMovil({ sesion, alPresionar }: Props) {
  // El backend solo expone capacidad para el subtipo correspondiente.
  // Si los campos están definidos, los renderizamos como "X / Y";
  // si son null/undefined, mostramos el dato disponible o nada.
  const capacidad = construirTextoCapacidad(sesion);

  return (
    <TouchableOpacity
      onPress={alPresionar}
      activeOpacity={0.75}
      style={estilos.tarjeta}
      accessibilityRole="button"
      accessibilityLabel={`Abrir detalle de la sesión ${sesion.nombre}`}
    >
      <View style={estilos.filaBadges}>
        <BadgeModoSesionMovil modo={sesion.modo} />
        <BadgeEstadoSesionMovil estado={sesion.estado} />
      </View>

      <Text style={estilos.nombre} numberOfLines={2}>
        {sesion.nombre}
      </Text>

      {sesion.descripcion?.trim().length > 0 && (
        <Text style={estilos.descripcion} numberOfLines={2}>
          {sesion.descripcion}
        </Text>
      )}

      <View style={estilos.filaDatos}>
        <Dato etiqueta="Fecha" valor={formatearFechaHora(sesion.fechaProgramada)} />
        <Dato etiqueta="Misiones" valor={String(sesion.cantidadMisiones)} />
        {capacidad && (
          <Dato
            etiqueta={sesion.modo === "Individual" ? "Cupo" : "Equipos"}
            valor={capacidad}
          />
        )}
      </View>
    </TouchableOpacity>
  );
}

function construirTextoCapacidad(sesion: SesionDisponibleMovilDto): string | null {
  if (sesion.modo === "Individual") {
    const actuales = sesion.cantidadParticipantesActuales ?? null;
    const maximo = sesion.capacidadMaximaParticipantes ?? null;
    if (actuales !== null && maximo !== null) return `${actuales} / ${maximo}`;
    if (maximo !== null) return `máx. ${maximo}`;
    return null;
  }
  if (sesion.modo === "Grupal") {
    const actuales = sesion.cantidadEquiposActuales ?? null;
    const maximo = sesion.capacidadMaximaEquipos ?? null;
    if (actuales !== null && maximo !== null) return `${actuales} / ${maximo}`;
    if (maximo !== null) return `máx. ${maximo}`;
    return null;
  }
  return null;
}

function Dato({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return (
    <View style={estilos.dato}>
      <Text style={estilos.datoEtiqueta}>{etiqueta}</Text>
      <Text style={estilos.datoValor}>{valor}</Text>
    </View>
  );
}

const estilos = StyleSheet.create({
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.md,
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
    fontSize: tema.tipografia.tamanos.h4,
    fontWeight: tema.tipografia.pesos.bold,
  },
  descripcion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
    lineHeight: 18,
  },
  filaDatos: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: tema.espacios.md,
    marginTop: tema.espacios.sm,
  },
  dato: { minWidth: 90 },
  datoEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.xs,
    textTransform: "uppercase",
  },
  datoValor: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.sm,
    fontWeight: tema.tipografia.pesos.semibold,
    marginTop: 2,
  },
});
