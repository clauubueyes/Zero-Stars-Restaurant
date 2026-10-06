# ADR 0008: snap de montaje, agarre del agregado y retirada visible de ventas

Fecha: 2026-10-06. Estado: implementada; validación manual del polish pendiente.
Base aprobada M6: `9335899`. Rama `fix/vertical-slice-polish`.

## Contexto

El usuario valida M6 pero detecta montaje lento, agarre dependiente de dónde se
apunta, desaparición inmediata de ventas y estaciones demasiado separadas.
Se autoriza corregir ese flujo exclusivamente, sin M7 ni nuevas reglas Domain.

## Decisión

- Añadir Player/PlaceIngredient, Button, clic izquierdo. DishAssemblyInteraction
  tiene su propia copia del input, revalida el primer hit/alcance y adapta intención
  a AssemblySurface. E/G/botón derecho mantienen manipulación física. El clic que
  recupera cursor no coloca; las acciones M2 tienen prioridad si son simultáneas.
- AssemblyPlacement calcula bounds a orientación de bandeja sin mutar previamente
  el objeto. AssemblySurface apila por altura, margen 4 mm, centro XZ y volumen
  libre dentro del sensor. Solo admite FoodItem real, activo y registrado; claim
  Domain antes de liberar. PhysicalCarry.Drop restaura claim, configuración y
  colisiones; después se coloca la misma unidad, velocidades cero y Sleep. Sigue
  dinámica, extraíble con E y sometida al único FoodSimulation. Sin slots, recetas
  obligatorias, inventario, clones ni cambios de estado/reglas Domain.
- DishItem conserva un proxy BoxCollider que abarca todos los ingredientes, y
  retira sus colliders/Pickup individuales al confirmar. Detector resuelve el
  primer ancestro interactuable activo del primer hit sólido. Un hijo retirado no
  oculta al dueño agregado. Para todas las composiciones se configura elevación
  mínima 5° en Pickup; default -90° preserva alimentos/cajas. PhysicalCarry limita
  dirección de transporte y permite aproximación inicial desde suelo: tolerancia
  de error comienza en la distancia al destino validado y decrece hasta el tether
  normal. No se aumentan fuerza/velocidad ni se ignoran paredes/cápsula del jugador.
- Tras la transacción Domain intacta, CustomerDishCarrier toma el mismo Dish en un
  anchor `(0, 1.2, 0.65)` del cliente: cinemático, Pickup y colliders desactivados,
  base apoyada a altura del anchor. Se conserva visible durante Pay/Leave, con
  FoodState originales avanzados por el único reloj. La evidencia de evaluación
  permanece histórica e inmutable; IsSold/ledger impiden reventa/doble pago.
- Al completar la salida se desregistran originales antes de destruir el Dish,
  y se oculta el placeholder reutilizable. Cancelar servicio también limpia;
  rechazos quedan disponibles. Movimiento solo mueve; portador solo compone
  representación; CustomerServiceLoop coordina transacción/ciclo sin crear platos.
- Reubicar Prep, Grill y Assembly en una fila compacta al fondo y despejar el
  pasillo hacia Delivery. Conservar objetos, materiales, unidades, fileIDs y GUID;
  generador existente sigue siendo fuente reproducible de la fixture.

## Consecuencias

Montaje rápido opcional; el snap usa bounds conservadores y bandejas horizontales,
no geometría de contacto exacta. El plato mantiene un proxy convexo aproximado y
una esfera de transporte conservadora; uno demasiado grande puede no caber.
El agarre elevado permite recoger mirando hacia abajo; para depositar en cocina
se usa G y gravedad. Cajas/comida mantienen el destino según mirada de M2.

El portador del cliente retira colisiones provisionalmente: la ruta cinemática no
evita obstáculos dinámicos ni existe animación/manos. La limpieza puede ocurrir
antes de salida si se cancela el servicio/se descarga escena. No hay recuperación
de ventas canceladas, persistencia o producción infinita de comida. El cliente se
reutiliza con nuevas identidades de visita/pedido. M7 sigue sin autorización.
