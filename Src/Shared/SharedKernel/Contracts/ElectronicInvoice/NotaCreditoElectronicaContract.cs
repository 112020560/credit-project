namespace SharedKernel.Contracts.ElectronicInvoice;

/// <summary>
/// Contrato para envío de Nota de Crédito Electrónica a Hacienda.
/// TipoDocumento = "03"
/// </summary>
public class NotaCreditoElectronicaContract
{
    /// <summary>
    /// ID del tenant propietario del documento
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// ID del documento en el sistema externo que originó la solicitud (opcional).
    /// </summary>
    public string? ExternalDocumentId { get; set; }

    public string? ProveedorSistemas { get; set; }
    public string? CodigoActividad { get; set; }
    public string Situacion { get; set; } = "01";

    // Documento
    public long ConsecutivoDocumento { get; set; }
    public DateTime FechaDocumento { get; set; }
    public string TipoDocumento { get; set; } = "03"; // Nota de Crédito
    public string Sucursal { get; set; } = "0010010001";
    public string Terminal { get; set; } = "0001";
    public string? CondicionVenta { get; set; }
    public int PlazoCredito { get; set; }
    public string? MedioPago { get; set; }
    public List<DetalleNotaCreditoElectronicaContract>? DetalleServicios { get; set; }

    // Emisor
    public string? EmisorNombre { get; set; }
    public string? EmisorTipoIdentificacion { get; set; }
    public string? EmisorNumeroIdentificacion { get; set; }
    public string? EmisorProvincia { get; set; }
    public string? EmisorCanton { get; set; }
    public string? EmisorDistrito { get; set; }
    public string? EmisorBarrio { get; set; }
    public string? EmisorOtrasSenas { get; set; }
    public string? EmisorCorreoElectronico { get; set; }
    public string? EmisorTelefono { get; set; }
    public string? EmisorCodigoPaisTelefono { get; set; }

    // Receptor
    public bool Receptor { get; set; }
    public string? ReceptorNombre { get; set; }
    public string? ReceptorTipoIdentificacion { get; set; }
    public string? ReceptorNumeroIdentificacion { get; set; }
    public string ReceptorCodigoPais { get; set; } = "506";
    public string? ReceptorTelefono { get; set; }
    public string? ReceptorCorreoElectronico { get; set; }

    // Resumen
    public string CodigoMoneda { get; set; } = "CRC";
    public string TipoCambio { get; set; } = "1.0000";
    public decimal TotalServGravados { get; set; }
    public decimal TotalServExentos { get; set; }
    public decimal TotalMercanciasGravadas { get; set; }
    public decimal TotalMercanciasExentas { get; set; }
    public decimal TotalGravado { get; set; }
    public decimal TotalExento { get; set; }
    public decimal TotalVenta { get; set; }
    public decimal TotalDescuentos { get; set; }
    public decimal TotalVentaNeta { get; set; }
    public decimal TotalImpuesto { get; set; }
    public decimal TotalComprobante { get; set; }

    // Normativa
    public string? NumeroResolucion { get; set; }
    public string? FechaResolucion { get; set; }

    /// <summary>
    /// OBLIGATORIO: Información de referencia al documento que se está afectando
    /// </summary>
    public InformacionReferenciaContract InformacionReferencia { get; set; } = new();
}

/// <summary>
/// Detalle de línea para Nota de Crédito Electrónica
/// </summary>
public class DetalleNotaCreditoElectronicaContract
{
    public int NumeroLinea { get; set; }
    public string? ArticuloTipo { get; set; }
    public string? CodigoArticulo { get; set; }
    public int Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public string? DetalleArticulo { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Precio { get; set; }
    public decimal Descuento { get; set; }
    public string NaturalezaDescuento { get; set; } = "Descuento al Cliente";
    public decimal SubTotal { get; set; }
    public string? CodigoImpuesto { get; set; } = "01";
    public decimal TarifaImpuesto { get; set; } = 13;
    public decimal MontoImpuesto { get; set; }
    public decimal MontoTotalLinea { get; set; }
}

/// <summary>
/// Información de referencia al documento original
/// </summary>
public class InformacionReferenciaContract
{
    /// <summary>
    /// Tipo de documento referenciado:
    /// 01=Factura Electrónica, 02=Nota de Débito, 03=Nota de Crédito,
    /// 04=Tiquete Electrónico, 05=Nota de despacho, 06=Contrato,
    /// 07=Procedimiento, 08=Comprobante contingencia, 09=Sustituye rechazado,
    /// 10=Sustituye provisional, 99=Otros
    /// </summary>
    public string TipoDoc { get; set; } = "01";

    /// <summary>
    /// Clave de 50 dígitos del documento referenciado
    /// </summary>
    public string Numero { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de emisión del documento referenciado
    /// </summary>
    public DateTime FechaEmision { get; set; }

    /// <summary>
    /// Código de razón:
    /// 01=Anula documento, 02=Corrige texto, 03=Corrige monto,
    /// 04=Referencia a otro documento, 05=Sustituye provisional, 99=Otros
    /// </summary>
    public string Codigo { get; set; } = "01";

    /// <summary>
    /// Razón de la referencia (máximo 180 caracteres)
    /// </summary>
    public string Razon { get; set; } = string.Empty;
}
