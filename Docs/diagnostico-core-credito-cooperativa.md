# Diagnóstico del Credit Core — Visión Banca / Cooperativa

**Fecha de análisis:** 2026-08-08
**Revisado por:** Arquitecto de sistemas + experto en banca y finanzas
**Rama analizada:** `development` — commit `b753c8a`

---

## ✅ LO QUE ESTÁ BIEN

### Fundamentos de arquitectura
- **Event Sourcing** bien implementado: audit trail completo por evento — requisito regulatorio no negociable en una cooperativa supervisada
- **Clean Architecture + DDD**: las capas están respetadas, el dominio no tiene dependencias externas
- **CQRS con MediatR**: separación lectura/escritura correcta, con behaviors de validación y logging
- **Outbox pattern**: garantiza entrega at-least-once de mensajes — crítico para integridad financiera
- **Múltiples métodos de amortización** (French, German, Flat, American, InterestOnly): cubre la mayoría de productos crediticios reales
- **Motor de reglas extensible**: `ContractEngine` con hard stops y configuración externalizada en `UnderwritingPolicy`
- **Ciclo de vida completo del préstamo**: `Approved → Active → Delinquent → Default → PaidOff` con transiciones bien modeladas
- **Reestructuración**: operación crítica en una cooperativa, bien modelada
- **Crédito revolvente**: línea de crédito con estados, draws, estados de cuenta mensuales
- **Webhooks**: para integrar con sistemas externos (core bancario, CRM, notificaciones)
- **Workers de fondo**: acumulación de interés diaria, detección de pagos perdidos — automatización operativa esencial

---

## ❌ LO QUE ESTÁ MAL / ES RIESGOSO

### Problemas financieros / de negocio

**1. Cálculo de interés puede ser incorrecto bajo ciertas condiciones**
El `AccrueInterest` calcula `tasa_diaria × saldo_actual × días`. El problema: el sistema no distingue si la tasa almacenada es **nominal** o **efectiva**. En Costa Rica (SUGEF) y en la mayoría de cooperativas la tasa se pacta como tasa efectiva anual. Mezclar nominal/efectiva rompe el cálculo de cuotas del schedule y el interés acumulado.

**2. Late fee calculado fuera del dominio**
`LateFeeConfiguration` vive en Application, y el monto del late fee llega al aggregate como parámetro externo. La política de mora es una **regla de negocio del dominio** — debería vivir ahí. Si el cálculo está mal en el Job, el dominio lo acepta sin validar.

**3. Sin período de gracia**
Toda cooperativa tiene un período de gracia (típicamente 3–8 días) antes de cobrar mora. Actualmente cualquier día de atraso = mora inmediata.

**4. Sin tasa moratoria diferenciada**
Hay late fees (cargo fijo) pero no **interés moratorio** — una tasa superior a la corriente que se aplica sobre el saldo vencido durante el período de mora. Son conceptos distintos y ambos son requeridos regulatoriamente.

**5. Monto máximo de préstamo hardcodeado en la regla**
`MaxLoanAmountRule` tiene `500,000` fijo en código. Si cambia la política, hay que redeployar. Esto debería estar en `UnderwritingPolicy`.

**6. DTI del 50% es demasiado permisivo**
La SUGEF y estándares de Basilea III recomiendan no superar 35–40% de DTI. Al 50% la cooperativa estaría financiando deudores sobre-endeudados.

### Problemas técnicos

**1. Carga bloqueante de la política en startup**
```csharp
// DependencyInjection.cs
services.AddSingleton<UnderwritingPolicy>(sp =>
    sp.GetRequiredService<IUnderwritingPolicyRepository>()
      .GetActiveAsync()
      .GetAwaiter()
      .GetResult());  // ← bloqueo síncrono, riesgo de deadlock
```
Si la DB no está disponible, el servicio no arranca. En producción esto puede causar fallos en cascada durante un restart.

**2. Workers sin distributed locking**
`InterestAccrualWorker` y `PaymentMissedWorker` no tienen ningún mecanismo de exclusión mutua. Con 2 instancias corriendo (lo normal en producción con HA), el interés se acumula **dos veces por día**. Bug de producción crítico.

**3. Sin autenticación ni autorización en ningún endpoint**
Cualquiera con acceso a la red puede crear contratos, aplicar pagos, hacer defaults. En una cooperativa los endpoints deben estar protegidos por rol: oficial de crédito, analista, cajero, gerente, auditor.

**4. Proyecciones sin idempotencia**
Si el event store re-entrega un evento (at-least-once), los projectors pueden duplicar registros en las read models. No hay manejo de `event_id` como clave única en las proyecciones.

**5. `CustomerReadRepository` inconsistente**
Recibe `connectionString` como string directo en el constructor, mientras el resto del sistema usa `IConfiguration`. Rompe el patrón.

---

## 🚧 LO QUE LE FALTA para operar en una cooperativa

### Crítico — sin esto no puede operar

| # | Funcionalidad faltante | Por qué es crítico |
|---|---|---|
| 1 | **Autenticación JWT + roles** (oficial, analista, cajero, gerente, auditor) | Sin esto cualquiera puede operar el sistema |
| 2 | **Flujo de aprobación multi-nivel / Comité de crédito** | Las cooperativas no aprueban automáticamente — requieren firma de un oficial y, sobre cierto monto, aprobación del comité |
| 3 | **Concepto de Socio (Member) con aportaciones** | Una cooperativa presta a sus **socios**. El socio tiene aportaciones de capital que determinan cuánto puede pedir |
| 4 | **Catálogo de Productos de Crédito** | Préstamo personal, hipotecario, vehicular, consumo, microcrédito — cada uno tiene tasa, plazo máximo, LTV y colateral distintos |
| 5 | **Garantías como entidad del dominio** | El colateral hoy es un `decimal?`. Una garantía real es: tipo (hipoteca, prenda, fianza, depósito a plazo), bien, valor de avalúo, registro |
| 6 | **Período de gracia** | Regla básica de toda cooperativa |
| 7 | **Tasa moratoria** | Requerimiento legal — se cobra sobre el saldo vencido durante la mora |
| 8 | **Categorías de riesgo de la cartera (SUGEF 1-05)** | Grupos A/B/C/D/E con estimaciones (provisiones) — obligatorio para cooperativas supervisadas en CR |
| 9 | **Tabla de amortización como endpoint público** | El socio necesita ver cuánto paga cada mes, cuánto es interés y cuánto capital |
| 10 | **Comisiones de originación / formalización** | La cooperativa cobra una comisión al formalizar el préstamo — debe quedar en el contrato y en el estado de cuenta |

### Importante — afecta operación diaria

| # | Funcionalidad | Impacto |
|---|---|---|
| 11 | **Registro de pagos por canal** (SINPE, ventanilla, débito automático, ACH) | Trazabilidad de pagos para conciliación contable |
| 12 | **Comprobantes y documentos** (comprobante de pago, carta de saldo, constancia) | El socio los necesita; también el oficial |
| 13 | **Concentración y límites de exposición** | Un socio no puede tener más de X% de la cartera total |
| 14 | **Tasa variable / reajustable** | Muchos préstamos de cooperativa van ligados a la tasa básica pasiva del BCCR |
| 15 | **Multi-moneda real con tipo de cambio** | Tienen USD y CRC pero sin conversión de referencia ni riesgo cambiario modelado |
| 16 | **Distributed locking para workers** | Sin esto no pueden escalar horizontalmente |
| 17 | **Healthchecks** (DB, RabbitMQ, Redis) | Requerido para HA y Kubernetes |
| 18 | **Idempotency keys en pagos** | Un pago doble es un error financiero grave |
| 19 | **Soft audit trail (quién hizo qué)** | Toda operación financiera debe tener usuario + timestamp |
| 20 | **Exportación de reportes** (cartera, mora, provisiones) | Para el regulador y para la junta directiva |

---

## Hoja de ruta — Fases de implementación

### Fase 1 — Fundamentos operativos
> Sin esto la cooperativa no puede abrir operaciones

1. **Autenticación y autorización** — JWT + roles (oficial de crédito, analista, cajero, gerente, auditor)
2. **Concepto de Socio (Member)** — membresía, aportaciones de capital, relación con CustomerCreditProfile
3. **Catálogo de Productos de Crédito** — tipo de producto, tasa, plazo máximo, LTV, colateral requerido
4. **Garantías como entidad de dominio** — tipo, bien, valor de avalúo, estado, registro

### Fase 2 — Cumplimiento regulatorio
> Requerido por SUGEF para cooperativas supervisadas

5. **Período de gracia** — días de tolerancia antes de cobrar mora
6. **Tasa moratoria** — tasa adicional sobre saldo vencido durante la mora
7. **Comisiones de originación** — cobro al formalizar el préstamo
8. **Categorías de riesgo SUGEF 1-05** — grupos A/B/C/D/E con estimaciones (provisiones)
9. **Flujo de aprobación multi-nivel** — comité de crédito para montos altos

### Fase 3 — Robustez técnica
> Requerido para operar en producción con alta disponibilidad

10. **Distributed locking en workers** — evitar doble acumulación de interés en multi-instancia
11. **Idempotencia de pagos** — idempotency key a nivel de API
12. **Healthchecks** — DB, RabbitMQ, estado de workers
13. **Audit trail de usuario** — quién ejecutó cada operación

### Fase 4 — Operación completa
> Mejora de experiencia operativa

14. **Canales de pago** — SINPE, ventanilla, débito automático, ACH
15. **Documentos** — comprobante de pago, carta de saldo, tabla de amortización exportable
16. **Tasa variable / reajustable** — vinculada a tasa básica pasiva BCCR
17. **Reportes regulatorios** — cartera, mora, provisiones para el regulador

---

## Notas regulatorias (Costa Rica)

- **SUGEF 1-05**: gestión integral de riesgo crediticio — categorías de deudor, estimaciones mínimas
- **SUGEF 6-00**: reglamento de grupos vinculados — exposición máxima por grupo
- **Artículo 61 Ley SUGEF**: provisiones dinámicas
- **Tasa de interés**: debe expresarse como Tasa Efectiva en Colones (TEC) o equivalente en USD
- **SINPE**: el sistema de pagos del BCCR — integración obligatoria para recepción de pagos

---

*Este documento es el punto de partida para la planificación de las Fases 1-4. Se actualiza conforme avance la implementación.*
