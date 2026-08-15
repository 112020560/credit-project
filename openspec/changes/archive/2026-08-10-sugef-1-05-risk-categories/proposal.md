## Why

Las cooperativas supervisadas por SUGEF en Costa Rica están obligadas por la normativa SUGEF 1-05 a clasificar cada deudor en una categoría de riesgo y a mantener estimaciones (provisiones) mínimas sobre el saldo de la deuda. Sin esta funcionalidad el sistema no puede ser usado legalmente por ninguna cooperativa supervisada ni pasar una auditoría del regulador.

## What Changes

- Nuevo enum `LoanRiskCategory` (A1, A2, B1, B2, C1, C2, D, E) con porcentajes de estimación asociados según SUGEF 1-05
- Nuevo domain service `RiskClassificationService` que clasifica un préstamo según días de mora
- Nuevo evento de dominio `LoanRiskCategoryChanged` emitido cuando la categoría de un préstamo cambia
- Campos `RiskCategory` y `EstimatedProvision` en `LoanSummaryReadModel` y tabla `rm_loan_summaries`
- Nuevo job `RiskClassificationJob` que reclasifica diariamente todos los préstamos activos/morosos
- Nuevo worker `RiskClassificationWorker` que ejecuta el job en horario configurable
- Endpoint `GET /api/loans/risk-summary` — resumen de cartera por categoría de riesgo
- Endpoint `PUT /api/loans/{loanId}/risk-category` — reclasificación manual (solo degradación)
- Migración SQL con columnas nuevas en `rm_loan_summaries`

## Capabilities

### New Capabilities

- `loan-risk-classification`: Clasificación de préstamos en categorías de riesgo SUGEF 1-05 (A1–E), cálculo de estimaciones (provisiones) mínimas, reclasificación automática diaria y manual por analista
- `risk-portfolio-report`: Reporte de cartera en riesgo agrupado por categoría — cantidad de préstamos, saldo total y estimación total por categoría

### Modified Capabilities

- `loan-contract`: El contrato ahora tiene categoría de riesgo y estimación de provisión como parte de su estado proyectado en el read model

## Impact

- **Domain**: nuevo enum `LoanRiskCategory`, nuevo domain service `RiskClassificationService`, nuevo evento `LoanRiskCategoryChanged`
- **Application**: nuevo `RiskClassificationJob`, interfaz `IRiskClassificationJob`
- **Infrastructure**: nuevo `RiskClassificationWorker`, proyector para `LoanRiskCategoryChanged`, migración SQL, nuevo query de resumen de riesgo
- **API**: 2 nuevos endpoints en grupo `LoanRiskEndpoints`
- **Read Models**: `LoanSummaryReadModel` extendido con `RiskCategory` y `EstimatedProvision`
- Sin breaking changes en la API existente
