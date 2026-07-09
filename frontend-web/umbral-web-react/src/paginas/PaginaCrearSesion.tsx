import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { Boton } from '../componentes/Boton'
import { Alerta } from '../componentes/Alerta'
import { SelectorModoSesion } from '../componentes/sesiones/SelectorModoSesion'
import { AyudaModoSesion } from '../componentes/sesiones/AyudaModoSesion'
import { SelectorMisiones } from '../componentes/sesiones/SelectorMisiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import {
  useCrearSesion,
  MIN_EQUIPOS,
  MIN_PARTICIPANTES_INDIVIDUAL,
  MIN_PARTICIPANTES_POR_EQUIPO
} from '../hooks/useCrearSesion'
import { useMisionesActivas } from '../hooks/useMisionesActivas'

// La página solo orquesta y renderiza. Estado, validación y envío
// viven en `useCrearSesion`; la carga de misiones activas en
// `useMisionesActivas`; la presentación del selector y la ayuda en
// componentes reutilizables.

function aDateTimeLocalString(fecha: Date): string {
  const desplazamiento = fecha.getTimezoneOffset() * 60_000
  return new Date(fecha.getTime() - desplazamiento).toISOString().slice(0, 16)
}

export function PaginaCrearSesion() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase =
    usuario?.rol === 'Administrador' ? '/administrador/sesiones' : '/operador/sesiones'

  const {
    datos,
    errores,
    errorGeneral,
    enviando,
    exito,
    actualizarCampo,
    agregarMision,
    quitarMision,
    enviar,
  } = useCrearSesion({ token })

  const {
    misiones: misionesActivas,
    cargando: cargandoMisiones,
    error: errorMisiones,
  } = useMisionesActivas(token)

  // Asegura que el efecto useMisionesActivas tenga un token disponible
  // antes de iniciar; el hook ya maneja el caso null, este efecto es
  // para que el linter no marque la advertencia exhaustive-deps.
  useEffect(() => { /* dependiendo del token, gestionado en el hook */ }, [token])

  async function manejarEnvio(e: React.FormEvent) {
    e.preventDefault()
    await enviar()
  }

  if (exito) {
    return (
      <LayoutPanel
        titulo="Sesión creada"
        descripcion="La sesión quedó en estado Programada."
      >
        <section className="seccion">
          <Alerta tono="exito">Sesión creada correctamente.</Alerta>
          <div className="detalle-grilla" style={{ marginTop: 'var(--espacio-4)' }}>
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Nombre</span>
              <span className="detalle-campo-valor">{exito.nombre}</span>
            </div>
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Tipo de sesión</span>
              <span className="detalle-campo-valor">{exito.modo}</span>
            </div>
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Estado</span>
              <span className="detalle-campo-valor">{exito.estado}</span>
            </div>
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Código de acceso</span>
              <span
                className="detalle-campo-valor"
                style={{ fontFamily: 'monospace', fontSize: '1.1rem' }}
              >
                {exito.codigoAcceso}
              </span>
            </div>
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Misiones asociadas</span>
              <span className="detalle-campo-valor">{exito.misionesIds.length}</span>
            </div>
          </div>

          <div
            className="acciones-formulario-trivia"
            style={{ marginTop: 'var(--espacio-4)' }}
          >
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>
              Volver al listado
            </Boton>
            <Boton variante="primario" onClick={() => navegar(`${rutaBase}/${exito.id}`)}>
              Ver detalle
            </Boton>
          </div>
        </section>
      </LayoutPanel>
    )
  }

  return (
    <LayoutPanel
      titulo="Crear sesión"
      descripcion="Defina la sesión y seleccione entre 1 y 5 misiones activas."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Nueva sesión</h2>
            <p>
              La sesión quedará en estado Programada. No se agregan participantes ni
              equipos: ellos ingresan por su cuenta desde la app móvil.
            </p>
          </div>
        </div>

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
          <AyudaModoSesion
            modo={datos.modo}
            maximoParticipantes={datos.maximoParticipantes}
            maximoEquipos={datos.maximoEquipos}
            maximoParticipantesPorEquipo={datos.maximoParticipantesPorEquipo}
          />

          {datos.modo === 'Individual' ? (
            <CampoFormulario
              etiqueta="Máximo de participantes"
              htmlFor="maximoParticipantes"
              error={errores.maximoParticipantes}
              ayuda="Indica cuántos participantes podrán ingresar a esta sesión individual."
            >
              <input
                id="maximoParticipantes"
                type="number"
                min={MIN_PARTICIPANTES_INDIVIDUAL}
                step={1}
                placeholder="Ej: 10"
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
                ayuda="Indica cuántos equipos podrán participar en esta sesión grupal."
              >
                <input
                  id="maximoEquipos"
                  type="number"
                  min={MIN_EQUIPOS}
                  step={1}
                  placeholder="Ej: 4"
                  value={datos.maximoEquipos}
                  onChange={(e) => actualizarCampo('maximoEquipos', e.target.value)}
                  disabled={enviando}
                />
              </CampoFormulario>

              <CampoFormulario
                etiqueta="Participantes por equipo"
                htmlFor="maximoParticipantesPorEquipo"
                error={errores.maximoParticipantesPorEquipo}
                ayuda="Indica cuántos integrantes podrá tener cada equipo."
              >
                <input
                  id="maximoParticipantesPorEquipo"
                  type="number"
                  min={MIN_PARTICIPANTES_POR_EQUIPO}
                  step={1}
                  placeholder="Ej: 3"
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

          <CampoFormulario
            etiqueta="Duración límite (minutos, opcional)"
            htmlFor="duracionMinutosLimite"
            ayuda="Si se indica, la sesión se finalizará automáticamente al cumplirse ese tiempo desde que se inicia. Déjelo vacío para no establecer límite de tiempo."
          >
            <input
              id="duracionMinutosLimite"
              type="number"
              min={1}
              step={1}
              placeholder="Ej: 60"
              value={datos.duracionMinutosLimite}
              onChange={(e) => actualizarCampo('duracionMinutosLimite', e.target.value)}
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
              onClick={() => navegar(rutaBase)}
              disabled={enviando}
            >
              Cancelar
            </Boton>
            <Boton
              variante="primario"
              type="submit"
              disabled={enviando || cargandoMisiones}
            >
              {enviando ? 'Creando…' : 'Crear sesión'}
            </Boton>
          </div>
        </form>
      </section>
    </LayoutPanel>
  )
}
