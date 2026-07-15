import { useEffect, useRef } from "react";
import { StyleSheet, View } from "react-native";
import WebView from "react-native-webview";

export interface MarcadorMovil {
  id: string;
  latitud: number;
  longitud: number;
  nombre: string;
  color?: string;
}

interface Props {
  latitud: number;
  longitud: number;
  alto?: number;
  marcadores?: MarcadorMovil[];
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

    window._marcadores = {};

    window.actualizarMarcadores = function(lista) {
      var ids = {};
      lista.forEach(function(m) { ids[m.id] = true; });

      Object.keys(window._marcadores).forEach(function(id) {
        if (!ids[id]) {
          mapa.removeLayer(window._marcadores[id]);
          delete window._marcadores[id];
        }
      });

      lista.forEach(function(m) {
        var icono = L.divIcon({
          className: '',
          html: '<div style="background:' + (m.color || '#f97316') + ';width:14px;height:14px;border-radius:50%;border:2px solid #fff;box-shadow:0 1px 4px rgba(0,0,0,.5)"></div>',
          iconSize: [14, 14],
          iconAnchor: [7, 7]
        });
        if (window._marcadores[m.id]) {
          window._marcadores[m.id].setLatLng([m.latitud, m.longitud]);
        } else {
          window._marcadores[m.id] = L.marker([m.latitud, m.longitud], { icon: icono })
            .bindTooltip(m.nombre, { permanent: false, direction: 'top' })
            .addTo(mapa);
        }
      });
    };
  </script>
</body>
</html>`;
}

export function MapaGps({ latitud, longitud, alto = 260, marcadores = [] }: Props) {
  const refWebView = useRef<WebView>(null);
  const listoCargado = useRef(false);
  const marcadoresPendientes = useRef<MarcadorMovil[] | null>(null);

  useEffect(() => {
    if (!listoCargado.current) {
      marcadoresPendientes.current = marcadores;
      return;
    }
    const js = `window.actualizarMarcadores(${JSON.stringify(marcadores)}); true;`;
    refWebView.current?.injectJavaScript(js);
  }, [marcadores]);

  const alCargar = () => {
    listoCargado.current = true;
    if (marcadoresPendientes.current && marcadoresPendientes.current.length > 0) {
      const js = `window.actualizarMarcadores(${JSON.stringify(marcadoresPendientes.current)}); true;`;
      refWebView.current?.injectJavaScript(js);
      marcadoresPendientes.current = null;
    }
  };

  return (
    <View style={[estilos.contenedor, { height: alto }]}>
      <WebView
        ref={refWebView}
        source={{ html: generarHtmlMapa(latitud, longitud) }}
        style={estilos.webView}
        scrollEnabled={false}
        javaScriptEnabled
        originWhitelist={["*"]}
        mixedContentMode="always"
        onLoadEnd={alCargar}
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
