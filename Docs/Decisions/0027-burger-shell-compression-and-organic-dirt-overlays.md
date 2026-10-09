# ADR 0027: proporciones del shell de Hamburger y overlays de Dirt

Base: VP1B/C validado `e4fb2fd`. Rama `fix/visual-burger-dirt-polish`.
Proyecto entregado: `C:/Users/Usuario/Zero Stars Restaurant M15`.

Las dimensiones físicas históricas de Bun/Patty/Cheese forman una torre. Reducir
las cajas o mover ingredientes rompería la colocación, geometría individual de
contacto/cocción, montaje, proxy del Dish y entrega. La solución autorizada es de
presentación exclusivamente.

`BurgerIngredientVisual` vive en `Visual_VP1BC` con referencia explícita al FoodItem
original. Lee el DishState confirmado y reconocido, orden e identidades originales.
Para Hamburger/Cheeseburger compacta únicamente esos hijos visuales, conservando
diámetro y base. Bottom bun usa un mesh de corte plano sin semillas; top bun conserva
el mesh/material VP1B/C. Los alimentos sueltos son más finos y apoyan su visual en
la base del collider original. Custom Dish no recibe una disposición nueva.
No mueve Food/Dish, modifica colliders ni escribe estados. La comparación restaura
los visuales anteriores de esas mismas unidades, sin clonar alimentos.

Dirt conserva la misma fuente M17 y sus seis renderers por superficie. Un atlas
RGBA con bordes irregulares, grasa, salpicaduras y restos se distribuye en meshes
combinados distintos; no se crean objetos por cada marca. La fuente Grill y el
DirtKind de SurfaceFoodContact seleccionan acabados propios sin un sistema nuevo
de tipos. Una semilla visual estable aporta posición/rotación/escala no equidistantes.
Amount controla aparición, crecimiento y opacidad continuos; AmountsByKind controla
brillo de grasa. Limpiar reduce esos visuales y cero los oculta, sin sanitizar M18.

URP/Lit transparente usa alpha convencional, sin Preserve Specular: un texel
transparente tampoco deja un reflejo rectangular. No proyecta sombras, tiene
collider o intercepta raycasts. No cambia materiales principales ni iluminación.

El instalador añade solo componentes de presentación y campos visuales explícitos.
Mantiene todos los documentos originales y transforms guardados; conserva GUID y
fileIDs. Una repetición no modifica la instalación ni ajustes de arte posteriores.

Límite deliberado: el agregado conserva el proxy grande anterior y las dimensiones
lógicas de sus ingredientes. Antes de confirmar, los apoyos y separación físicos
siguen siendo los históricos; la compactación de la hamburguesa se presenta al
confirmar con F. No cambia el ciclo Food/Assembly/Cooking/Delivery ni M14–M18.

Validación, inventario y capturas en [el informe](../VISUAL-POLISH-BURGER-DIRT.md).
