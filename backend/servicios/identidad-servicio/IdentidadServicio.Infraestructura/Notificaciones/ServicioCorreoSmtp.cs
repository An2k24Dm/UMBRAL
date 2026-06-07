using System.Net;
using System.Net.Mail;
using IdentidadServicio.Aplicacion.Puertos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentidadServicio.Infraestructura.Notificaciones;

public sealed class OpcionesCorreo
{
    public const string Seccion = "Correo";
    public bool Habilitado { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string RemitenteCorreo { get; set; } = "no-reply@umbral.local";
    public string RemitenteNombre { get; set; } = "UMBRAL";
}

public sealed class ServicioCorreoSmtp : IServicioCorreo
{
    private readonly OpcionesCorreo _opciones;
    private readonly ILogger<ServicioCorreoSmtp> _registro;

    public ServicioCorreoSmtp(
        IOptions<OpcionesCorreo> opciones,
        ILogger<ServicioCorreoSmtp> registro)
    {
        _opciones = opciones.Value;
        _registro = registro;
    }

    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoTextoPlano,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ArgumentException("Destinatario obligatorio.", nameof(destinatario));

        if (!_opciones.Habilitado || string.IsNullOrWhiteSpace(_opciones.Host))
        {
            _registro.LogWarning(
                "Envío de correo deshabilitado (Correo:Habilitado=false). " +
                "Destino={Destino}, asunto='{Asunto}'.",
                destinatario, asunto);
            return;
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_opciones.RemitenteCorreo, _opciones.RemitenteNombre),
            Subject = asunto,
            Body = cuerpoTextoPlano,
            IsBodyHtml = false
        };
        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(_opciones.Host, _opciones.Puerto)
        {
            EnableSsl = _opciones.UsarSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrEmpty(_opciones.Usuario))
        {
            cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Contrasena);
        }

        try
        {
            await cliente.SendMailAsync(mensaje, cancelacion);
            _registro.LogInformation(
                "Correo enviado a {Destino} con asunto '{Asunto}'.",
                destinatario, asunto);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "Fallo enviando correo a {Destino} con asunto '{Asunto}'.",
                destinatario, asunto);
            throw;
        }
    }
}
