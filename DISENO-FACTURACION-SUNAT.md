# Diseño: facturación electrónica SUNAT (Factura, Boleta, Nota de crédito, Nota de débito)

Estado (2026-11-08): **backend implementado y probado contra el Beta de SUNAT** (rama `feat/facturacion-electronica`). Falta la pantalla del frontend. Ver sección 13.
Lo marcado **[VERIFICAR]** es algo que hay que confirmar contra la documentación vigente de SUNAT o con
el estado real de la empresa antes de programarlo; no se da por cierto.

## 1. Objetivo y alcance

Emitir comprobantes electrónicos desde el ERP, con el mismo nivel de control que ya tienen las guías de remisión:

| Fase | Documento | Código SUNAT |
|---|---|---|
| 1 | Factura electrónica | 01 |
| 1 | Nota de crédito / Nota de débito | 07 / 08 |
| 1 | Comunicación de baja (anular facturas y notas) | RA |
| 2 | Boleta de venta | 03 |
| 2 | Resumen diario de boletas (y anulación de boletas) | RC |
| 3 | Factura con detracción / retención / anticipos / exportación | variantes de 01 |

Fuera de alcance por ahora: factura de contingencia, comprobante de percepción/retención emitido por nosotros,
liquidaciones de compra, y el módulo de contabilidad (asientos y PLE), que se diseña aparte y consume estos datos.

Por qué fases: una constructora factura sobre todo **facturas a clientes empresa** (valorizaciones, servicios,
venta de materiales). Las boletas exigen el resumen diario, que es una segunda mecánica; se hacen después.

## 2. Qué se reutiliza de las guías (ya funciona)

| Pieza existente | Reuso |
|---|---|
| `XmlDsigSigner` / `IXmlSigner` (XMLDSig enveloped dentro de `ext:UBLExtensions`) | **Directo.** Es la misma firma para todo UBL 2.1. |
| Certificado `.p12` y su configuración | **Directo.** El mismo certificado de la empresa. |
| Patrón `Settings` + `Ambiente` Beta/Producción | Se replica en una sección nueva `SunatCpe`. |
| Patrón constructor UBL (`GuiaRemisionUblXmlBuilder`) | Mismo estilo; cambia el esquema (`Invoice` / `CreditNote` / `DebitNote`). |
| Guardado de XML firmado, hash, CDR y QR en la tabla | Mismo patrón de columnas. |
| Reglas de estado (borrador, enviado, aceptado, rechazado, anulado) | Mismo ciclo de vida. |

**Lo que NO se reutiliza: el canal de envío.** Las guías van por la API REST nueva con OAuth2
(`api-cpe.sunat.gob.pe`). Para facturas, boletas y notas SUNAT sigue usando el servicio web **SOAP `billService`**
(`sendBill`, `sendSummary`, `getStatus`, `getStatusCdr`) con autenticación WS-Security (usuario `RUC + usuario SOL` y clave SOL)
**[VERIFICAR si SUNAT ya publicó una API REST equivalente para comprobantes y desde qué fecha es obligatoria]**.
Por eso el cliente de envío es una pieza nueva: `ISunatCpeClient`.

## 3. Decisión de proveedor (hay que decidirla antes de codificar)

| Opción | A favor | En contra |
|---|---|---|
| **A. Emisión directa a SUNAT (SEE-SOL / sistema propio)** | Sin costo por documento. Control total. Reusa firma y certificado. | Hay que estar autorizado como emisor electrónico por sistema propio **[VERIFICAR que H&P lo esté]**. Nosotros mantenemos el cambio normativo. |
| **B. OSE (Operador de Servicios Electrónicos)** | Valida antes de SUNAT, soporte, menos rechazos. | Costo mensual o por documento y dependencia de un tercero. |
| **C. PSE** | Es quien emite por nosotros. | Pierde control y el flujo queda fuera del ERP. |

**Recomendación: A**, con la capa de envío detrás de una interfaz (`ISunatCpeClient`) para poder enchufar un OSE
sin tocar el resto, si SUNAT o la empresa lo exigen.

## 4. Arquitectura

Nuevo módulo `FacturacionElectronicaModule`, con la misma estructura que `GuiasRemisionModule`:

```
FacturacionElectronicaModule/
  Application/   Dtos, Interfaces, Services (EmisionService, SerieService, AnulacionService, ResumenDiarioService)
  Infrastructure/
    Models/      Comprobante, ComprobanteItem, ComprobanteReferencia, SerieComprobante, ...
    Sunat/       ComprobanteUblXmlBuilder (Invoice/CreditNote/DebitNote), SunatCpeSoapClient, CdrParser
  Presentation/  ComprobantesController, SeriesController
```

Flujo de emisión (factura/nota):

```
Borrador ──emitir──► XML UBL 2.1 ──► firma XMLDSig ──► ZIP ──► sendBill (SOAP)
                                                              │
                          ┌───────────── CDR (ZIP con XML) ◄──┘
                          ▼
              ACEPTADO (0)  /  ACEPTADO CON OBSERVACIONES (4000+)  /  RECHAZADO (2000-3999)
```

`sendBill` responde el CDR en la misma llamada (a diferencia de la GRE, que usa ticket). Para el resumen diario y la
baja sí hay ticket y se consulta con `getStatus`. Eso lo cubre `ResumenDiarioService` y un job en segundo plano.

## 5. Modelo de datos (tablas nuevas, prefijo `lb_`, script en `Migrations_Manual/`)

- `lb_serie_comprobante`: tipo (01/03/07/08), serie (`F001`, `B001`, `FC01`…), correlativo actual, establecimiento, activa.
- `lb_comprobante`: tipo, serie, número, fecha emisión y vencimiento, moneda, tipo de cambio, cliente (tipo y número de documento, razón social, dirección),
  gravado / exonerado / inafecto, IGV, total, forma de pago (contado/crédito y cuotas), detracción (código, % y monto), retención, estado,
  `xml_firmado`, `hash_firma`, `cdr_xml`, `sunat_codigo`, `sunat_descripcion`, `qr_url`, `contrato_id`, `proyecto_id`, auditoría.
- `lb_comprobante_item`: descripción, unidad SUNAT, cantidad, valor unitario, precio unitario, tipo de afectación IGV, descuento, total; `producto_id` opcional (descuenta stock si corresponde), `partida_id` opcional.
- `lb_comprobante_referencia`: para notas, el comprobante que modifican y el motivo (catálogos 09 y 10).
- `lb_comprobante_envio`: historial de cada intento (fecha, request, respuesta, error), para soporte y reintentos.
- `lb_baja_resumen`: comunicaciones de baja y resúmenes diarios (ticket, estado, CDR).

**Correlativo sin huecos ni duplicados:** se asigna **al emitir**, no al crear el borrador, dentro de una transacción con bloqueo de fila
sobre `lb_serie_comprobante` (`SELECT ... FOR UPDATE`). Si SUNAT rechaza por un error de datos, el número se quema y se re-emite con el siguiente;
la regla es que **nunca se reutiliza un número ya enviado**.

## 6. Reglas de negocio y validaciones antes de enviar

Se validan localmente, para no gastar un rechazo de SUNAT:
- RUC del cliente con dígito verificador (ya existe `ComprobanteValidador.RucValido`, se extrae a `Shared`).
- Factura solo a cliente con RUC; boleta con DNI o sin documento (tope por monto sin identificar **[VERIFICAR monto vigente]**).
- Total = suma de líneas + IGV; redondeos a 2 decimales con la misma regla que SUNAT (error típico: diferencia de centavos).
- IGV: tasa 18 % **[VERIFICAR que no haya cambios]**. Operaciones gravadas, exoneradas e inafectas por separado.
- Detracción: si el total supera el umbral y la operación está en el anexo, obligatoria la leyenda 2006 y los datos de la cuenta del Banco de la Nación.
- Fecha de emisión: no futura y dentro del plazo que SUNAT acepta **[VERIFICAR plazo vigente]**.
- Notas: el comprobante referenciado debe estar **aceptado**; el motivo define qué se puede modificar.
- Anulación: factura por comunicación de baja dentro del plazo **[VERIFICAR]**; después corresponde nota de crédito.

## 7. Representación impresa (PDF)

PDF con los datos obligatorios, el **QR** (RUC, tipo, serie, número, IGV, total, fecha, tipo y número de documento del cliente, hash) y el hash de la firma.
Se reutiliza el generador que ya usa el PDF de guías. El XML firmado y el CDR se guardan siempre: son constancia legal.

## 8. Integración con el resto del ERP (donde está el valor)

- **Contratos y partidas (`lb_contrato`, `lb_partida`)**: emitir la factura desde una valorización, con el avance por partida como líneas.
- **Almacén / kardex**: una factura de venta de material genera la salida de stock (o se vincula a la guía de remisión que ya la generó).
- **Guías de remisión**: referenciar la guía en la factura (`DespatchDocumentReference`) para el cruce.
- **Cuentas por cobrar** (siguiente módulo): cada factura emitida nace con su cronograma de cobro.
- **Contabilidad**: el comprobante emitido es la fuente del Registro de Ventas (PLE 14.1).

## 9. Seguridad y operación

- **Ambiente separado.** En tu `appsettings.Local.json` las **guías están configuradas en `Produccion`** con credenciales reales. Para desarrollar facturas
  hay que apuntar a **Beta** o se emitirían documentos reales con valor tributario desde una PC de desarrollo. La sección `SunatCpe` tendrá su propio `Ambiente`,
  con **Beta por defecto**, y un candado: en Producción exige una bandera explícita.
- Credenciales (clave SOL, certificado) solo en configuración de servidor, nunca en el repositorio; el `.p12` ya vive fuera del proyecto.
- **Reintentos con idempotencia**: si el envío falla por red, antes de re-enviar se consulta `getStatusCdr` para saber si SUNAT ya lo recibió.
- **Permisos de rol** para emitir y anular, y **auditoría** de quién emitió cada comprobante.
- Registro de cada intento en `lb_comprobante_envio` y alerta cuando un comprobante lleva horas sin respuesta.

## 10. Pruebas

1. Beta de SUNAT con el usuario de pruebas **[VERIFICAR credenciales y certificado de prueba vigentes]**: factura simple, con descuento, con detracción, en dólares, nota de crédito, baja.
2. Casos de rechazo conocidos (RUC inválido, total que no cuadra, serie inexistente) para comprobar que el mensaje llega claro al usuario.
3. Pruebas unitarias del cálculo de IGV y redondeo, y del constructor XML contra ejemplos oficiales.
4. Validación XSD / del validador de SUNAT antes de pasar a producción.
5. Primera emisión real en producción: un comprobante pequeño, acompañado.

## 11. Plan por etapas

| Etapa | Entrega | Estimación orientativa |
|---|---|---|
| 0 | Decisiones abiertas (sección 12) y credenciales Beta | 1 día |
| 1 | Modelo de datos, series, cálculo de impuestos y validaciones | 2–3 días |
| 2 | XML UBL 2.1 de factura + firma + cliente SOAP + CDR (contra Beta) | 3–4 días |
| 3 | Pantallas: emitir, listar, ver estado, descargar XML, CDR y PDF | 3–4 días |
| 4 | Notas de crédito / débito y comunicación de baja | 2–3 días |
| 5 | Boletas y resumen diario | 3 días |
| 6 | Integración con contratos, kardex y guías; pasada a producción | 3 días |

Son estimaciones de orden de magnitud, no compromisos; la mayor incertidumbre está en las pruebas contra el ambiente Beta de SUNAT.

## 12. Decisiones abiertas (necesito tu respuesta)

1. ¿H&P ya está **autorizada como emisor electrónico por sistema propio** (SEE-SOL), o hoy factura por otro medio (Facturador SUNAT, un OSE, un PSE)?
2. ¿Qué se factura realmente? ¿Valorizaciones de obra, venta de materiales, servicios? Define las líneas y las detracciones.
3. ¿Quién emite hoy las facturas y desde qué sistema? Hay que decidir si se migra de golpe o se corre en paralelo un tiempo.
4. ¿Hay series y correlativos ya en uso (por ejemplo `F001`)? Hay que arrancar el correlativo donde está hoy.
5. ¿Se necesitan boletas (cliente final) o solo facturas a empresas?
6. ¿Dispones de usuario SOL secundario con permiso de envío de comprobantes y de las credenciales Beta?

## 13. Lo aprendido al probar contra el Beta (2026-11-08)

Se emitieron en el Beta de SUNAT y fueron **aceptadas**: factura al contado, factura en dólares al crédito con detracción, factura mixta
(gravado + exonerado), factura de exportación, nota de crédito y nota de débito. La baja envía bien y devuelve ticket.

Reglas reales de SUNAT que aparecieron (no estaban en el diseño):
- **3128**: una factura con detracción lleva tipo de operación **1001**, no 0101. El sistema lo pone solo.
- **2800**: en una exportación (0200) el cliente **no puede identificarse con RUC**; lleva documento del extranjero y país.
- **3107**: en una exportación no va el bloque de IGV (1000), solo el 9995.
- El Beta usa el RUC genérico 20000000001 con usuario MODDATOS; el XML lleva ese RUC como emisor. Funciona con el certificado real de H&P.
- La consulta del ticket de baja (`getStatus`) en el Beta es lenta o se corta a veces. El servicio deja la baja en `BAJA_PENDIENTE` y se reintenta con "Consultar baja".
- Se corrige lo escrito en la sección 4: un comprobante **RECHAZADO se reenvía con el mismo número** (SUNAT no lo registra), igual que las guías.

Notas técnicas: EF tiene reintentos automáticos, así que la asignación de correlativo va dentro de `CreateExecutionStrategy()`.

### Pendiente
1. Pantalla en el frontend (lista, formulario, detalle, series, nota de crédito/débito, baja) y PDF con QR.
2. Correlativo real de H&P (hay que fijar `ultimo_numero` de la serie en producción antes de emitir).
3. Domicilio fiscal, ubigeo y cuenta de detracciones en `SunatCpe` (appsettings).
4. Consultar `getStatusCdr` antes de reenviar tras un corte de red (hoy el envío es seguro pero puede duplicar el intento).
5. Anticipos, retención de IGV, descuentos globales y detracciones con catálogo de bienes/servicios.
6. Aplicar `Migrations_Manual/2026-11-09_lb_facturacion_electronica.sql` en producción.

### Protección contra otro canal en la misma serie (2026-11-08)
- Antes de enviar un comprobante nuevo, el sistema consulta a SUNAT (`getStatusCdr`) si ese número ya existe y **salta al siguiente libre**
  (hasta 10 seguidos; luego frena y pide usar una serie propia). Si la consulta falla, no responde en 8 s o el servicio no existe, **no bloquea**.
- **No se pudo probar contra el Beta**: el servicio de consulta de comprobantes no existe allí (responde 404). La lógica de salto se probó con un cliente simulado;
  la respuesta real de SUNAT se verifica en la primera emisión en producción (códigos de existencia asumidos: 0001, 0002, 0003 [VERIFICAR]). Se apaga con `SunatCpe:VerificarExistenciaAntesDeEmitir = false`.
- Un comprobante **rechazado** puede "Pedir número nuevo": libera el número fallido y toma el siguiente en el reenvío.
- Recomendación operativa: una serie por canal (ej. F001 portal, F002 ERP).

### Boletas y resumen diario (2026-11-08)
- Boleta (03, serie `B001`): DNI o sin documento hasta S/ 700 (cliente "CLIENTES VARIOS"), solo en soles, al contado. Al emitirla se asigna número y se firma su XML como respaldo;
  **no se envía sola**: queda `POR_ENVIAR` hasta el resumen diario.
- Resumen diario (RC): agrupa las boletas de una fecha (máx. 500), se firma y se envía con `sendSummary`; SUNAT responde con un ticket. Estados de boleta:
  `POR_ENVIAR` → `EN_RESUMEN` → `ACEPTADO`; si SUNAT rechaza el resumen vuelven a `POR_ENVIAR`. Anular una boleta aceptada la marca `BAJA_SOLICITADA`: va en el resumen de su fecha con estado 3 y queda `ANULADO`.
- **Probado en el Beta:** 3 boletas aceptadas (DNI, sin documento y exonerada, enviadas de forma individual para validar su XML). El XML del resumen fue validado por SUNAT al recibirlo
  (reglas aprendidas: 2992 y 3504 → cada línea lleva `cbc:Percent` 18.00 del IGV) y devolvió ticket. **No verificado:** el resultado final del ticket, porque el `getStatus` del Beta falla.
- Script: `Migrations_Manual/2026-11-10_lb_boletas_resumen_diario.sql`. Pendiente: boletas con nota de crédito, venta en USD, resumen automático a medianoche (hoy es manual desde la pantalla).
