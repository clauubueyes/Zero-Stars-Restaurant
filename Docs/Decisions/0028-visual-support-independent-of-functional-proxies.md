# ADR 0028: apoyo visual separado de proxies funcionales

Bugfix desde `b023fe8`, rama `fix/food-visual-placement-regressions`.

El polish redujo Bun/Patty/Cheese, pero anclaba cada visual suelto a la base de
su collider histórico. AssemblyPlacement sigue colocando cajas funcionales
completas. Aparecían huecos de hasta 12,5 cm entre capas y F cambiaba a otro
algoritmo exclusivo de Hamburger/Cheeseburger, desplazando visuales hasta 33,3 cm.
Además, COLLECT HERE es un revestimiento sin collider 12 mm por encima de la mesa;
el Cheese, de 3,864 mm, quedaba completamente oculto al reposar en la mesa real.

Se conserva el script/GUID BurgerIngredientVisual y se sustituyen sus decisiones
por ID/receta con datos de presentación en los prefabs/componentes: altura normal,
altura de base que sostiene comida y diámetro. Se conservan las proporciones
aprobadas. El runtime consulta contacto geométrico real y bounds de meshes visibles;
solo traslada el shell para apoyarlo en el visual inferior. Base/corte se aplican
ya durante el montaje. La misma relación de apoyo continúa después de F usando
las cajas originales retiradas y el marco del Dish, también al girarlo/transportarlo.
Funciona con pilas parciales y Custom Dish sin modificar reconocimiento/pertenencia.

FoodVisualSupportSurface es metadata de presentación reutilizable en un apoyo
existente, con referencias explícitas a sus overlays. Permite colocar el visual
sobre un revestimiento que sobresale del collider, solo dentro de su footprint.
En la mesa de Procurement referencia Cladding de OutputMarker: eleva únicamente
la presentación ~13 mm, incluido 1 mm para evitar coincidencia de caras.
No añade colisiones, triggers, raycasts interceptores o GameObjects.

InteractionDetector.Detect resuelve la Food visible dentro de una columna física
conectada cuando las cajas históricas contienen el visual de otra capa. Conserva
rango, exclusión del cuerpo sostenido y oclusión por sólidos. TryDetectHit sigue
devolviendo el hit físico original para placement/Assembly. Un test con clic real
evita que el Cheese visible seleccione el Bun de debajo; un obstáculo bloquea la selección.

No cambia AssemblyPlacement, PhysicalDishAssembly, Procurement, FoodState,
DishState, dimensiones/poses físicas, geometría térmica/sanitaria, M14–M18, precios,
recetas, luces, arte/Dirt o materiales. Los raycasts de lectura de contacto son
NonAlloc y no avanzan simulación. No hay otro driver ni registro global.

El instalador añade tres campos visuales por componente existente y un componente
de metadata al apoyo actual bajo OutputMarker. Conserva todos los documentos
restantes; una repetición conserva también los ajustes manuales posteriores.

Validación y capturas en [el informe](../FOOD-VISUAL-PLACEMENT-REGRESSIONS.md).
