## 1. Architecture Decision Record

- [x] 1.1 Crear directorio `docs/adr/` si no existe
- [x] 1.2 Crear `docs/adr/0001-strategic-design-domain-classification.md` con formato MADR: contexto, decisión, clasificación de subdomains (Core/Supporting/Generic), mapa de bounded contexts, integraciones externas conocidas
- [x] 1.3 Agregar referencia al ADR en el `README.md` o `CLAUDE.md` del proyecto

## 2. Renombrar CustomersRef → ExternalCustomerRef

- [x] 2.1 Renombrar la clase `CustomersRef` a `ExternalCustomerRef` en `Crm.Domain/Customers/ExternalCustomerRef.cs`
- [x] 2.2 Agregar comentario XML `<summary>` que explique que representa una referencia a un cliente originado en un sistema externo que no fue creado en este CRM
- [x] 2.3 Actualizar todas las referencias a `CustomersRef` en el codebase (Infrastructure, migrations, DbContext)
- [x] 2.4 Agregar `.ToTable("customers_ref")` en la configuración EF Core para mantener compatibilidad con el nombre de tabla existente

## 3. Repositorio e infraestructura para ExternalCustomerRef

- [x] 3.1 Crear interfaz `IExternalCustomerRefRepository` en `Crm.Domain/Abstractions/Persistence/` con métodos `UpsertAsync(ExternalCustomerRef, CancellationToken)` y `GetByExternalIdAsync(Guid, CancellationToken)`
- [x] 3.2 Crear `ExternalCustomerRefRepository` en `Crm.Infrastructure/Adapters/Outbound/EntityFramework/Repositories/` implementando la interfaz con lógica de upsert por `ExternalId`
- [x] 3.3 Agregar `ExternalCustomerRef` al `CrmDbContext` si no está ya configurado
- [x] 3.4 Registrar `IExternalCustomerRefRepository → ExternalCustomerRefRepository` en `DependendyInjection.cs`

## 4. Endpoint RegisterExternalCustomer

- [x] 4.1 Crear DTO `RegisterExternalCustomerDto` en `Crm.Application/ExternalCustomers/Dtos/` con campos: `ExternalId`, `DisplayName`, `LegalName`, `RiskScore`, `Metadata`
- [x] 4.2 Crear `RegisterExternalCustomerCommand` y su handler en `Crm.Application/ExternalCustomers/` usando `IExternalCustomerRefRepository.UpsertAsync`
- [x] 4.3 Crear endpoint `POST /api/v1/external-customers` en `Crm.WebApi/Endpoints/ExternalCustomers/Register.cs` implementando `IEndpoint`

## 5. Verificación

- [x] 5.1 Ejecutar `dotnet build CrmProject.sln` y confirmar que compila sin errores
- [x] 5.2 Confirmar que no existen referencias al nombre antiguo `CustomersRef` en el codebase
- [ ] 5.3 Probar `POST /api/v1/external-customers` manualmente — crear y actualizar una referencia
- [ ] 5.4 Revisar el ADR con al menos un miembro del equipo para validar la clasificación de subdomains
