namespace SharedKernel.Contracts.ElectronicInvoice;

public class FacturaElectronicaContract
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
    //Documento
    public long ConsecutivoDocumento { get; set; }
    public DateTime FechaDocumento { get; set; }
    public string? TipoDocumento { get; set; } = "01";
    public string Sucursal { get; set; } = "0010010001";
    public string Terminal { get; set; } = "0001";
    public string? CondicionVenta { get; set; }
    // '01: Contado
    // '02: Credito
    // '03: Consignación
    // '04: Apartado
    // '05: Arrendamiento con opcion de compra
    // '06: Arrendamiento con función financiera
    // '99: Otros
    public int PlazoCredito { get; set; }
    public string? MedioPago { get; set; }
    public List<DetalleFacturaElectronicaContract>? DetalleServicios { get; set; }
    //Emisor
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
    //Receptor
    public bool Receptor { get; set; }
    public string? ReceptorNombre { get; set; }
    public string? ReceptorTipoIdentificacion { get; set; }
    public string? ReceptorNumeroIdentificacion { get; set; }
    public string ReceptorCodigoPais { get; set; } = "506";
    public string? ReceptorTelefono { get; set; }
    public string? ReceptorCorreoElectronico { get; set; }

    //RESUMEN
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

    //Normativa
    public string? NumeroResolucion { get; set; }
    public string? FechaResolucion { get; set; }
}

public class DetalleFacturaElectronicaContract
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