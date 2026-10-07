# ADR 0019: entrega en pad, HUD común y electricidad pendiente

Fecha: 2026-10-07. Estado: implementado; comprobación visual manual pendiente.
Corrige el estado de M13; sustituye el cobro eléctrico diario de ADR 0018.

## Decisión

DeliveryZone es la única puerta espacial del servicio. CustomerServiceLoop
referencia explícitamente esa zona; su API pública vuelve a pasar por ella.
La detección toma posición, rotación y footprint del BoxCollider del pad verde;
el trigger existente proporciona altura y habilitación, sin una posición de
entrega independiente. Esto permite mover el pad sin mantener dos transforms
espaciales sincronizados. La colocación acepta overlap parcial y exige apoyo,
baja velocidad, proxy activo y Pickup libre. No consulta jugador, cámara o input.

Solo el cliente activo en Service Position recibe el pedido. El carrier verifica
propiedad antes de la transacción de evaluación/pago y adjunta sin callbacks el
mismo Dish aceptado. Bloquea pickups/colisiones del agregado, conserva ingredientes
y Plate opcional, mantiene escala mundial y sincroniza sus cuerpos nativos.
LateUpdate reafirma únicamente la pose visual tras física, sin avanzar simulación.
La salida del servicio retira las unidades del único FoodSimulation y destruye el
agregado en Exit; la cancelación explícita del servicio conserva su cleanup previo.
El rechazo deja el Dish disponible y exige retirarlo/recolocarlo antes de otro envío.

DebugHudPresenter es el único OnGUI. Los componentes de feedback conservan sus
referencias y helpers, y pasan a producir texto. DebugHudLayout distribuye zonas
disjuntas para jornada, economía, electricidad, servicio, contexto y mensajes.
El resumen sustituye solo la zona central de contexto; no invade los otros bloques.
Paneles con clipping y scroll limitan ingredientes/recibos arbitrariamente largos.
En ventanas pequeñas se escala un canvas lógico mínimo de 960×640. No se cambia
captura/input FPS ni se introduce UI artística.

ElectricitySupplyState sigue siendo la fuente de kWh. Junto al histórico y diario
añade PendingKilowattHours y un ID de período. Next Day reinicia solo diarios:
el pendiente cruza jornadas e incluye consumo real mientras se consulta el resumen.
No hay calendario, otro Update eléctrico, noche simulada ni otro saldo.

TrySettleDay cobra solamente la lista de costes fijos. DailySummary separa
ElectricityAccruedCents de ElectricityPaidCents; OperatingNetCents excluye facturas
eléctricas y NetCents representa caja, incluyendo únicamente facturas pagadas.
El importe pendiente visible procede del período vivo, nunca de sumar redondeos
diarios. Se convierte el kWh real a decimal y se redondea una vez el total del
período con MidpointRounding.AwayFromZero a céntimos long.

El menú Inspector **Development: Settle Electricity Bill** llama al único ledger
con ID de período, kWh y tarifa de sesión. PaymentLedger valida toda la aritmética
antes de publicar cargo y ElectricityBillReceipt inmutable. Los IDs impiden repetir
pagos. Solo después se completa ese período; no se tocan diarios, aparatos, comida,
objetos, hora ni alquiler. Sin consumo, la llamada tiene éxito con cero cargos y
ningún recibo nuevo. Consumo positivo que redondee a cero puede registrar un recibo
de cero y cerrar el período. El consumo posterior usa otro ID.

Se permite pagar con el día abierto o al consultar un resumen cerrado. En el
segundo caso el ledger proyecta las nuevas transacciones reales en otro snapshot
inmutable del mismo día y sustituye su entrada de historial, sin volver a cobrar
alquiler ni modificar el snapshot anterior. Así Balance siempre concilia caja.
Next Day toma ese saldo como apertura. Las ventas/compras permanecen bloqueadas
después de liquidar el día y conservan sus reglas anteriores.

## Preservación y límites

Se toma la escena actual del usuario como referencia, sin regenerar layout o assets.
El instalador incremental añade componentes y referencias faltantes y rechaza
referencias conflictivas. Reinstalar conserva objetos, transforms y materiales.
GenerateScene se niega a reemplazar una escena existente incluso en batch.

Valores provisionales: rent 300 céntimos/día, tarifa 30 céntimos/kWh,
saldo de desarrollo 1000 céntimos; configurables antes de Play. No hay factura
mensual automática, consecuencias de deuda, otras recetas/clientes ni M14.
Ver [corrección y pruebas manuales](../DELIVERY-HUD-ELECTRICITY-FIX.md).
