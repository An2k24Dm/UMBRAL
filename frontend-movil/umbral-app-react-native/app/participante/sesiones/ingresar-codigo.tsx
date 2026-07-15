import { useEffect, useState } from "react";
import { StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { BotonMovil } from "../../../componentes/BotonMovil";
import { CampoTextoMovil } from "../../../componentes/CampoTextoMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import { useIngresoSesion } from "../../../hooks/useIngresoSesion";

const LONGITUD_MAXIMA_CODIGO = 32;

export default function PantallaIngresarCodigoSesion() {
  return (
    <RutaProtegidaMovil>
      <Contenido />
    </RutaProtegidaMovil>
  );
}

function Contenido() {
  const enrutador = useRouter();
  const { cerrarSesion } = useAutenticacion();
  const [codigo, setCodigo] = useState("");
  const [errorLocal, setErrorLocal] = useState<string | null>(null);
  const {
    ingresando,
    error,
    sesionExpirada,
    ingresarPorCodigo,
    limpiarError,
  } = useIngresoSesion();

  useEffect(() => {
    if (sesionExpirada) cerrarSesion().finally(() => enrutador.replace("/"));
  }, [sesionExpirada, cerrarSesion, enrutador]);

  const alCambiarCodigo = (valor: string) => {
    setCodigo(valor.toUpperCase());
    setErrorLocal(null);
    limpiarError();
  };

  const enviar = async () => {
    const limpio = codigo.trim();
    if (!limpio) {
      setErrorLocal("El código de la sesión es obligatorio.");
      return;
    }
    if (limpio.length > LONGITUD_MAXIMA_CODIGO) {
      setErrorLocal(`El código no puede superar ${LONGITUD_MAXIMA_CODIGO} caracteres.`);
      return;
    }

    const resultado = await ingresarPorCodigo(limpio);
    if (resultado?.redirigirADetalle) {
      enrutador.replace(`/participante/sesiones/${resultado.sesionId}`);
    }
  };

  return (
    <PantallaBase>
      <View style={estilos.encabezado}>
        <Text style={estilos.titulo}>Ingresar con código</Text>
        <Text style={estilos.subtitulo}>
          Escribe el código compartido por el operador de la sesión.
        </Text>
      </View>

      <View style={estilos.tarjeta}>
        <CampoTextoMovil
          etiqueta="Código de sesión"
          valor={codigo}
          onCambio={alCambiarCodigo}
          placeholder="Ej. TEST01"
          autoCapitalize="characters"
          editable={!ingresando}
          error={errorLocal ?? error}
        />
        <BotonMovil
          titulo="Ingresar"
          onPress={enviar}
          cargando={ingresando}
          deshabilitado={!codigo.trim()}
        />
        <BotonMovil
          titulo="Cancelar"
          onPress={() => enrutador.replace("/participante/sesiones")}
          variante="secundario"
          deshabilitado={ingresando}
        />
      </View>
    </PantallaBase>
  );
}

const estilos = StyleSheet.create({
  encabezado: { marginBottom: tema.espacios.lg, paddingTop: tema.espacios.md },
  titulo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
  },
  subtitulo: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
    marginTop: tema.espacios.xs,
    lineHeight: 20,
  },
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderColor: tema.colores.bordeTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    padding: tema.espacios.lg,
  },
});
