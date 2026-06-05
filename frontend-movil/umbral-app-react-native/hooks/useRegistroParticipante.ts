import { useState } from 'react'
import { registrarParticipanteApi } from '../servicios/participantesApi'
import { ErrorValidacionRegistro } from '../tipos/errores'
import type {
  DatosRegistroParticipante,
  RespuestaCrearUsuario,
} from '../tipos/participantes'

interface EstadoUseRegistroParticipante {
  enviando: boolean
  resultado: RespuestaCrearUsuario | null
  errorGeneral: string | null
  erroresPorCampo: Record<string, string>
  enviar: (datos: DatosRegistroParticipante) => Promise<RespuestaCrearUsuario | null>
  limpiarError: () => void
}

export function useRegistroParticipante(): EstadoUseRegistroParticipante {
  const [enviando, setEnviando] = useState(false)
  const [resultado, setResultado] = useState<RespuestaCrearUsuario | null>(null)
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const [erroresPorCampo, setErroresPorCampo] = useState<Record<string, string>>({})

  async function enviar(
    datos: DatosRegistroParticipante,
  ): Promise<RespuestaCrearUsuario | null> {
    setEnviando(true)
    setErrorGeneral(null)
    setErroresPorCampo({})
    try {
      const respuesta = await registrarParticipanteApi(datos)
      setResultado(respuesta)
      return respuesta
    } catch (e) {
      if (e instanceof ErrorValidacionRegistro) {
        const mapa: Record<string, string> = {}
        for (const err of e.errores) {
          mapa[err.campo] = err.mensaje
        }
        setErroresPorCampo(mapa)
        setErrorGeneral(e.message)
      } else if (e instanceof Error) {
        setErrorGeneral(e.message)
      } else {
        setErrorGeneral('No fue posible registrar la cuenta.')
      }
      return null
    } finally {
      setEnviando(false)
    }
  }

  function limpiarError() {
    setErrorGeneral(null)
    setErroresPorCampo({})
  }

  return { enviando, resultado, errorGeneral, erroresPorCampo, enviar, limpiarError }
}
