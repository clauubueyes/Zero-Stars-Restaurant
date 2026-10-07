# ADR 0015: transferencia completa del Dish al cliente

## Contexto

La entrega física debe funcionar desde cualquier posición y mirada del jugador.
DeliveryZone ya consulta exclusivamente el Dish suelto sobre el pad. El servicio
cerraba el pedido y cobraba antes de llamar a `CustomerDishCarrier.Take`, cuyo
resultado booleano ignoraba. El portador retiraba la física del root, pero no
normalizaba todos los Rigidbody de los ingredientes ni sincronizaba sus poses
nativas explícitamente durante la salida.

## Decisión

DeliveryZone conserva la detección física, tolerancias y protección de rechazos.
CustomerServiceLoop identifica la cabeza activa en Service Position y delega en
`CustomerDishCarrier.TryReceive`: valida la capacidad de transferencia antes de
la transacción Domain y, si acepta, fija el objeto original en la misma llamada.
No hay una segunda transferencia que pueda devolver false después del cobro.
La visita pasa a Pay/Reject únicamente al terminar esta operación.

El portador deshabilita todos los Pickup y colliders del agregado y normaliza
todos sus Rigidbody: cinemáticos, sin gravedad, colisiones ni interpolación.
Conserva las poses de los cuerpos respecto al Dish y sincroniza Transform y
Rigidbody al tomarlo y después de cada paso del servicio. El anchor y el cliente
activo son las únicas referencias que determinan el transporte; no hay acceso a
cámara, raycast de interacción, posición del jugador ni otro Update.

## Consecuencias

Se conservan DishItem, DishState, FoodItem, FoodState e IDs originales. No se
clonan alimentos, cambian las reglas de evaluación ni duplican pagos o relojes.
Un rechazo conserva el plato recuperable. Un fallo de configuración física o
pago conserva el pedido abierto, el plato disponible y el portador vacío.
Al llegar a Exit, el servicio desregistra los originales y limpia el Dish; la
cola elimina el cliente y libera su reserva. Cancelar el servicio mantiene la
limpieza previa sin resetear el ledger. La API Take permanece compatible para
herramientas; el lifecycle usa TryReceive.
