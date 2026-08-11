# Sistema de Gestión de Crédito
## Documento Ejecutivo de Producto

> **Para:** Gerencia de Cooperativas y Financieras
> **Versión:** 1.0 | Agosto 2026

---

## Resumen Ejecutivo

El **Sistema de Gestión de Crédito** es una plataforma digital integral diseñada específicamente para cooperativas de ahorro y crédito, y financieras. Automatiza y centraliza la totalidad del ciclo de vida crediticio: desde la evaluación inicial de un socio hasta la liquidación final de su crédito, pasando por el cobro de intereses, la gestión de mora, la clasificación de riesgo y la generación automática de documentos.

La plataforma permite a la institución operar con múltiples productos de crédito simultáneamente, controlar su cartera en tiempo real y cumplir con los requisitos de clasificación de riesgo de la normativa vigente (SUGEF 1-05 en el caso de Costa Rica).

---

## Capacidades Principales

| Capacidad | Descripción |
|---|---|
| Gestión de socios | Registro, perfil financiero y historial crediticio de cada asociado |
| Préstamos a plazo | Créditos amortizables con 5 métodos de cálculo disponibles |
| Líneas de crédito revolvente | Créditos rotativos con ciclo de facturación mensual |
| Motor de evaluación crediticia | Reglas automáticas de aprobación basadas en perfil del solicitante |
| Gestión de tasas variables | Préstamos indexados a tasas de referencia externas (TBP, LIBOR, etc.) |
| Garantías | Registro de garantías asociadas a cada crédito |
| Cobro automático de intereses | Acumulación diaria automatizada sin intervención manual |
| Gestión de mora | Detección automática de atrasos y reclasificación de estado |
| Clasificación de riesgo | Cartera clasificada conforme a SUGEF 1-05 con provisiones estimadas |
| Documentos en PDF y Excel | Comprobantes, cartas de saldo y tablas de amortización generados al instante |
| Webhooks y notificaciones | Notificaciones automáticas a sistemas externos ante eventos de pago |
| Auditoría completa | Registro trazable de cada operación realizada en el sistema |

---

## 1. Gestión de Socios

El sistema mantiene un perfil financiero completo de cada socio o cliente, que alimenta directamente el motor de evaluación crediticia.

**Información gestionada por socio:**

- Datos de identificación y contacto
- Score crediticio
- Ingresos mensuales declarados
- Nivel de endeudamiento actual (deuda total existente)
- Historial de créditos dentro de la institución
- Garantías aportadas

La información de socios puede sincronizarse automáticamente con sistemas externos de la cooperativa mediante integración de mensajería, eliminando la necesidad de doble digitación.

---

## 2. Productos de Crédito

La plataforma soporta dos grandes familias de productos crediticios, que pueden coexistir para el mismo socio:

### 2.1 Préstamo a Plazo

Un crédito con monto definido, plazo fijo y tabla de pagos calculada al momento del desembolso. Cada cuota puede ser fija o variable dependiendo del método de cálculo elegido.

**Parámetros configurables por producto:**

| Parámetro | Opciones |
|---|---|
| Moneda | CRC, USD u otra |
| Tasa de interés | Fija o variable (indexada a tasa de referencia) |
| Plazo | Libre, en meses |
| Método de amortización | 5 métodos disponibles (ver sección 4) |
| Comisión de originación | Monto fijo opcional al desembolso |
| Garantías requeridas | Configurables por política de suscripción |

### 2.2 Línea de Crédito Revolvente

Un cupo disponible que el socio puede utilizar parcialmente en múltiples disposiciones, pagando un mínimo mensual. A diferencia del préstamo a plazo, el saldo puede subir y bajar a lo largo del tiempo.

**Parámetros configurables:**

| Parámetro | Opciones |
|---|---|
| Límite de crédito | Monto máximo disponible |
| Tasa de interés | Aplicada sobre saldo diario |
| Porcentaje de pago mínimo | Porcentaje del saldo adeudado |
| Monto mínimo de pago | Piso fijo de pago mínimo |
| Día de corte | Día del mes (1–28) en que se genera el estado de cuenta |
| Período de gracia | Días adicionales tras el corte para pagar sin recargo |

---

## 3. Ciclo de Vida de un Préstamo a Plazo

Cada préstamo a plazo pasa por estados bien definidos. El sistema controla automáticamente las transiciones y registra cada cambio con su fecha y motivo.

```
  SOLICITUD         DESEMBOLSO        VIDA ACTIVA         CIERRE
      │                  │                 │                  │
      ▼                  ▼                 ▼                  ▼
 ┌─────────┐       ┌──────────┐      ┌──────────┐      ┌──────────┐
 │Aprobado │──────►│  Activo  │─────►│ Moroso   │─────►│  Default │
 └─────────┘       └──────────┘      └──────────┘      └──────────┘
                         │                 │                  │
                         │                 ▼                  ▼
                         │          ┌──────────┐      ┌──────────────┐
                         └─────────►│ Liquidado│      │Reestructurado│
                                    └──────────┘      └──────────────┘
```

### Estados y sus significados

| Estado | Descripción | Acciones disponibles |
|---|---|---|
| **Aprobado** | El crédito fue evaluado y aprobado. Aún no se entregó el dinero. | Desembolsar |
| **Activo** | El dinero fue desembolsado. El socio está pagando normalmente. | Aplicar pagos, acumular interés, liquidar anticipadamente |
| **Moroso** | El socio tiene cuotas vencidas sin pagar. Intereses siguen acumulando. | Aplicar pagos, reestructurar |
| **Default / Incumplimiento** | La mora escaló a un nivel crítico. Se activa proceso de cobro. | Reestructurar |
| **Reestructurado** | Las condiciones del crédito fueron modificadas (tasa, plazo, quita). | Continúa ciclo normal |
| **Liquidado** | El saldo total fue pagado. Crédito cerrado. | — |

### Transiciones Automáticas

- **Activo → Moroso**: cuando el sistema detecta una cuota impaga al momento de su vencimiento (proceso nocturno automático).
- **Moroso → Activo**: cuando el socio realiza un pago que cubre los atrasos.
- **Moroso → Activo** (tras reestructuración): cuando se modifica el plan de pago.

---

## 4. Métodos de Amortización

El sistema soporta cinco métodos de cálculo de cuotas. La institución elige el método al crear el producto crediticio; el sistema genera la tabla de pagos automáticamente.

### 4.1 Francés (cuota fija)

La cuota mensual es siempre la misma. Al inicio, la mayor parte de la cuota corresponde a interés; con el tiempo, la proporción de capital aumenta. Es el método más utilizado en créditos de consumo e hipotecario.

| N° Cuota | Cuota | Interés | Capital | Saldo |
|---|---|---|---|---|
| 1 | ₡ 338,470 | ₡ 100,000 | ₡ 238,470 | ₡ 9,761,530 |
| 2 | ₡ 338,470 | ₡ 97,615 | ₡ 240,855 | ₡ 9,520,675 |
| ... | ₡ 338,470 | decreciente | creciente | decreciente |
| 36 | ₡ 338,470 | ₡ 3,358 | ₡ 335,112 | ₡ 0 |

### 4.2 Alemán (capital fijo)

La cuota varía: el capital pagado es igual en cada período, pero el interés disminuye conforme baja el saldo. Las primeras cuotas son más altas y las últimas más bajas. Ideal para instituciones que prefieren recuperar capital rápidamente.

### 4.3 Plano (interés sobre capital original)

El interés se calcula siempre sobre el monto original del préstamo, no sobre el saldo. Cuota fija, pero el interés total pagado es mayor que en el método francés. Común en créditos de consumo rápido.

### 4.4 Americano (balloon)

El socio paga solo intereses durante todo el plazo. Al vencimiento, paga el capital completo en una sola cuota. Utilizado en financiamiento empresarial de corto plazo.

### 4.5 Solo Interés

Similar al americano, pero el capital puede pagarse en cuotas al final del plazo. Variante flexible para financiamientos estructurados.

---

## 5. Ciclo de Vida de una Línea de Crédito Revolvente

La línea revolvente tiene su propio ciclo de vida, diseñado para operar como una tarjeta de crédito o línea de sobregiro:

```
  APERTURA              OPERACIÓN              CIERRE
      │                     │                    │
      ▼                     ▼                    ▼
 ┌──────────┐         ┌──────────┐         ┌──────────┐
 │Pendiente │────────►│  Activa  │────────►│  Cerrada │
 └──────────┘         └──────────┘         └──────────┘
                           │    ▲
                           ▼    │
                      ┌──────────┐
                      │Congelada │
                      └──────────┘
```

### Operaciones en Estado Activo

| Operación | Descripción |
|---|---|
| **Disposición de fondos** | El socio utiliza parte de su cupo disponible |
| **Pago** | Abona al saldo; libera crédito disponible |
| **Estado de cuenta** | Generado automáticamente en la fecha de corte |
| **Acumulación de interés** | Calculada diariamente sobre el saldo utilizado |

### Congelamiento Automático

Si el socio no cubre el pago mínimo al vencimiento del período de gracia, la línea se **congela automáticamente**: no puede hacer nuevas disposiciones hasta regularizar. Al recibir un pago que cubra el mínimo, la línea se descongela de manera automática.

### Cálculo del Pago Mínimo

El sistema calcula el pago mínimo mensual como el **mayor** entre:
- Un porcentaje del saldo total adeudado (configurable, ej. 5%)
- Un monto mínimo fijo (configurable, ej. ₡ 25,000)

El pago mínimo nunca puede exceder el total adeudado.

---

## 6. Motor de Evaluación Crediticia

Antes de aprobar cualquier préstamo, el sistema ejecuta automáticamente un conjunto de reglas de suscripción sobre el perfil del solicitante. Las reglas se evalúan en orden de prioridad.

### Reglas de Evaluación

| Regla | Tipo | Criterio |
|---|---|---|
| **Score crediticio mínimo** | Bloqueo total | Rechaza si el score es menor al umbral configurado |
| **Índice de endeudamiento (DTI)** | Bloqueo total | Rechaza si la deuda total supera el porcentaje de ingresos configurado |
| **Monto máximo de crédito** | Bloqueo total | Rechaza si el monto solicitado supera el límite permitido |
| **Préstamos en mora activos** | Bloqueo total | Rechaza si el cliente tiene créditos morosos vigentes |
| **Colateral / Garantía** | Ajuste de tasa | Reduce la tasa si el ratio de garantía es favorable |

Las reglas de **bloqueo total** rechazan la solicitud inmediatamente. Las reglas de ajuste modifican la tasa final dentro del margen autorizado.

### Configuración de Políticas

Los umbrales de cada regla (score mínimo, DTI máximo, monto máximo, etc.) se gestionan mediante la **Política de Suscripción** de la institución, modificable sin necesidad de cambios de sistema. Existe un health check que verifica la vigencia de la política al iniciar el servicio.

---

## 7. Tasas de Interés Variables

Para productos indexados a tasas externas (como la Tasa Básica Pasiva del Banco Central), el sistema gestiona automáticamente los reajustes.

**Funcionamiento:**

1. La institución registra la tasa de referencia vigente (ej. TBP = 5.25%)
2. El préstamo tiene un **spread** adicional sobre esa tasa (ej. spread = 8%)
3. La **tasa efectiva** se calcula automáticamente: TBP + spread = 13.25%
4. Cuando la tasa de referencia cambia, el sistema **recalcula y actualiza** la tasa efectiva de todos los préstamos indexados a esa referencia de manera automática

Esto elimina la necesidad de ajustes manuales préstamo por préstamo al cambiar la tasa de referencia.

---

## 8. Gestión de Garantías

Cada préstamo puede tener una o varias garantías registradas. El sistema registra:

- Tipo de garantía (hipotecaria, prendaria, fianza, etc.)
- Valor de la garantía
- Ratio de cobertura sobre el saldo del préstamo
- Documentación asociada

El ratio de cobertura es utilizado por las reglas de evaluación para ajustar la tasa y determinar la elegibilidad del crédito.

---

## 9. Automatización Operativa

El sistema ejecuta procesos automáticos nocturnos que eliminan la necesidad de intervención manual en operaciones recurrentes:

| Proceso | Frecuencia | Función |
|---|---|---|
| **Acumulación de interés** | Diario (02:00 a.m.) | Calcula y registra el interés generado sobre cada préstamo activo |
| **Detección de mora** | Diario (03:00 a.m.) | Identifica cuotas vencidas y cambia el estado del préstamo a Moroso |
| **Interés revolvente** | Diario | Acumula interés sobre saldos de líneas de crédito activas |
| **Estados de cuenta** | Mensual (fecha de corte) | Genera el estado de cuenta de cada línea revolvente en su fecha configurada |
| **Mora revolvente** | Diario | Detecta incumplimiento de pago mínimo y congela líneas automáticamente |
| **Clasificación de riesgo** | Diario | Reclasifica la cartera según días de atraso (normativa SUGEF 1-05) |
| **Reajuste de tasas** | Cada 6 horas | Actualiza tasas efectivas de préstamos variables cuando cambia la referencia |

Todos estos procesos están protegidos contra ejecución duplicada: si el sistema corre en múltiples servidores, el proceso se ejecuta una sola vez gracias al mecanismo de bloqueo distribuido.

---

## 10. Clasificación de Riesgo (SUGEF 1-05)

El sistema clasifica automáticamente la cartera crediticia conforme a la normativa SUGEF 1-05, calculando la provisión estimada requerida para cada categoría.

### Categorías de Clasificación

| Categoría | Días de Atraso | Provisión Estimada | Perfil de Riesgo |
|---|---|---|---|
| **A1** | Al día | 0% | Riesgo normal |
| **A2** | 1 – 30 días | 0.5% | Riesgo normal con alerta |
| **B1** | 31 – 60 días | 1% | Riesgo especial |
| **B2** | 61 – 90 días | 5% | Riesgo especial alto |
| **C1** | 91 – 120 días | 25% | Dudoso cobro |
| **C2** | 121 – 150 días | 50% | Dudoso cobro alto |
| **D** | 151 – 180 días | 75% | Pérdida probable |
| **E** | Más de 180 días | 100% | Pérdida |

### Reportes Disponibles

- **Resumen de cartera por categoría**: total de préstamos, saldo y provisión requerida por categoría
- **Detalle por categoría**: listado de todos los préstamos clasificados en una categoría específica
- **Reclasificación manual**: posibilidad de degradar manualmente un préstamo a una categoría de mayor riesgo (la mejora de categoría es automática por reducción de días de atraso)

---

## 11. Generación de Documentos

El sistema genera documentos de manera instantánea, directamente desde los datos del sistema, sin necesidad de preparación manual.

### Documentos Disponibles

#### Comprobante de Pago (PDF)
Generado para cada pago registrado. Incluye:
- Datos del socio y número de préstamo
- Fecha y monto del pago
- Desglose: capital aplicado, interés aplicado, comisiones (si aplica)
- Saldo restante tras el pago

#### Carta de Saldo (PDF)
Documento formal que certifica el estado actual de un crédito. Incluye:
- Monto original del préstamo
- Saldo vigente a la fecha
- Tasa de interés aplicable (fija o variable con spread)
- Fecha del próximo pago y monto
- Fecha de vencimiento del crédito
- Advertencia de vigencia del documento (30 días)

#### Tabla de Amortización (PDF o Excel)
Proyección completa del plan de pagos:
- Todas las cuotas con número, fecha, monto total, interés, capital y saldo
- Fila de totales (suma de pagos, intereses y capital durante la vida del crédito)
- Disponible en formato PDF para entrega al socio o Excel para análisis interno

---

## 12. Control y Auditoría

### Idempotencia en Pagos

El sistema garantiza que un mismo pago no pueda registrarse dos veces, incluso si ocurre un error de comunicación. Cada operación de pago puede identificarse con una clave única; si se envía nuevamente, el sistema devuelve el resultado original sin duplicar el registro.

### Auditoría de Operaciones

Cada operación sensible (creación de contrato, desembolso, aplicación de pago) queda registrada con:
- Acción realizada
- Entidad afectada
- Usuario que ejecutó la operación
- Fecha y hora exacta
- Detalle de los datos procesados

### Notificaciones Automáticas (Webhooks)

Los sistemas externos de la institución (apps móviles, portales de socios, sistemas contables) pueden suscribirse para recibir notificaciones automáticas cuando ocurren eventos:

| Evento | Descripción |
|---|---|
| Pago aplicado | Se confirma un pago en el sistema |
| Cuota vencida | Un socio no pagó a tiempo |
| Desembolso realizado | Se entregó el dinero de un crédito |
| Préstamo liquidado | Un crédito fue pagado en su totalidad |
| Préstamo en default | Un crédito escaló a estado de incumplimiento |

Las notificaciones están firmadas digitalmente (HMAC-SHA256) para garantizar su autenticidad.

---

## 13. Indicadores de Cartera

El sistema expone en tiempo real los siguientes indicadores de cartera:

| Indicador | Fuente |
|---|---|
| Saldo total de cartera activa | Suma de saldos en `rm_loan_summaries` |
| Cartera en mora (por días de atraso) | Vista de préstamos `Delinquent` |
| Cartera en default | Préstamos en estado `Default` |
| Préstamos liquidados en el período | Filtro por fecha en préstamos `PaidOff` |
| Provisiones requeridas por categoría SUGEF | Clasificación automática diaria |
| Crédito disponible en líneas revolventes | `AvailableCredit` en líneas activas |
| Intereses acumulados pendientes de cobro | `AccruedInterest` proyectado |

---

## 14. Integridad y Confiabilidad del Sistema

### Trazabilidad Total (Event Sourcing)

Cada cambio de estado en un crédito se almacena como un evento inmutable con fecha, versión y hash de integridad. Esto significa que es posible reconstruir el estado exacto de cualquier crédito en cualquier momento del pasado, lo que facilita auditorías, resolución de disputas y cumplimiento regulatorio.

### Consistencia de Mensajes

Los mensajes enviados a sistemas externos se almacenan en la misma base de datos que el evento de negocio (patrón outbox), garantizando que nunca se pierda una notificación aunque ocurra un error de red o reinicio del servidor.

### Alta Disponibilidad

El sistema está diseñado para operar en múltiples instancias simultáneas. Los procesos automáticos nocturnos incluyen mecanismos de coordinación para garantizar que solo una instancia ejecute cada proceso, evitando duplicaciones.

### Observabilidad

El sistema emite trazas distribuidas, métricas y logs estructurados compatibles con herramientas estándar de monitoreo (OpenTelemetry, Seq, Grafana). Esto permite a los equipos de TI diagnosticar problemas rápidamente y garantizar la disponibilidad del servicio.

---

## Resumen de Capacidades por Perfil de Usuario

### Gerencia / Directivo

- Dashboard de cartera en tiempo real
- Clasificación de riesgo SUGEF 1-05 con provisiones
- Indicadores de mora y recuperación
- Reportes de cartera por producto, moneda, estado

### Oficial de Crédito

- Evaluación automática de solicitudes
- Creación y desembolso de préstamos
- Registro de garantías
- Gestión de reestructuraciones
- Generación de cartas de saldo y tablas de amortización

### Caja / Pagos

- Aplicación de pagos con comprobante instantáneo
- Consulta de saldo e historial de pagos
- Cálculo de monto de liquidación anticipada

### Sistemas / TI

- API REST con documentación Swagger
- Integración via webhooks y mensajería
- Configuración de políticas de suscripción
- Ejecución manual de procesos automáticos
- Monitoreo y health checks

---

*Sistema de Gestión de Crédito — Documento para presentación ejecutiva. Agosto 2026.*
