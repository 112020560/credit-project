# Credit API Gateway — YARP Proxy (Paso 1: básico, sin seguridad)

## Contexto

El repo `credit_gateway` está vacío salvo por `CreditApiGateway.slnx` y un
`README.md` ("cace_gateway — Yarp Gateway"). Este es el primer paso de un
gateway construido sobre YARP para exponer/proxyar una API existente que
corre en `http://localhost:5156` (cuyo OpenAPI está en
`http://localhost:5156/swagger/v1/swagger.json`).

Este paso es intencionalmente mínimo: sin autenticación, sin
transformaciones, sin rate limiting. El objetivo es tener un proxy
funcional configurado desde `appsettings.json`, más una UI de
documentación (Scalar) que renderice el spec del backend a través del
propio proxy.

## Alcance

Incluye:
- Proyecto ASP.NET Core `CreditApiGateway` (.NET 10) con `Yarp.ReverseProxy`.
- Ruteo catch-all: toda request al gateway se reenvía tal cual a
  `http://localhost:5156`.
- Configuración de rutas/clusters en `appsettings.json` (sección
  `ReverseProxy`), no hardcodeada en código.
- UI de documentación con `Scalar.AspNetCore`, apuntando al
  `swagger.json` del backend a través del mismo proxy (sin generar un
  OpenAPI propio del gateway).
- El proyecto se agrega al `CreditApiGateway.slnx` existente.

Explícitamente fuera de alcance (YAGNI, para iteraciones futuras):
autenticación/autorización, rate limiting, health checks custom,
transformación de headers/paths, logging estructurado, múltiples
clusters/destinos.

## Arquitectura

Un único proyecto web mínimo:

```
src/CreditApiGateway/
  CreditApiGateway.csproj
  Program.cs
  appsettings.json
  appsettings.Development.json
  Properties/launchSettings.json
```

`Program.cs` registra YARP cargando su configuración desde
`IConfiguration` (`builder.Configuration.GetSection("ReverseProxy")`),
mapea el middleware de proxy, y registra Scalar apuntando a la misma
ruta que el proxy ya reenvía.

No hay `UseAuthentication`/`UseAuthorization` ni ningún middleware de
seguridad en este paso.

## Configuración (`appsettings.json`)

Sección `ReverseProxy` con un route catch-all y un cluster con destino
al backend:

- Route: matchea cualquier path (`"/{**catch-all}"`) → cluster único.
- Cluster: un destino, `http://localhost:5156`.

Esto cubre automáticamente `/swagger/v1/swagger.json`, ya que cualquier
path no definido explícitamente cae en el catch-all y se reenvía al
backend sin necesidad de reglas adicionales.

## Documentación (Scalar)

- Paquete `Scalar.AspNetCore`.
- `app.MapScalarApiReference(options => options.OpenApiRoutePattern =
  "/swagger/v1/swagger.json");`
- La UI queda expuesta en la ruta default de Scalar (`/scalar/v1`).
- Scalar no genera un spec propio (no hay `AddOpenApi()` de Microsoft):
  simplemente pide `/swagger/v1/swagger.json` al propio gateway, que lo
  reenvía al backend vía el catch-all de YARP.

## Puerto del gateway

Puertos default de la plantilla ASP.NET Core (http/https, ej.
`5000`/`5001` o los asignados por Kestrel), definidos en
`Properties/launchSettings.json`. No debe chocar con el `5156` del
backend.

## Testing

Verificación manual (no hay lógica de negocio propia que unit-testear
en este paso):
1. Levantar el backend en `5156`.
2. Levantar el gateway (`dotnet run`).
3. `curl` a `http://localhost:<puerto-gateway>/swagger/v1/swagger.json`
   y confirmar que devuelve el spec del backend.
4. Abrir `/scalar/v1` en el navegador y confirmar que la UI renderiza
   el spec correctamente.

## Fuera de alcance / próximos pasos

- Autenticación (JWT / API key).
- Rate limiting.
- Health checks.
- Transformaciones de request/response (headers, path rewriting).
- Logging/observabilidad estructurada.
- Múltiples clusters/destinos según crezca la API real.
