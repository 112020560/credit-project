# Flujo: Creación de Cliente y Enrolamiento de Socio

Base URL: `https://{host}/api/v1`

> **Nota:** La creación del cliente NO tiene endpoint REST — llega vía RabbitMQ desde el CRM (`CustomerCreated`). Para pruebas manuales, insertar directamente en la base de datos o simular el mensaje.

---

## PASO 0 — Simular CustomerCreated (solo para pruebas)

Insertar directamente el perfil crediticio del cliente:

```sql
INSERT INTO customer_credit_profiles
    (id, external_id, full_name, document_type, document_number,
     credit_score, monthly_income, monthly_debt, created_at, updated_at)
VALUES
    (gen_random_uuid(),
     'a1b2c3d4-0000-0000-0000-000000000001',  -- este es el externalId del CRM
     'María González Solano',
     'CEDULA',
     '112340567',
     720,
     850000,
     150000,
     NOW(), NOW());
```

---

## PASO 1 — Verificar que el cliente existe

```http
GET /api/v1/loans/customer/a1b2c3d4-0000-0000-0000-000000000001
```

`404` → el cliente no está en el sistema, volver al PASO 0.
`200` → listo para continuar.

---

## PASO 2 — Enrolar como socio cooperativo

```http
POST /api/v1/members/enroll
Content-Type: application/json

{
  "externalCustomerId": "a1b2c3d4-0000-0000-0000-000000000001",
  "joinedAt": "2026-08-11T00:00:00Z",
  "initialSharesAmount": 5000.00,
  "sharesCurrency": "CRC"
}
```

**Respuesta `201 Created`:**
```json
{
  "success": true,
  "memberId": "...",
  "memberNumber": "CM-2026-00001",
  "externalCustomerId": "a1b2c3d4-0000-0000-0000-000000000001",
  "joinedAt": "2026-08-11T00:00:00Z"
}
```

**Errores posibles:**
- `400` `"Customer not found..."` → el cliente no existe en `customer_credit_profiles`
- `400` `"Customer is already a cooperative member."` → ya fue enrolado

---

## PASO 3 — Consultar perfil del socio

```http
GET /api/v1/members/a1b2c3d4-0000-0000-0000-000000000001
```

**Respuesta `200 OK`:**
```json
{
  "externalId": "a1b2c3d4-0000-0000-0000-000000000001",
  "memberNumber": "CM-2026-00001",
  "status": "Active",
  "joinedAt": "2026-08-11T00:00:00Z",
  "totalSharesAmount": 5000.00,
  "sharesCurrency": "CRC",
  "numberOfContributions": 1,
  "lastContributionDate": "2026-08-11T00:00:00Z"
}
```

---

## PASO 4 — Consultar aportaciones del socio

```http
GET /api/v1/members/a1b2c3d4-0000-0000-0000-000000000001/shares
```

---

## PASO 5 — Continuar al flujo de préstamo

Con el socio enrolado, usar el mismo `externalCustomerId` para crear un préstamo:

```http
POST /api/v1/loans
Content-Type: application/json

{
  "externalCustomerId": "a1b2c3d4-0000-0000-0000-000000000001",
  "productId": "{productId}",
  "amount": 1500000,
  "currency": "CRC",
  "termMonths": 24,
  "amortizationMethod": "French"
}
```

Ver flujo completo en `Docs/flows/loan-lifecycle.md`.

---

## Limpieza post-prueba

```sql
DELETE FROM cooperative_members   WHERE external_id = 'a1b2c3d4-0000-0000-0000-000000000001';
DELETE FROM customer_credit_profiles WHERE external_id = 'a1b2c3d4-0000-0000-0000-000000000001';
DELETE FROM member_number_sequences WHERE year = 2026;  -- opcional, resetea el contador
```
