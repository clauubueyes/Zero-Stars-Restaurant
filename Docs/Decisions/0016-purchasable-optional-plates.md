# ADR 0016: Plate comprable y Dish independiente del utensilio

Fecha: 2026-10-07. Alcance solicitado antes de M12, desde `5fe1742` y el estado
local actual de escena. Sustituye la reposición gratuita descrita en ADR 0014.

## Decisión

PlateItem es un utensilio físico con ID propio, Rigidbody/BoxCollider/Pickup,
sin FoodState, DishState ni receta. PlateProduct configura un prefab raíz inactivo
y precio positivo en céntimos; default 50. IngredientPurchaseStation añade Plate
como producto final y conserva los tres ingredientes, el mismo PaymentLedger y
OUTPUT. Comprueba fondos y ocupación antes de crear; inicializa un ID único, cobra
una vez y activa. Plate nunca se registra como alimento en FoodSimulation.

La escena deja de usar AssemblySurface y DishTraySupply. Los tres soportes físicos
se conservan en la misma pose y tamaño como PrepSupport1/2/3, sin identidad Dish ni
Pickup. El adaptador retira esos nueve componentes y vacía las referencias de
estación en el input, conservando el montaje libre y los controles existentes.
DishTraySupply queda inerte por compatibilidad con escenas antiguas aún abiertas:
no tiene Update ni crea objetos. No se duplica ni repone ningún utensilio.

PhysicalDishAssembly confirma ingredientes originales por contacto vertical y
apoyo sólido real. Bounds solo filtra candidatos; raycasts sobre los colliders
confirman contacto y evitan agregar cajas rotadas que solo solapan sus AABB. Una
encimera no agrupa ingredientes dispersos. Un Plate puede agrupar varias unidades
que él soporte físicamente; así admite Custom Dish junto a pilas Hamburger y
Cheeseburger. Plate sostenido o ya unido a un Dish no admite otra finalización.

DishItem sigue siendo la raíz de comida y único objeto del pedido. Si su soporte
directo es Plate, incorpora el mismo objeto como hijo, retira su Pickup/física y
amplía el proxy de transporte; su ausencia conserva el mismo algoritmo de comida.
La identidad DishState procede únicamente de la composición, nunca del utensilio.
FoodItem/FoodState, IDs, cooking, freshness y contamination no se recrean.

DeliveryZone, OrderEvaluation, CustomerDishCarrier y lifecycle no consultan Plate.
Entrega, evaluación, pago y fijación siguen usando Dish; el carrier sincroniza
todos los cuerpos hijos, incluido el Plate opcional, y Exit limpia ese mismo
agregado. Composición vendida sigue bloqueada para interacción/reventa. Grill
consulta geometría original por ingrediente retirado; FoodSimulation continúa
siendo el único driver. No se añade conducción de calor a través del utensilio.

## Límites

Se conserva toda la distribución local existente. Solo se añade BuyPlate y su
etiqueta al Procurement actual; el precio no inventa una tienda, controles,
electricidad, M12 ni un sistema de inventario. Persistencia de utensilios y
devolución de platos no forman parte de esta tarea. Plate se limpia con Dish en
Exit, como se solicita.
