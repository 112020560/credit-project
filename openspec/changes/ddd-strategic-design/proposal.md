## Why

El Core Domain del CRM (el flujo de originación de crédito: Prospect → CreditApplication → RiskEvaluation → Customer) no está documentado explícitamente, y la integración con sistemas externos no tiene Anti-Corruption Layer. Sin esta documentación, los desarrolladores no saben dónde poner el esfuerzo de modelado más profundo ni cómo proteger el dominio de modelos externos. `CustomersRef` sugiere que ya existe una integración con un sistema externo que no tiene ACL.

## What Changes

- Crear un Architecture Decision Record (ADR) que define: Core Domain vs Supporting vs Generic subdomains, bounded contexts existentes, y mapa de integraciones externas conocidas
- Crear un Anti-Corruption Layer explícito para la integración que produce `CustomersRef` (o cualquier sistema externo que proyecte datos hacia el CRM)
- Documentar el propósito de `CustomersRef` en el ADR y en el código

## Capabilities

### New Capabilities

- `strategic-design-adr`: Documento ADR en `docs/adr/` que clasifica subdomains y define el mapa de contextos
- `external-customer-acl`: Adapter que traduce datos de sistemas externos al modelo del dominio, reemplazando el acceso directo a través de `CustomersRef`

### Modified Capabilities

(ninguna — este cambio es principalmente documentación y estructural)

## Impact

- **docs/adr/**: Nuevo archivo ADR de strategic design
- **Crm.Domain**: Posible rename/clarificación de `CustomersRef`
- **Crm.Infrastructure**: Nuevo adapter para la integración externa que produce datos de `CustomersRef`
- Sin breaking changes en API REST
