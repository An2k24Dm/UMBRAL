import { StyleSheet, Text, View } from "react-native";
import { tema } from "../../estilos/tema";
import { formatearSegundos } from "../../utilidades/formatoTiempo";
import type {
  EtapaSesionMovilDto,
  MisionSesionMovilDto,
} from "../../tipos/sesiones";

interface Props {
  mision: MisionSesionMovilDto;
  // Etapas completadas GLOBALMENTE (por todos). Autoridad del backend.
  etapasCompletadasGlobalmenteIds?: ReadonlySet<string>;
}

// Tarjeta de una misión dentro del detalle. Lista las etapas con su
// modo de juego ("Trivia" / "Búsqueda del tesoro") y el tiempo
// estimado formateado.
const ETIQUETAS_MODO_JUEGO: Record<string, string> = {
  Trivia: "Trivia",
  BusquedaDelTesoro: "Búsqueda del tesoro",
};

function describirModoJuego(etapa: EtapaSesionMovilDto): string {
  // El backend manda discriminador + nombre amigable. Si conocemos
  // el discriminador, usamos la etiqueta canónica; si no, caemos al
  // nombre amigable enviado por juegos-servicio.
  const etiqueta = ETIQUETAS_MODO_JUEGO[etapa.tipoModoDeJuego];
  if (etiqueta) return etiqueta;
  if (etapa.nombreModoDeJuego?.trim().length > 0) return etapa.nombreModoDeJuego;
  return etapa.tipoModoDeJuego;
}

export function TarjetaMisionSesionMovil({
  mision,
  etapasCompletadasGlobalmenteIds,
}: Props) {
  const sinDatos = !mision.nombre?.trim();
  const completadas = etapasCompletadasGlobalmenteIds ?? new Set<string>();

  const etapaCompletada = (etapaId: string) => completadas.has(etapaId);
  // Una misión está COMPLETADA solo cuando TODAS sus etapas lo están.
  const misionCompletada =
    mision.etapas.length > 0 &&
    mision.etapas.every((etapa) => completadas.has(etapa.id));

  return (
    <View style={estilos.tarjeta}>
      <View style={estilos.encabezado}>
        <View style={estilos.indiceCircular}>
          <Text style={estilos.indiceTexto}>{mision.orden}</Text>
        </View>
        <View style={estilos.encabezadoTexto}>
          <View style={estilos.tituloFila}>
            <Text style={estilos.titulo} numberOfLines={2}>
              {sinDatos ? "Misión sin información disponible" : mision.nombre}
            </Text>
            {misionCompletada && <PastillaCompletada />}
          </View>
          {!sinDatos && mision.descripcion?.trim().length > 0 && (
            <Text style={estilos.descripcion} numberOfLines={3}>
              {mision.descripcion}
            </Text>
          )}
        </View>
      </View>

      <View style={estilos.filaMeta}>
        {mision.dificultad && (
          <MetaPill etiqueta={`Dificultad: ${mision.dificultad}`} />
        )}
        <MetaPill etiqueta={`${mision.totalEtapas} etapa${mision.totalEtapas === 1 ? "" : "s"}`} />
      </View>

      {mision.etapas.length > 0 ? (
        <View style={estilos.listaEtapas}>
          {mision.etapas.map((etapa) => (
            <View key={etapa.id} style={estilos.filaEtapa}>
              <Text style={estilos.etapaIndice}>{etapa.orden}.</Text>
              <View style={estilos.etapaCuerpo}>
                <View style={estilos.etapaFilaTitulo}>
                  <Text style={estilos.etapaModo}>{describirModoJuego(etapa)}</Text>
                  {etapaCompletada(etapa.id) && <PastillaCompletada />}
                </View>
                <Text style={estilos.etapaTiempo}>
                  Tiempo estimado: {formatearSegundos(etapa.tiempoEstimadoSegundos)}
                </Text>
              </View>
            </View>
          ))}
        </View>
      ) : (
        <Text style={estilos.sinEtapas}>Esta misión no tiene etapas registradas.</Text>
      )}
    </View>
  );
}

function MetaPill({ etiqueta }: { etiqueta: string }) {
  return (
    <View style={estilos.metaPill}>
      <Text style={estilos.metaPillTexto}>{etiqueta}</Text>
    </View>
  );
}

// Pastilla pequeña y no invasiva de estado COMPLETADA (verde suave).
function PastillaCompletada() {
  return (
    <View style={estilos.pastillaCompletada}>
      <Text style={estilos.pastillaCompletadaTexto}>COMPLETADA</Text>
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
  encabezado: {
    flexDirection: "row",
    alignItems: "flex-start",
    gap: tema.espacios.md,
  },
  indiceCircular: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: "rgba(124,92,255,0.18)",
    borderWidth: 1,
    borderColor: tema.colores.primario,
    alignItems: "center",
    justifyContent: "center",
  },
  indiceTexto: {
    color: tema.colores.enlace,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  encabezadoTexto: { flex: 1 },
  tituloFila: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: tema.espacios.sm,
  },
  titulo: {
    flex: 1,
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
  },
  pastillaCompletada: {
    paddingHorizontal: tema.espacios.sm,
    paddingVertical: 2,
    borderRadius: tema.radios.pastilla,
    borderWidth: 1,
    borderColor: "#34d399",
    backgroundColor: "#d1fae5",
  },
  pastillaCompletadaTexto: {
    color: "#065f46",
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.bold,
    letterSpacing: tema.tipografia.espaciadoLetra.xs,
  },
  descripcion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
    lineHeight: 18,
  },
  filaMeta: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: tema.espacios.sm,
    marginTop: tema.espacios.sm,
  },
  metaPill: {
    paddingHorizontal: tema.espacios.sm,
    paddingVertical: 2,
    borderRadius: tema.radios.pastilla,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.entradaFondo,
  },
  metaPillTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.xs,
  },
  listaEtapas: {
    marginTop: tema.espacios.md,
    paddingTop: tema.espacios.sm,
    borderTopWidth: 1,
    borderTopColor: tema.colores.bordeTarjeta,
    gap: tema.espacios.sm,
  },
  filaEtapa: {
    flexDirection: "row",
    alignItems: "flex-start",
    gap: tema.espacios.sm,
  },
  etapaIndice: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    fontWeight: tema.tipografia.pesos.bold,
    minWidth: 18,
  },
  etapaCuerpo: { flex: 1 },
  etapaFilaTitulo: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: tema.espacios.sm,
  },
  etapaModo: {
    flex: 1,
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  etapaTiempo: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: 2,
  },
  sinEtapas: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.md,
    fontStyle: "italic",
  },
});
