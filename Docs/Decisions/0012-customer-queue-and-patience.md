# ADR 0012: cola física e identidad de cliente

Estado: implementado en M10, desde M9 validado (`aa146c3`).

## Contexto

M6 reutilizaba un cliente y M9 preparó cuatro QueuePoints delante del mostrador.
Se necesita una cola física sin pedidos simultáneos ni navegación compleja,
conservando la transacción de entrega/pago y el transporte del plato original.

## Decisión

`CustomerQueueState` reserva hasta cuatro plazas FIFO, **incluida Service Position**.
Cada admisión crea `QueuedCustomerState` con identidad y paciencia independientes.
La reserva empieza en Entrance y permanece durante el resultado y la salida.
Solo al completar Exit se retira la cabeza, se reasignan las plazas y puede
entrar un nuevo cliente. Un cliente entrando conserva sus esquinas pendientes
al cambiar su destino de cola; los demás avanzan una plaza por la misma línea.

`CustomerQueueController` instancia el placeholder inactivo existente y adapta
las reservas a movimiento. `CustomerServiceLoop` sigue siendo el único reloj
de clientes; procesa pasos de 0.05 s y conserva el remanente entre llamadas.
No hay NavMesh, búsqueda global, física autónoma de NPC ni otro Update de cola.
Entrada y avance usan la misma velocidad. Hay 1.25 m entre plazas y separación
mínima al admitir por Entrance. La ruta de salida sigue las esquinas públicas M9.

Solo la cabeza, detenida en Service Position, crea un `OrderState`. Su
`CustomerVisit` conserva la identidad del cliente y un ID de pedido diferente.
La entrega comprueba identidad, posición y estado de esa cabeza antes de usar
el pipeline M6 sin cambios: evaluación, ledger único, pago una vez y transporte
del mismo Dish. Al salir se desregistran sus alimentos y se destruyen plato y
NPC. Rechazar conserva el plato y exige retirarlo antes de otra colocación.
El modo sin cola permanece para pruebas/fixtures M6 aisladas.

La paciencia se configura en el template y se copia a cada admisión; la API
permite un valor individual explícito. Cuenta espera detenido en cola y
Order/Wait en servicio; se pausa andando, durante el resultado y al salir.
El valor restante se limita a cero. **A cero no hay abandono, enfado, penalización
ni efectos sobre pedidos/pagos**: solo debug. No se añade reputación.

## Consecuencias

Nunca hay más de cuatro clientes admitidos ni dos reservas de una plaza. Durante
la salida el mostrador queda libre visualmente, pero la siguiente atención espera
a Exit. Es una política conservadora y verificable; aumentar aforo o solapar
salidas requeriría otra decisión de circulación.

Cancelar servicio limpia la cola y el plato vendido sin restaurar el ledger.
Reactivar crea nuevas identidades; una sesión Play nueva restaura el saldo de
desarrollo M8. No cambia FoodSimulation, assets de comida, input ni paquetes.
