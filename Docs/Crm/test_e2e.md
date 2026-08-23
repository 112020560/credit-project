# E2E — Flujo de Originación Crediticio

**Base URL:** `http://localhost:5000/api/v1`

---

### FASE 0 — Setup Admin (una sola vez)

#### 0.1 — Crear reglas de riesgo

```bash
# Regla 1: Edad entre 18 y 65 años (peso 25)
curl -X POST http://localhost:5000/api/v1/risk-rules \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Edad valida",
    "ruleType": "RangeCheck",
    "targetField": "AgeYears",
    "parameters": { "Min": "18", "Max": "65" },
    "weight": 25
  }'
# Capturar: RULE_AGE_ID = response.id
```

```bash
# Regla 2: Ingreso mensual >= 1500 (peso 30)
curl -X POST http://localhost:5000/api/v1/risk-rules \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Ingreso suficiente",
    "ruleType": "ThresholdCheck",
    "targetField": "MonthlyIncome",
    "parameters": { "Value": "1500", "Direction": "above" },
    "weight": 30
  }'
# Capturar: RULE_INCOME_ID = response.id
```

```bash
# Regla 3: Tiene informacion laboral (peso 20)
curl -X POST http://localhost:5000/api/v1/risk-rules \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Tiene empleo",
    "ruleType": "EnumCheck",
    "targetField": "HasWorkInfo",
    "parameters": { "AllowedValues": "True" },
    "weight": 20
  }'
# Capturar: RULE_WORK_ID = response.id
```

```bash
# Regla 4: Tiene domicilio registrado (peso 15)
curl -X POST http://localhost:5000/api/v1/risk-rules \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Tiene domicilio",
    "ruleType": "EnumCheck",
    "targetField": "HasAddress",
    "parameters": { "AllowedValues": "True" },
    "weight": 15
  }'
# Capturar: RULE_ADDR_ID = response.id
```

```bash
# Regla 5: Tiene informacion fiscal (peso 10)
curl -X POST http://localhost:5000/api/v1/risk-rules \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Tiene RFC",
    "ruleType": "EnumCheck",
    "targetField": "HasFiscalInfo",
    "parameters": { "AllowedValues": "True" },
    "weight": 10
  }'
# Capturar: RULE_FISCAL_ID = response.id
```

> **Score máximo posible:** 100. AutoApprove >= 95, AutoReject <= 20, zona media = ManualReview.

#### 0.2 — Crear y activar la matriz de riesgo

```bash
# Reemplazar los 5 IDs capturados arriba
curl -X POST http://localhost:5000/api/v1/risk-matrices \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Matriz v1",
    "autoApproveThreshold": 95,
    "autoRejectThreshold": 20,
    "ruleIds": [
      "<RULE_AGE_ID>",
      "<RULE_INCOME_ID>",
      "<RULE_WORK_ID>",
      "<RULE_ADDR_ID>",
      "<RULE_FISCAL_ID>"
    ]
  }'
# Capturar: MATRIX_ID = response.id
```

```bash
curl -X POST http://localhost:5000/api/v1/risk-matrices/<MATRIX_ID>/activate
# Esperado: 204 No Content
```

#### 0.3 — Crear y activar workflow de aprobacion (2 pasos)

```bash
curl -X POST http://localhost:5000/api/v1/workflows \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Flujo Credito Estandar",
    "steps": [
      { "stepName": "Revision Analista", "order": 1, "requiredRole": "Analyst" },
      { "stepName": "Aprobacion Director", "order": 2, "requiredRole": "Director" }
    ]
  }'
# Capturar: WORKFLOW_ID = response.id
```

```bash
curl -X POST http://localhost:5000/api/v1/workflows/<WORKFLOW_ID>/activate
# Esperado: 204 No Content
```

---

### FASE 1 — Registro del Prospecto

#### 1.1 — Crear prospecto

```bash
curl -X POST http://localhost:5000/api/v1/prospects \
  -H "Content-Type: application/json" \
  -d '{
    "identificationType": "INE",
    "identificationNumber": "GOML900101HDFMZN01",
    "fullName": "Luis Gomez Martinez",
    "displayName": "Luis Gomez",
    "birthDate": "1990-01-15",
    "contacts": [
      { "type": "Email", "value": "luis.gomez@email.com", "isPrimary": true },
      { "type": "Phone", "value": "5512345678", "isPrimary": true }
    ]
  }'
# Esperado: 201 Created
# Capturar: PROSPECT_ID = response.id
# Estado Prospect: Draft
```

#### 1.2 — Enriquecer prospecto

```bash
curl -X PUT http://localhost:5000/api/v1/prospects/<PROSPECT_ID>/enrich \
  -H "Content-Type: application/json" \
  -d '{
    "addresses": [
      {
        "type": "Home",
        "street": "Av. Insurgentes Sur 1234",
        "city": "Ciudad de Mexico",
        "state": "CDMX",
        "country": "MX",
        "postalCode": "03100",
        "isPrimary": true
      }
    ],
    "workInfos": [
      {
        "occupation": "Ingeniero de Software",
        "employerName": "TechCorp SA de CV",
        "salary": 28000
      }
    ],
    "fiscalInfos": [
      {
        "taxId": "GOML900101ABC",
        "taxRegime": "Sueldos y Salarios",
        "economicActivity": "Servicios Profesionales",
        "industry": "Tecnologia"
      }
    ]
  }'
# Esperado: 204 No Content
# Prospect ahora tiene: HasAddress=true, HasWorkInfo=true, HasFiscalInfo=true
# Score esperado con edad 34 + income 28000 + work + address + fiscal = 25+30+20+15+10 = 100
# Pero AutoApproveThreshold=95 → AutoApprove
#
# Para forzar ManualReview: omitir fiscalInfos y addresses → score = 25+30+20 = 75
```

---

### FASE 2 — Solicitud de Credito

#### 2.1 — Crear solicitud

```bash
curl -X POST http://localhost:5000/api/v1/credit-applications \
  -H "Content-Type: application/json" \
  -d '{
    "prospectId": "<PROSPECT_ID>"
  }'
# Esperado: 201 Created
# Capturar: APP_ID = response.id
# Estado CreditApplication: Draft
```

#### 2.2 — Adjuntar documentos requeridos

> Los documentos requeridos son `NationalId` e `IncomeProof`. Sin ambos el submit falla.

```bash
# Documento 1: Identificacion oficial
curl -X POST http://localhost:5000/api/v1/credit-applications/<APP_ID>/documents \
  -H "Content-Type: application/json" \
  -d '{
    "type": "NationalId",
    "storageUrl": "https://storage.example.com/docs/ine-goml.pdf"
  }'
# Esperado: 201 Created
```

```bash
# Documento 2: Comprobante de ingresos
curl -X POST http://localhost:5000/api/v1/credit-applications/<APP_ID>/documents \
  -H "Content-Type: application/json" \
  -d '{
    "type": "IncomeProof",
    "storageUrl": "https://storage.example.com/docs/recibo-nomina-goml.pdf"
  }'
# Esperado: 201 Created
```

---

### FASE 3 — Envio y Evaluacion de Riesgo

```bash
curl -X POST http://localhost:5000/api/v1/credit-applications/<APP_ID>/submit
# Esperado: 204 No Content
#
# Internamente:
#   - Valida documentos requeridos
#   - Corre RiskEngine → score 100 → AutoApprove (con todos los datos)
#     O → score 75 → ManualReview (sin address ni fiscal)
#
# Si AutoApprove → Prospect=Converted, CreditApplication=Approved, Customer creado → FIN
# Si ManualReview → Prospect=Submitted, CreditApplication=InReview → continuar fase 4
```

#### 3.1 — Consultar resultado de la evaluacion de riesgo

```bash
curl http://localhost:5000/api/v1/applications/<APP_ID>/evaluation
# Esperado: 200 OK
#
# Response incluye:
#   id              → EVALUATION_ID (para referencia)
#   totalScore      → puntaje final (0–100)
#   outcome         → "AutoApprove" | "ManualReview" | "AutoReject"
#   suggestedInterestRate / suggestedMaxAmount (si la matriz los calcula)
#   entries[]       → detalle por regla:
#     ruleName           → nombre de la regla
#     targetField        → campo evaluado (AgeYears, MonthlyIncome, etc.)
#     observedValue      → valor que tenia el prospecto
#     passed             → true/false
#     weightedContribution → puntos que aportó al score
```

---

### FASE 4A — Path AutoApprove (FIN)

Si el score fue >= 95, el sistema ya:
- Creo el Customer automaticamente (Status: Active)
- Publico `CustomerCreated`
- Cambio Prospect a `Converted` y CreditApplication a `Approved`

No hay nada mas que hacer.

---

### FASE 4B — Path ManualReview con Workflow (2 pasos)

El workflow tiene `Revision Analista` (paso 1) y `Aprobacion Director` (paso 2). Cada `POST /approve` avanza un paso.

```bash
# Paso 1: Aprobacion del Analista
curl -X POST http://localhost:5000/api/v1/credit-applications/<APP_ID>/approve
# Esperado: 204 No Content
# Internamente:
#   - Registra ApprovalDecision para paso 1 (Revision Analista)
#   - Quedan pasos pendientes → CreditApplication sigue en InReview
#   - Publica ApprovalRequested para paso 2 (Aprobacion Director)
```

```bash
# Paso 2: Aprobacion del Director (ultimo paso)
curl -X POST http://localhost:5000/api/v1/credit-applications/<APP_ID>/approve
# Esperado: 204 No Content
# Internamente:
#   - Registra ApprovalDecision para paso 2
#   - No quedan pasos → convierte Prospect a Customer
#   - CreditApplication → Approved, Prospect → Converted
#   - Publica ApplicationApproved + CustomerCreated
```

---

### FASE 4C — Rechazo en cualquier paso

```bash
curl -X POST http://localhost:5000/api/v1/credit-applications/<APP_ID>/reject \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Capacidad de pago insuficiente segun politica de riesgo"
  }'
# Esperado: 204 No Content
# Internamente:
#   - CreditApplication → Rejected
#   - Prospect → regresa a Draft (puede volver a aplicar)
#   - Publica ApplicationRejected
```

---

### Resumen de IDs a capturar

| Variable | Viene de | Campo en response |
|----------|----------|-------------------|
| `RULE_AGE_ID` | POST /risk-rules | `id` |
| `RULE_INCOME_ID` | POST /risk-rules | `id` |
| `RULE_WORK_ID` | POST /risk-rules | `id` |
| `RULE_ADDR_ID` | POST /risk-rules | `id` |
| `RULE_FISCAL_ID` | POST /risk-rules | `id` |
| `MATRIX_ID` | POST /risk-matrices | `id` |
| `WORKFLOW_ID` | POST /workflows | `id` |
| `PROSPECT_ID` | POST /prospects | `id` |
| `APP_ID` | POST /credit-applications | `id` |

> **Tip para Postman:** usa variables de entorno (`{{PROSPECT_ID}}` etc.) y en el tab "Tests" de cada request captura el ID con:
> ```javascript
> pm.environment.set("PROSPECT_ID", pm.response.json().id)
> ```
