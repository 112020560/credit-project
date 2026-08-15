## MODIFIED Requirements

### Requirement: Perfil del socio incluye saldo de capital social
El perfil de un socio cooperativo SHALL incluir el campo `SocialCapitalBalance` que representa el monto total acumulado de aportes al capital social a través de todos sus préstamos en la institución.

#### Scenario: Saldo inicial de capital social es cero
- **WHEN** se registra un nuevo socio en el sistema
- **THEN** su `social_capital_balance` es `0.00` en la moneda base de la cooperativa

#### Scenario: Saldo de capital social reflejado en el perfil del socio
- **WHEN** se consulta el perfil de un socio mediante `GET /members/{id}`
- **THEN** la respuesta incluye el campo `socialCapitalBalance` con el monto acumulado actualizado

#### Scenario: Saldo de capital social consultable en endpoint dedicado
- **WHEN** se consulta `GET /members/{id}/social-capital`
- **THEN** el sistema retorna `{ memberId, socialCapitalBalance, currency, lastUpdatedAt }` o 404 si el socio no existe
