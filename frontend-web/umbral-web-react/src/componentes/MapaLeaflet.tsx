import { useEffect, useRef } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'

// Fix default Leaflet marker icons when bundled with Vite
void (() => {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  delete (L.Icon.Default.prototype as any)._getIconUrl
  L.Icon.Default.mergeOptions({
    iconUrl: new URL('leaflet/dist/images/marker-icon.png', import.meta.url).href,
    iconRetinaUrl: new URL('leaflet/dist/images/marker-icon-2x.png', import.meta.url).href,
    shadowUrl: new URL('leaflet/dist/images/marker-shadow.png', import.meta.url).href,
  })
})()

interface Props {
  latitud?: number
  longitud?: number
  onChange?: (lat: number, lng: number) => void
  alto?: number
}

// Centro por defecto: Lima, Perú
const CENTRO_DEFAULT: [number, number] = [-12.0464, -77.0428]
const ZOOM_MUNDO = 3
const ZOOM_DETALLE = 14

export function MapaLeaflet({ latitud, longitud, onChange, alto = 320 }: Props) {
  const refContenedor = useRef<HTMLDivElement>(null)
  const refMapa = useRef<L.Map | null>(null)
  const refMarcador = useRef<L.Marker | null>(null)

  const hayCoords = latitud != null && longitud != null

  useEffect(() => {
    if (!refContenedor.current || refMapa.current) return

    const centro: [number, number] = hayCoords ? [latitud!, longitud!] : CENTRO_DEFAULT
    const zoom = hayCoords ? ZOOM_DETALLE : ZOOM_MUNDO

    const mapa = L.map(refContenedor.current, { zoomControl: true }).setView(centro, zoom)
    refMapa.current = mapa

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(mapa)

    if (hayCoords) {
      refMarcador.current = L.marker([latitud!, longitud!]).addTo(mapa)
    }

    if (onChange) {
      mapa.on('click', (e: L.LeafletMouseEvent) => {
        const { lat, lng } = e.latlng
        if (refMarcador.current) {
          refMarcador.current.setLatLng([lat, lng])
        } else {
          refMarcador.current = L.marker([lat, lng]).addTo(mapa)
        }
        onChange(lat, lng)
      })
    }

    return () => {
      mapa.remove()
      refMapa.current = null
      refMarcador.current = null
    }
  }, []) // solo al montar

  // Actualizar marcador si los props cambian (ej: edición de pista existente)
  useEffect(() => {
    if (!refMapa.current) return
    if (latitud != null && longitud != null) {
      if (refMarcador.current) {
        refMarcador.current.setLatLng([latitud, longitud])
      } else {
        refMarcador.current = L.marker([latitud, longitud]).addTo(refMapa.current)
      }
      refMapa.current.setView([latitud, longitud], ZOOM_DETALLE)
    }
  }, [latitud, longitud])

  return (
    <div style={{ borderRadius: 8, overflow: 'hidden', border: '1px solid var(--color-borde-tarjeta)' }}>
      <div ref={refContenedor} style={{ height: alto, width: '100%' }} />
    </div>
  )
}
