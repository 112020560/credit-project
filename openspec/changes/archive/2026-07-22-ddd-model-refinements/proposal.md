## Why

El análisis DDD identificó 4 refinamientos de baja prioridad que mejoran la calidad del modelo sin afectar funcionalidad existente: políticas de negocio hardcodeadas que deben ser configurables desde base de datos, un evento de dominio faltante (`ContractApproved`), comportamiento de la regla de crédito cuando no hay score disponible que debería ser configurable, y comentarios en español mezclados con código en inglés que fragmentan el Ubiquitous Language en el código. Este spec depende de `ddd-ubiquitous-language` estar completado.

## What Changes

- Extraer la tasa base de interés (`BaseInterestRate = 8.0m`) y el umbral de auto-default (90 días) de constantes hardcodeadas a una tabla de configuración en PostgreSQL — introducir `UnderwritingPolicy` como objeto de dominio que encapsula estas políticas y se carga desde base de datos
- Agregar el evento de dominio `ContractApproved` para capturar la aprobación del contrato como momento explícito del negocio, separado de `ContractCreated`
- Hacer configurable el comportamiento de `CreditScoreRule` cuando no hay score disponible: actualmente pasa silenciosamente, pero la política (¿rechazar? ¿aprobar con penalización?) debe ser configurable via `UnderwritingPolicy`
- Estandarizar todos los comentarios de código a inglés (eliminar comentarios en español en archivos de dominio y aplicación)

## Capabilities

### New Capabilities

- `underwriting-policy`: Configuración de políticas de underwriting (tasa base, umbral de default, comportamiento sin credit score) almacenada en PostgreSQL y cargada al iniciar la aplicación via un repositorio de dominio

### Modified Capabilities

- `loan-contract`: Se agrega el evento `ContractApproved` al ciclo de vida — el flujo cambia de `ContractCreated → (Approved state)` a `ContractCreated → ContractApproved → (Active state on disburse)`

## Impact

- `CreditSystem.Domain` — nuevo value object/record `UnderwritingPolicy`, nuevo evento `ContractApproved`, modificación a `ContractEngine` para recibir `UnderwritingPolicy` en lugar de constante hardcodeada, modificación a `CreditScoreRule` para leer política de comportamiento sin score
- `CreditSystem.Application` — nuevo `IUnderwritingPolicyRepository` en abstracciones; handler de creación de contrato carga la política antes de invocar el engine
- `CreditSystem.Infrastructure` — `UnderwritingPolicyRepository` con Dapper, nuevo script de migración con tabla `underwriting_policies` y datos seed
- `CreditSystem.Domain` (eventos) — nuevo `ContractApproved.cs` en `Aggregates/LoanContract/Events/`; `LoanContractAggregate.ApplyEvent` agrega case para `ContractApproved`
- **PostgreSQL**: nueva tabla `underwriting_policies` con seed de valores actuales (tasa base 8%, umbral 90 días, comportamiento sin score = "approve_with_penalty")
- Comentarios en español eliminados de todos los archivos de `CreditSystem.Domain` y `CreditSystem.Application`
