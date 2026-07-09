import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { Boton } from '../componentes/Boton'
import { Alerta } from '../componentes/Alerta'
import { SelectorModoSesion } from '../componentes/sesiones/SelectorModoSesion'
import { SelectorMisiones } from '../componentes/sesiones/SelectorMisiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { obtenerSesion } from '../servicios/sesionesApi'
import {
  useEditarSesion,
  MIN_EQUIPOS,
  MIN_PARTICIPANTES_INDIVIDUAL,
  MIN_PARTICIPANTES_POR_EQUIPO
} from '../hooks/useEditarSesion'
import { useMisionesActivas } from '../hooks/useMisionesActivas'

// HU38: edición de una sesión en estado Programada. Reutiliza los mismos
// componentes y patrón visual del formulario de creación, añadiendo los
// campos de capacidad (que dependen del modo). Código de acceso y estado NO
// se editan: ni siquiera se exponen como campos del formulario.

function aDateTimeLocalString(fecha: Date): string {
  const desplazamiento = fecha.getTimezoneOffset() * 60_000
  return new Date(fecha.getTime() - desplazamiento).toISOString().slice(0, 16)
}

export function PaginaEditarSesion() {
  const { id } = useParams<{ id: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase =
    usuario?.rol === 'Administrador' ? '/administrador/sesiones' : '/operador/sesiones'
  const rutaDetalle = `${rutaBase}/${id}`

  const [estadoCarga, setEstadoCarga] =
    useState<'cargando' | 'error' | 'noProgramada' | 'listo'>('cargando')
  const [mensajeCarga, setMensajeCarga] = useState<string | null>(null)

  const {
    datos,
    errores,
    errorGeneral,
    enviando,
    prefijar,
    actualizarCampo,
    agregarMision,
    quitarMision,
    enviar,
  } = useEditarSesion({ token, id })

  const {
    misiones: misionesActivas,
    cargando: cargandoMisiones,
    error: errorMisiones,
  } = useMisionesActivas(token)

  useEffect(() => {
    const ref = { cancelado: false }
    async function cargar() {
      if (!token || !id) {
        setEstadoCarga('error')
        setMensajeCarga('Identificador de sesión inválido.')
        return
      }
      try {
        const detalle = await obtenerSesion(id, token)
        if (ref.cancelado) return
        if (detalle.estado !== 'Programada') {
          setEstadoCarga('noProgramada')
          return
        }
        prefijar(detalle)
        setEstadoCarga('listo')
      } catch (e) {
        if (ref.cancelado) return
        setMensajeCarga(
          e instanceof Error ? e.message : 'No se pudo cargar la sesión a editar.')
        setEstadoCarga('error')
      }
    }
    cargar()
    return () => { ref.cancelado = true }
    // prefijar es estable (proviene del hook); id/token disparan la recarga.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, id])

  async function manejarEnvio(e: React.FormEvent) {
    e.preventDefault()
    const actualizada = await enviar()
    if (actualizada) {
      // El detalle se recarga al montarse; le pasamos el mensaje de éxito.
      navegar(rutaDetalle, { state: { mensajeExito: 'Sesión actualizada correctamente.' } })
    }
  }

  if (estadoCarga === 'cargando') {
    return (
      <LayoutPanel titulo="Editar sesión" descripcion="Cargando…">
        <section className="seccion">
          <p className="detalle-mensaje-vacio">Cargando datos de la sesión…</p>
        </section>
      </LayoutPanel>
    )
  }

  if (estadoCarga === 'error') {
    return (
      <LayoutPanel titulo="Editar sesión" descripcion="">
        <div style={{ marginBottom: 'var(--espacio-4)' }}>
          <Boton variante="volver" onClick={() => navegar(rutaDetalle)}>
            ← Volver al detalle
          </Boton>
        </div>
        <section className="seccion">
          <Alerta tono="error">
            {mensajeCarga ?? 'No se pudo cargar la sesión a editar.'}
          </Alerta>
        </section>
      </LayoutPanel>
    )
  }

  if (estadoCarga === 'noProgramada') {
    return (
      <LayoutPanel titulo="Editar sesión" descripcion="">
        <div style={{ marginBottom: 'var(--espacio-4)' }}>
          <Boton variante="volver" onClick={() => navegar(rutaDetalle)}>
            ← Volver al detalle
          </Boton>
        </div>
        <section className="seccion">
          <Alerta tono="aviso">
            Solo se pueden editar sesiones en estado Programada.
          </Alerta>
        </section>
      </LayoutPanel>
    )
  }

  return (
    <LayoutPanel
      titulo="Editar sesión"
      descripcion="Modifique los datos de la sesión. El código de acceso y el estado no se modifican."
    >
      <div style={{ marginBottom: 'var(--espacio-4)' }}>
        <Boton variante="volver" onClick={() => navegar(rutaDetalle)} disabled={enviando}>
          ← Volver al detalle
        </Boton>
      </div>

      <section className="seccion">
        {errorGeneral && <Alerta tono="error">{errorGeneral}</Alerta>}

        <form onSubmit={manejarEnvio} noValidate className="formulario-trivia">
          <CampoFormulario etiqueta="Nombre" htmlFor="nombre" error={errores.nombre}>
            <input
              id="nombre"
              type="text"
              maxLength={150}
              value={datos.nombre}
              onChange={(e) => actualizarCampo('nombre', e.target.value)}
              disabled={enviando}
              placeholder="Ej. Sesión piloto de historia"
            />
          </CampoFormulario>

          <CampoFormulario
            etiqueta="Descripción"
            htmlFor="descripcion"
            error={errores.descripcion}
          >
            <textarea
              id="descripcion"
              maxLength={1000}
              rows={3}
              value={datos.descripcion}
              onChange={(e) => actualizarCampo('descripcion', e.target.value)}
              disabled={enviando}
              placeholder="Resumen de la sesión que verán los operadores."
            />
          </CampoFormulario>

          <SelectorModoSesion
            valor={datos.modo}
            alCambiar={(modo) => actualizarCampo('modo', modo)}
            deshabilitado={enviando}
            error={errores.modo}
          />

          {datos.modo === 'Individual' ? (
            <CampoFormulario
              etiqueta="Máximo de participantes"
              htmlFor="maximoParticipantes"
              error={errores.maximoParticipantes}
              ayuda={`Mínimo ${MIN_PARTICIPANTES_INDIVIDUAL} participante(s).`}
            >
              <input
                id="maximoParticipantes"
                type="number"
                min={MIN_PARTICIPANTES_INDIVIDUAL}
                step={1}
                value={datos.maximoParticipantes}
                onChange={(e) => actualizarCampo('maximoParticipantes', e.target.value)}
                disabled={enviando}
              />
            </CampoFormulario>
          ) : (
            <>
              <CampoFormulario
                etiqueta="Máximo de equipos"
                htmlFor="maximoEquipos"
                error={errores.maximoEquipos}
                ayuda={`Mínimo ${MIN_EQUIPOS} equipo(s).`}
              >
                <input
                  id="maximoEquipos"
                  type="number"
                  min={MIN_EQUIPOS}
                  step={1}
                  value={datos.maximoEquipos}
                  onChange={(e) => actualizarCampo('maximoEquipos', e.target.value)}
                  disabled={enviando}
                />
              </CampoFormulario>

              <CampoFormulario
                etiqueta="Máximo de participantes por equipo"
                htmlFor="maximoParticipantesPorEquipo"
                error={errores.maximoParticipantesPorEquipo}
                ayuda={`Mínimo ${MIN_PARTICIPANTES_POR_EQUIPO} participantes por equipo.`}
              >
                <input
                  id="maximoParticipantesPorEquipo"
                  type="number"
                  min={MIN_PARTICIPANTES_POR_EQUIPO}
                  step={1}
                  value={datos.maximoParticipantesPorEquipo}
                  onChange={(e) =>
                    actualizarCampo('maximoParticipantesPorEquipo', e.target.value)}
                  disabled={enviando}
                />
              </CampoFormulario>
            </>
          )}

          <CampoFormulario
            etiqueta="Fecha y hora programada"
            htmlFor="fechaProgramada"
            error={errores.fechaProgramada}
          >
            <input
              id="fechaProgramada"
              type="datetime-local"
              value={datos.fechaProgramada}
              min={aDateTimeLocalString(new Date())}
              onChange={(e) => actualizarCampo('fechaProgramada', e.target.value)}
              disabled={enviando}
            />
          </CampoFormulario>

          <SelectorMisiones
            misionesActivas={misionesActivas}
            misionesSeleccionadasIds={datos.misionesIds}
            alAgregar={agregarMision}
            alQuitar={quitarMision}
            cargando={cargandoMisiones}
            errorCarga={errorMisiones}
            errorFormulario={errores.misionesIds}
            deshabilitado={enviando}
          />

          <div className="acciones-formulario-trivia">
            <Boton
              variante="volver"
              type="button"
              onClick={() => navegar(rutaDetalle)}
              disabled={enviando}
            >
              Cancelar
            </Boton>
            <Boton
              variante="primario"
              type="submit"
              disabled={enviando || cargandoMisiones}
            >
              {enviando ? 'Guardando…' : 'Guardar cambios'}
            </Boton>
          </div>
        </form>
      </section>
    </LayoutPanel>
  )
}
