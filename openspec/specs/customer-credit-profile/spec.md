# Spec: Customer Credit Profile

Perfil crediticio local de un cliente, sincronizado desde el CRM externo. Representa los datos del cliente que el Credit System necesita para evaluar solicitudes de crédito: identidad, contacto y métricas financieras.

---

## Requirements

### Requirement: CustomerCreditProfile reemplaza a CustomerReference
El sistema SHALL usar `CustomerCreditProfile` como nombre canónico para el perfil de cliente local en todo el código. El nombre `CustomerReference` queda eliminado.

#### Scenario: Compilación sin CustomerReference
- **WHEN** se compila la solución completa
- **THEN** no existe ninguna clase, interfaz, variable ni comentario de código que use el nombre `CustomerReference`

---

### Requirement: CustomerCreditProfile es un modelo no-anémico con construcción controlada
`CustomerCreditProfile` SHALL tener todos sus setters como `private set` y SHALL exponerse únicamente a través de un factory method estático `CustomerCreditProfile.Create(...)` que valida los campos requeridos antes de construir la instancia.

#### Scenario: Creación válida via factory method
- **WHEN** se llama a `CustomerCreditProfile.Create(externalId, fullName, documentType, documentNumber)`
- **THEN** se retorna una instancia con los campos requeridos populados
- **THEN** los campos opcionales (`Email`, `Phone`, `CreditScore`, `MonthlyIncome`, `MonthlyDebt`) se inicializan en `null`

#### Scenario: Construcción directa bloqueada
- **WHEN** cualquier código intenta instanciar `CustomerCreditProfile` con `new CustomerCreditProfile()`
- **THEN** el compilador rechaza la llamada (constructor privado o protegido)

#### Scenario: Actualización de campos via método Update
- **WHEN** el repositorio necesita actualizar un perfil existente con datos del CRM
- **THEN** usa el método `Update(...)` o propiedades con `internal set` accesibles desde Infrastructure
- **THEN** no existe asignación directa de propiedad desde fuera del ensamblado Domain

---

### Requirement: ICustomerReadRepository como puerto de lectura del perfil de cliente
El sistema SHALL exponer `ICustomerReadRepository` (renombrado desde `ICustomerService`) como puerto de lectura de `CustomerCreditProfile`. La implementación `CustomerReadRepository` (renombrada desde `CustomerService`) vive en Infrastructure.

#### Scenario: Resolución de repositorio de lectura
- **WHEN** se resuelve `ICustomerReadRepository` desde el contenedor DI
- **THEN** se obtiene `CustomerReadRepository` ubicado en `CreditSystem.Infrastructure/Repositories/`

#### Scenario: Búsqueda por ExternalId
- **WHEN** se llama a `ICustomerReadRepository.GetByExternalIdAsync(externalId)`
- **THEN** retorna el `CustomerCreditProfile` correspondiente o `null` si no existe
