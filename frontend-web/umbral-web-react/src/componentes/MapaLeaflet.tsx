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

export interface MarcadorMapa {
  id: string
  latitud: number
  longitud: number
  etiqueta?: string
  color?: string
}

interface Props {
  latitud?: number
  longitud?: number
  onChange?: (lat: number, lng: number) => void
  alto?: number
  marcadores?: MarcadorMapa[]
}

// Centro por defecto: Lima, Perú
const CENTRO_DEFAULT: [number, number] = [-12.0464, -77.0428]
const ZOOM_MUNDO = 3
const ZOOM_DETALLE = 14

export function MapaLeaflet({ latitud, longitud, onChange, alto = 320, marcadores = [] }: Props) {
  const refContenedor = useRef<HTMLDivElement>(null)
  const refMapa = useRef<L.Map | null>(null)
  const refMarcador = useRef<L.Marker | null>(null)
  const refMarcadoresDinamicos = useRef<Map<string, L.Marker>>(new Map())

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

  // Actualizar marcadores dinámicos (participantes en tiempo real)
  useEffect(() => {
    if (!refMapa.current) return
    const mapa = refMapa.current
    const actuales = refMarcadoresDinamicos.current
    const idsNuevos = new Set(marcadores.map(m => m.id))

    // Eliminar los que ya no están
    for (const [id, marker] of actuales) {
      if (!idsNuevos.has(id)) { marker.remove(); actuales.delete(id) }
    }

    // Añadir o mover
    for (const m of marcadores) {
      const icono = L.divIcon({
        className: '',
        html: `<div style="background:${m.color ?? '#3b82f6'};width:12px;height:12px;border-radius:50%;border:2px solid #fff;box-shadow:0 1px 3px rgba(0,0,0,.4)"></div>`,
        iconSize: [12, 12],
        iconAnchor: [6, 6]
      })
      if (actuales.has(m.id)) {
        actuales.get(m.id)!.setLatLng([m.latitud, m.longitud])
      } else {
        const marker = L.marker([m.latitud, m.longitud], { icon: icono })
          .bindTooltip(m.etiqueta ?? '', { permanent: false, direction: 'top' })
          .addTo(mapa)
        actuales.set(m.id, marker)
      }
    }
  }, [marcadores])

  return (
    <div style={{ borderRadius: 8, overflow: 'hidden', border: '1px solid var(--color-borde-tarjeta)' }}>
      <div ref={refContenedor} style={{ height: alto, width: '100%' }} />
    </div>
  )
}
