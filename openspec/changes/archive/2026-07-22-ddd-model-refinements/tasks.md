## 1. Tabla underwriting_policies en PostgreSQL

- [ ] 1.1 Crear script `Src/Core/CreditSystem.Infrastructure/Migrations/20260624_AddUnderwritingPoliciesTable.sql` con la tabla `underwriting_policies` (columnas: `id VARCHAR PRIMARY KEY`, `base_interest_rate DECIMAL`, `auto_default_threshold_days INT`, `no_score_behavior VARCHAR`, `created_at TIMESTAMPTZ`, `updated_at TIMESTAMPTZ`)
- [ ] 1.2 Agregar registro seed en el script: `INSERT INTO underwriting_policies VALUES ('default', 8.0, 90, 'approve_with_penalty', NOW(), NOW()) ON CONFLICT DO NOTHING`
- [ ] 1.3 Verificar que el script es idempotente ejecutándolo dos veces en el entorno de desarrollo

## 2. UnderwritingPolicy en Domain

- [ ] 2.1 Crear `Src/Core/CreditSystem.Domain/Models/UnderwritingPolicy.cs` como record con campos `BaseInterestRate (decimal)`, `AutoDefaultThresholdDays (int)`, `NoScoreBehavior (NoScoreBehavior)`
- [ ] 2.2 Crear `Src/Core/CreditSystem.Domain/Enums/NoScoreBehavior.cs` con valores `ApproveWithPenalty` y `Reject`
- [ ] 2.3 Crear `Src/Core/CreditSystem.Domain/Abstractions/Repositories/IUnderwritingPolicyRepository.cs` con método `Task<UnderwritingPolicy> GetActiveAsync(CancellationToken ct = default)`
- [ ] 2.4 Crear `Src/Core/CreditSystem.Infrastructure/Repositories/UnderwritingPolicyRepository.cs` implementando `IUnderwritingPolicyRepository` con Dapper (SELECT de `underwriting_policies WHERE id = 'default'`)
- [ ] 2.5 Registrar `IUnderwritingPolicyRepository` → `UnderwritingPolicyRepository` en DI
- [ ] 2.6 En `Program.cs`, cargar la política en startup y registrarla como singleton: `var policy = await policyRepo.GetActiveAsync(); builder.Services.AddSingleton(policy);`

## 3. ContractEngine usa UnderwritingPolicy

- [ ] 3.1 Agregar `UnderwritingPolicy` como parámetro del constructor de `ContractEngine`
- [ ] 3.2 Reemplazar `private const decimal BaseInterestRate = 8.0m` por uso de `_policy.BaseInterestRate` en `EvaluateAsync`
- [ ] 3.3 Actualizar el registro DI de `ContractEngine` si es necesario (debería resolverse automáticamente al tener `UnderwritingPolicy` como singleton)
- [ ] 3.4 Ejecutar `dotnet build` y confirmar cero errores

## 4. CreditScoreRule usa NoScoreBehavior de la política

- [ ] 4.1 Agregar `UnderwritingPolicy` como parámetro del constructor de `CreditScoreRule`
- [ ] 4.2 Reemplazar el bloque `if (!score.HasValue) return Pass(...)` por lógica que lee `_policy.NoScoreBehavior`:
  - Si `Reject`: retornar `Fail(RuleName, "Credit score required — policy rejects applications without score")`
  - Si `ApproveWithPenalty`: retornar `Pass` con `RateAdjustment = 5.0m` y mensaje explicativo
- [ ] 4.3 Ejecutar `dotnet test` sobre las reglas del `ContractEngine` para confirmar el nuevo comportamiento

## 5. LoanContractAggregate — umbral configurable y evento ContractApproved

- [ ] 5.1 Crear `Src/Core/CreditSystem.Domain/Aggregates/LoanContract/Events/ContractApproved.cs` como record que hereda de `DomainEvent` con campos `CustomerId (Guid)`, `ApprovedRate (InterestRate)`, `ApprovedPrincipal (Money)`, `EvaluationMetadata (Dictionary<string, object>)`
- [ ] 5.2 En `LoanContractAggregate.Create(...)`, emitir `ContractApproved` como segundo evento después de `ContractCreated`, usando la tasa aprobada y el principal
- [ ] 5.3 Agregar el case `ContractApproved e => state` (sin cambio de estado) al switch `ApplyEvent` — el estado ya es `Approved` tras `ContractCreated`
- [ ] 5.4 Actualizar la firma de `RecordMissedPayment` para agregar parámetro `int autoDefaultThresholdDays`
- [ ] 5.5 Reemplazar la constante `if (daysOverdue >= 90)` por `if (daysOverdue >= autoDefaultThresholdDays)` en `RecordMissedPayment`
- [ ] 5.6 Actualizar el caller `PaymentMissedJob.cs` para pasar `_policy.AutoDefaultThresholdDays` al llamar `RecordMissedPayment`
- [ ] 5.7 Ejecutar `dotnet build` y `dotnet test`

## 6. Estandarización de comentarios a inglés

- [ ] 6.1 Buscar todos los comentarios en español en `CreditSystem.Domain/` (líneas que empiecen con `//` y contengan texto en español) y traducirlos o eliminarlos si son auto-evidentes
- [ ] 6.2 Buscar todos los comentarios en español en `CreditSystem.Application/` y traducirlos o eliminarlos
- [ ] 6.3 Verificar específicamente `LoanContractAggregate.cs` (tiene varios comentarios inline en español como `// Para rehidratar desde eventos`, `// Aplicar en orden: fees -> interest -> principal`)
- [ ] 6.4 Verificar `RevolvingCreditAggregate.cs` (tiene comentarios como `// Descongelar si estaba congelado`)

## 7. Verificación final

- [ ] 7.1 Ejecutar `dotnet build CreditBackend.sln` y confirmar cero errores
- [ ] 7.2 Ejecutar `dotnet test` y confirmar que todos los tests pasan
- [ ] 7.3 Confirmar que `ContractEngine` no tiene constantes de tasa base hardcodeadas
- [ ] 7.4 Confirmar que `LoanContractAggregate.RecordMissedPayment` no tiene el número 90 hardcodeado
- [ ] 7.5 Confirmar que `CreditScoreRule` no tiene comportamiento `Pass` incondicional cuando no hay score
- [ ] 7.6 Confirmar que `ContractApproved` aparece en `UncommittedEvents` al crear un contrato aprobado
