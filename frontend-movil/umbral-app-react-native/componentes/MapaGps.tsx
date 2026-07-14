import { StyleSheet, View } from "react-native";
import WebView from "react-native-webview";

interface Props {
  latitud: number;
  longitud: number;
  alto?: number;
}

function generarHtmlMapa(lat: number, lng: number): string {
  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"/>
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
  <style>
    html, body { margin: 0; padding: 0; height: 100%; background: #1a1a2e; }
    #mapa { height: 100%; width: 100%; }
  </style>
</head>
<body>
  <div id="mapa"></div>
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
  <script>
    var mapa = L.map('mapa', { zoomControl: true, attributionControl: false })
      .setView([${lat}, ${lng}], 16);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19
    }).addTo(mapa);
    L.marker([${lat}, ${lng}]).addTo(mapa)
      .bindPopup('<b>Ubicaci&oacute;n del tesoro</b><br>${lat.toFixed(6)}, ${lng.toFixed(6)}')
      .openPopup();
  </script>
</body>
</html>`;
}

export function MapaGps({ latitud, longitud, alto = 260 }: Props) {
  return (
    <View style={[estilos.contenedor, { height: alto }]}>
      <WebView
        source={{ html: generarHtmlMapa(latitud, longitud) }}
        style={estilos.webView}
        scrollEnabled={false}
        javaScriptEnabled
        originWhitelist={["*"]}
        mixedContentMode="always"
      />
    </View>
  );
}

const estilos = StyleSheet.create({
  contenedor: {
    borderRadius: 10,
    overflow: "hidden",
    borderWidth: 1,
    borderColor: "rgba(255,255,255,0.12)",
  },
  webView: {
    flex: 1,
    backgroundColor: "transparent",
  },
});
