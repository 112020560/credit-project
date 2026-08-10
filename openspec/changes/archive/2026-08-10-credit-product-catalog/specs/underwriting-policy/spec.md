## MODIFIED Requirements

### Requirement: ContractEngine usa tasa base como parámetro por contrato
`ContractEngine.EvaluateAsync` SHALL recibir la tasa base como parámetro opcional en lugar de leerla siempre desde `UnderwritingPolicy.BaseInterestRate`. Cuando se provee la tasa base por parámetro, esta sobreescribe la de la política para ese contrato específico. Cuando no se provee (null), el engine usa `policy.BaseInterestRate` como fallback.

Firma actualizada:
```
EvaluateAsync(ContractEvaluationContext context, decimal? baseInterestRate = null)
```

La tasa efectiva usada por el engine es:
```
effectiveBaseRate = baseInterestRate ?? policy.BaseInterestRate
```

#### Scenario: Engine recibe tasa base del producto y la usa
- **WHEN** el handler llama `EvaluateAsync(context, baseInterestRate: product.Rates.BaseInterestRate)`
- **THEN** el engine usa esa tasa como base para calcular la tasa final
- **THEN** `ApprovedRate = baseInterestRate + Σ(ajustes de todas las reglas)`

#### Scenario: Engine sin tasa base explícita usa la de la política
- **WHEN** el handler llama `EvaluateAsync(context)` sin pasar `baseInterestRate`
- **THEN** el engine usa `policy.BaseInterestRate` como tasa base (comportamiento anterior)
- **THEN** los tests existentes que no pasan tasa base siguen funcionando sin cambios

#### Scenario: Engine no conoce el concepto de CreditProduct
- **WHEN** `ContractEngine` evalúa un contrato
- **THEN** el engine solo recibe la tasa base como decimal, no el objeto `CreditProduct`
- **THEN** el engine no tiene referencia directa ni dependencia al namespace de `CreditProduct`
