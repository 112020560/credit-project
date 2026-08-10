## Purpose

Exponer endpoints estándar de salud del servicio para que orquestadores (Kubernetes, Docker Compose) y herramientas de monitoreo puedan distinguir entre un servicio vivo (liveness) y un servicio listo para recibir tráfico (readiness).

## Requirements

### Requirement: Endpoint de liveness — /health

El sistema SHALL exponer `GET /health` que retorna `200 OK` con body `{ "status": "Healthy" }` cuando el proceso está corriendo, sin verificar dependencias externas. Este endpoint se usa para liveness probes — si falla, el orquestador reinicia el pod.

#### Scenario: Proceso corriendo sin errores críticos

- **WHEN** el proceso está activo y sin fallas fatales internas
- **THEN** `GET /health` retorna `200 OK`

### Requirement: Endpoint de readiness — /health/ready

El sistema SHALL exponer `GET /health/ready` que verifica activamente las dependencias externas antes de retornar. Retorna `200 OK` si todas las dependencias están disponibles, o `503 Service Unavailable` si alguna falla. Este endpoint se usa para readiness probes — si falla, el orquestador deja de enviar tráfico al pod.

Las verificaciones incluyen:
- **PostgreSQL**: ejecutar `SELECT 1` contra la conexión `CreditDb`
- **RabbitMQ**: verificar que MassTransit puede conectarse al broker
- **UnderwritingPolicy**: verificar que la política está cargada en memoria (no es null)

#### Scenario: Todas las dependencias disponibles

- **WHEN** PostgreSQL responde, RabbitMQ está alcanzable y `UnderwritingPolicy` está cargada
- **THEN** `GET /health/ready` retorna `200 OK` con detalle de cada check en estado `Healthy`

#### Scenario: PostgreSQL no disponible

- **WHEN** la conexión a PostgreSQL falla o excede el timeout
- **THEN** `GET /health/ready` retorna `503 Service Unavailable`
- **THEN** el body incluye el detalle del check fallido con el mensaje de error

#### Scenario: RabbitMQ no disponible

- **WHEN** RabbitMQ no está alcanzable
- **THEN** `GET /health/ready` retorna `503 Service Unavailable`
- **THEN** el body incluye el detalle del check de RabbitMQ en estado `Unhealthy`

#### Scenario: UnderwritingPolicy no cargada

- **WHEN** `UnderwritingPolicy` no está registrada en el contenedor DI (fallo en startup)
- **THEN** `GET /health/ready` retorna `503 Service Unavailable`
