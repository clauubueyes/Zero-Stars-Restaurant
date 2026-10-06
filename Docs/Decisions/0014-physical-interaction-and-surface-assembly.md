# ADR 0014: agarre continuo y montaje sobre apoyo físico

## Contexto

El snap del clic izquierdo competía con el manejo físico. Las estaciones retenían
una bandeja que desaparecía con la venta. Los colliders retirados al finalizar
impedían a Grill detectar los ingredientes originales del agregado.

## Decisión

El clic izquierdo pertenece exclusivamente a hold/release de Pickup. E mantiene
acciones contextuales; F confirma. La liberación conserva velocidad física acotada,
sin impulso añadido, separando movimiento de mano y corrección de adquisición.

PhysicalDishAssembly identifica contacto y apoyo de una pila fuera de estaciones,
sin marcar cada superficie ni modificar alimentos. El preview libera pertenencias;
confirmar usa el mismo DishState/DishItem y pipeline M6 con ingredientes originales.
Las estaciones existentes permanecen compatibles y reciben suministro de un prefab
vacío, con bloqueo geométrico de reposición. Nunca se clona el plato vendido.

FoodItem conserva geometría local antes de retirar colliders. Grill consulta
intersección orientada por ingrediente; FoodSimulation sigue avanzando estado una
sola vez. No se añade propagación de calor entre ingredientes ni reacciones nuevas.

## Consecuencias

No se necesita AssemblySurface para las superficies habituales del greybox.
El montaje depende de contacto físico y tolerancias configurables; no recoge
ingredientes sueltos de un área grande. Los apoyos vacíos de las estaciones siguen
fijos. Las APIs de snap/Throw históricas quedan para pruebas y desarrollo.
El cambio no incluye M12, recetas nuevas ni persistencia.

## Ajuste solicitado: mantener facilidad de montaje

El usuario valida hold/release y solicita conservar autoensamblaje. El press sigue
agarrando; release intenta colocación asistida solo con mano tranquila y apoyo/pila
cercana, usando el primer hit sólido y validando obstáculos/pertenencia. Las bandejas
usan el snap M5; otros apoyos alinean bounds sobre la pila sin crear comida ni cambiar
FoodState. G y gestos rápidos conservan liberación libre. F sigue confirmando.
InteractionInput referencia explícitamente DishAssemblyInteraction para decidir la
colocación en el momento de release; no hay otro driver ni búsqueda global.
