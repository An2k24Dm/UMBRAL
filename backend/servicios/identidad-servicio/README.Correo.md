# Envío de correo — identidad-servicio

El servicio de identidad envía las contraseñas temporales (alta de usuario y reseteo)
a través del puerto `IServicioCorreo`. Existen dos implementaciones intercambiables:

| Proveedor | Implementación | Uso previsto |
|-----------|----------------|--------------|
| `Smtp` (por defecto) | `ServicioCorreoSmtp` | Desarrollo local |
| `GmailApi` | `ServicioCorreoGmailApi` (HTTPS + OAuth 2.0) | Render / despliegue |

La selección es **solo por configuración**, mediante `EnvioCorreo:Proveedor`.
Si el valor está vacío o ausente, se usa **SMTP** (comportamiento actual).
Un valor no soportado provoca un error claro al resolver el servicio
(`"Proveedor de correo no soportado..."`).

> Ningún secreto real debe versionarse. En `appsettings.json` los valores de
> `GmailApi` van **vacíos**; se pueblan por variables de entorno en el despliegue.

## Desarrollo local (SMTP — sin cambios)

El comportamiento por defecto sigue siendo SMTP. Basta con las variables SMTP
actuales (transformadas por Docker Compose desde `CORREO_*`):

```
EnvioCorreo__Proveedor=Smtp        # opcional; es el valor por defecto

Correo__Habilitado=true
Correo__Host=smtp.gmail.com
Correo__Puerto=587
Correo__UsarSsl=true
Correo__Usuario=usuario@ejemplo.com
Correo__Contrasena=***            # App Password, nunca en Git
Correo__RemitenteCorreo=usuario@ejemplo.com
Correo__RemitenteNombre=UMBRAL
```

## Render (Gmail API por HTTPS/OAuth 2.0)

Configurar en el panel de Render (Environment) exactamente estas variables:

```
EnvioCorreo__Proveedor=GmailApi
GmailApi__ClientId=...
GmailApi__ClientSecret=...
GmailApi__RefreshToken=...
GmailApi__RemitenteCorreo=correo_remitente@gmail.com
GmailApi__RemitenteNombre=UMBRAL
```

`ClientId`, `ClientSecret` y `RefreshToken` provienen de una credencial OAuth 2.0
de Google Cloud (tipo "Web application" / "Desktop") con el scope
`https://www.googleapis.com/auth/gmail.send`. El `RemitenteCorreo` debe ser la
cuenta de Gmail dueña de ese refresh token.

### Notas de seguridad

- No se registran en logs `ClientSecret`, `RefreshToken`, `AccessToken`, el
  encabezado `Authorization` ni el cuerpo del correo.
- Un envío exitoso registra únicamente: proveedor (`GmailApi`), destinatario,
  `messageId` devuelto por Gmail y duración.
- El `HttpClient` tiene un timeout de 30 s y propaga `CancellationToken`; los
  logs distinguen timeout propio, cancelación de la solicitud, error de OAuth,
  error de Gmail y configuración incompleta.
