# Spec: Customer CRM Sync

Sincronización de perfiles de cliente desde el sistema CRM externo hacia el Credit System mediante una capa de anti-corrupción (ACL). El CRM publica eventos de dominio (`CustomerCreated`, `CustomerUpdated`) que son consumidos como mensajes de integración y traducidos al modelo interno del Credit System a través del command `SyncCustomerFromCrm`.

---

## ADDED Requirements

### Requirement: Consumidor CRM actúa como adaptador delgado
El consumer de mensajes CRM (`CustomerCreatedConsumer`, `CustomerUpdatedConsumer`) SHALL comportarse únicamente como un adaptador de traducción: recibir el mensaje de integración del CRM y despachar un command `SyncCustomerFromCrm` via MediatR, sin contener lógica de negocio ni acceso directo a base de datos.

#### Scenario: Mensaje CustomerCreated recibido
- **WHEN** llega un mensaje `CustomerCreated` desde RabbitMQ
- **THEN** el consumer construye un `SyncCustomerFromCrmCommand` mapeando los campos del contrato CRM a los campos internos
- **THEN** el consumer despacha el command via `IMediator.Send()`
- **THEN** el consumer NO ejecuta SQL directamente

#### Scenario: Mensaje CustomerUpdated recibido
- **WHEN** llega un mensaje `CustomerUpdated` desde RabbitMQ
- **THEN** el consumer construye un `SyncCustomerFromCrmCommand` con los datos actualizados
- **THEN** el consumer despacha el command via `IMediator.Send()`
- **THEN** el consumer NO ejecuta SQL directamente

#### Scenario: Error en el despacho del command
- **WHEN** `IMediator.Send()` lanza una excepción
- **THEN** el consumer deja propagar la excepción para que MassTransit aplique el retry policy configurado

---

### Requirement: SyncCustomerFromCrm command traduce el modelo CRM al modelo interno
El handler de `SyncCustomerFromCrmCommand` SHALL traducir los campos del contrato CRM (`IdentificationType`, `IdentificationNumber`, `CustomerId` como ExternalId) a los campos del modelo interno (`DocumentType`, `DocumentNumber`, `ExternalId`) sin que el resto del dominio conozca la nomenclatura CRM.

#### Scenario: Traducción de campos CRM a modelo interno
- **WHEN** el handler recibe un `SyncCustomerFromCrmCommand`
- **THEN** mapea `command.CrmCustomerId` → `CustomerCreditProfile.ExternalId`
- **THEN** mapea `command.IdentificationType` → `CustomerCreditProfile.DocumentType`
- **THEN** mapea `command.IdentificationNumber` → `CustomerCreditProfile.DocumentNumber`
- **THEN** extrae `CreditScore`, `MonthlyIncome`, `MonthlyDebt` del campo `Metadata` del contrato CRM

#### Scenario: Metadata ausente o sin campos financieros
- **WHEN** el mensaje CRM tiene `Metadata = null` o no contiene `CreditScore`/`MonthlyIncome`/`MonthlyDebt`
- **THEN** el handler persiste el perfil con esos campos en `null` sin lanzar excepción

---

### Requirement: ICustomerReferenceRepository como puerto de dominio
El sistema SHALL exponer `ICustomerReferenceRepository` en `CreditSystem.Domain/Abstractions/Repositories/` como el único puerto de acceso de escritura al perfil de cliente local. Ningún componente fuera de Infrastructure SHALL escribir directamente en la tabla `customer_references`.

#### Scenario: Upsert de perfil de cliente nuevo
- **WHEN** el handler llama a `ICustomerReferenceRepository.UpsertAsync()` con datos de un cliente que no existe
- **THEN** se inserta un nuevo registro en `customer_references`
- **THEN** el `Id` interno se genera como nuevo `Guid`

#### Scenario: Upsert de perfil de cliente existente
- **WHEN** el handler llama a `ICustomerReferenceRepository.UpsertAsync()` con `ExternalId` que ya existe
- **THEN** se actualizan los campos del registro existente
- **THEN** los campos financieros (`CreditScore`, `MonthlyIncome`, `MonthlyDebt`) se actualizan solo si el valor entrante no es `null` (COALESCE)

#### Scenario: Implementación en Infrastructure
- **WHEN** se resuelve `ICustomerReferenceRepository` desde el contenedor DI
- **THEN** se obtiene `CustomerReferenceRepository` ubicado en `CreditSystem.Infrastructure/Repositories/`
- **THEN** la implementación usa Dapper + Npgsql sin ORM
