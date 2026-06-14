## 1. Validación de spec contra implementación existente

- [ ] 1.1 Verificar que los umbrales del motor de reglas (score mínimo, DTI máximo, límite absoluto) coinciden con los valores documentados en la spec
- [ ] 1.2 Verificar que los ajustes de tasa por tramo de score coinciden exactamente con los valores en `CreditScoreRule`
- [ ] 1.3 Verificar que el orden de pagos (fees → interés → principal) está implementado correctamente en `LoanContractAggregate.ApplyPayment`
- [ ] 1.4 Verificar el umbral de auto-default (90 días) en `RecordMissedPayment`
- [ ] 1.5 Verificar que `Restructure` lanza excepción si el nuevo saldo es <= 0

## 2. Cobertura de tests

- [ ] 2.1 Confirmar test unitario para cada regla del `ContractEngine` (aprobación, rechazo, ajuste de tasa)
- [ ] 2.2 Confirmar test de la máquina de estados: todas las transiciones válidas e inválidas del `LoanContractAggregate`
- [ ] 2.3 Confirmar test de distribución de pagos con scenarios: pago parcial, pago exacto, pago que cubre payoff
- [ ] 2.4 Confirmar test de auto-default al registrar pago perdido con >= 90 días
- [ ] 2.5 Confirmar test de reestructuración desde estado `Delinquent` y `Default`

## 3. Sincronización de spec a carpeta principal

- [ ] 3.1 Ejecutar `/opsx:sync` para copiar `specs/loan-contract/spec.md` a `openspec/specs/loan-contract/spec.md`
