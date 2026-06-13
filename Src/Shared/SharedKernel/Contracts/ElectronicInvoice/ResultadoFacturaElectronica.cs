using System.Text.Json;

namespace SharedKernel.Contracts.ElectronicInvoice;

/// <summary>
/// DTO con los datos de respuesta de una factura electrónica procesada.
/// </summary>
public class DatosFacturaElectronica
{
    /// <summary>
    /// Clave numérica de 50 dígitos del documento electrónico
    /// </summary>
    public string Clave { get; set; } = string.Empty;

    /// <summary>
    /// Número consecutivo del documento
    /// </summary>
    public string NumeroConsecutivo { get; set; } = string.Empty;

    /// <summary>
    /// Estado devuelto por Hacienda (aceptado, procesando, enviado, rechazado, error)
    /// </summary>
    public string EstadoHacienda { get; set; } = string.Empty;

    /// <summary>
    /// Lista de advertencias de validación
    /// </summary>
    public List<string> Advertencias { get; set; } = new();
}

/// <summary>
/// Contrato de respuesta para el procesamiento de factura electrónica.
/// Usado en Request-Response entre PuntoVentas y FacturaElectronica.
/// </summary>
public class ResultadoFacturaElectronica
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Exitoso { get; set; }

    /// <summary>
    /// Mensaje principal de la respuesta
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Lista de errores (si los hay)
    /// </summary>
    public List<string> Errores { get; set; } = new();

    /// <summary>
    /// Lista de advertencias (no bloquean el proceso)
    /// </summary>
    public List<string> Advertencias { get; set; } = new();

    /// <summary>
    /// Datos adicionales de la respuesta (clave, consecutivo, etc.)
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Timestamp de la operación
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Código de estado personalizado (opcional)
    /// </summary>
    public string? CodigoEstado { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
