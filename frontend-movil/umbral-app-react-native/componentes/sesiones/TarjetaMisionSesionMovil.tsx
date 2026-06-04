import { StyleSheet, Text, View } from "react-native";
import { tema } from "../../estilos/tema";
import { formatearSegundos } from "../../utilidades/formatoTiempo";
import type {
  EtapaSesionMovilDto,
  MisionSesionMovilDto,
} from "../../tipos/sesiones";

interface Props {
  mision: MisionSesionMovilDto;
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

export function TarjetaMisionSesionMovil({ mision }: Props) {
  const sinDatos = !mision.nombre?.trim();

  return (
    <View style={estilos.tarjeta}>
      <View style={estilos.encabezado}>
        <View style={estilos.indiceCircular}>
          <Text style={estilos.indiceTexto}>{mision.orden}</Text>
        </View>
        <View style={estilos.encabezadoTexto}>
          <Text style={estilos.titulo} numberOfLines={2}>
            {sinDatos ? "Misión sin información disponible" : mision.nombre}
          </Text>
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
                <Text style={estilos.etapaModo}>{describirModoJuego(etapa)}</Text>
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
  titulo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
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
  etapaModo: {
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
