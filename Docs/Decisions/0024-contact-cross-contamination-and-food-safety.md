# ADR 0024: contaminación cruzada por contacto y seguridad independiente

## Estado

Implementado en M18 desde M17 `9ee1360`, rama
`feature/cross-contamination-food-safety`. Proyecto entregado en la carpeta M15.

## Decisión

Cada superficie y unidad de comida conserva un ContaminationState propio,
independiente de DirtState, frescura y CookingState. FoodSafetyPolicy es Domain
sin Unity y configura emisiones de carne Raw/Undercooked y fracciones por dirección.
Las reglas no consultan nombres de objetos, jugador, electricidad ni reputación.

SurfaceFoodContact reutiliza los episodios de contacto M17. Solo nuevas entradas
reciben/transmiten dosis; las listas de IDs deduplican múltiples registros. Se captura
el estado previo de superficie para evitar reflexión de la nueva dosis al propio
donante durante la misma entrada. La intensidad se combina por máximo por categoría,
no por suma: repetir/contactar en ciclos no amplifica la carga.

Las trazas inmutables conservan procedencia original y último donante; se retiene
solo la procedencia dominante por categoría para limitar memoria y ruido. Una
copia de solo lectura en IngredientSnapshot conserva evidencia al servir/destruir.
M15 sigue leyendo su propiedad normal IsContaminated y M16 su CustomerConsequence;
M14 sigue pagando por cantidades, sin dependencias de superficies sobre esos sistemas.

El soporte explícito y su región definen la superficie completa. Se consulta la
geometría individual conservada al retirar colliders en DishItem; nunca se atribuye
el proxy completo a cada ingrediente. La aproximación de bounds/tolerancia admite
placeholders del prototipo, sin alterar fuerzas, agarre ni colliders originales.

CleaningTool solo limpia Dirt. Sanitizar es una operación de desarrollo explícita,
configurable por fracción en Inspector. No borra contaminación de alimentos ni
evidencia histórica. CookingState tampoco sanitiza. Los estados de sesión sobreviven
a Next Day y cambios de cliente, sin almacenamiento en disco ni otro reloj.

## Consecuencias y límites

Contacto continuo no retransfiere al cambiar la contaminación de superficie: exige
salida/reentrada. Sanitizar un soporte ocupado tampoco reemite automáticamente hasta
otro episodio. Las pruebas documentan este límite predecible del modelo por eventos.

Food → Food queda pendiente porque la física cambia al finalizar Dish: los colliders
por ingrediente se retiran y el proxy no representa contactos individuales. Dar
transferencia solo a alimentos sueltos produciría comportamientos distintos antes y
después de F. Se prioriza Food ↔ Surface fiable sin introducir colisiones artificiales.

La adaptación de la escena añade únicamente referencias/componentes y conserva el
texto de los documentos originales, transforms y materiales. Inspector separa debug
sanitario del prompt normal de Dirt. Sin UI/HUD adicional, enfermedades concretas,
productos de limpieza, kill temperatures, inspecciones ni M19.
