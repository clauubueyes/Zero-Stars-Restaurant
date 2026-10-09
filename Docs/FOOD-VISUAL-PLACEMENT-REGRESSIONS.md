# Corrección de apoyo visual durante montaje y recogida

Base validada `b023fe8`; rama `fix/food-visual-placement-regressions`.
Implementación: `1b0eb80`. Pruebas, auditoría y capturas: `410c207`.
Proyecto real: **`C:/Users/Usuario/Zero Stars Restaurant M15`**. El Editor estaba
cerrado al iniciar. Se conservan los cambios locales de la carpeta original y
SceneTemplateSettings incidental. Sin Rebuild, cambio de milestone, merge o push.

## Causas verificadas en Play

1. `PhysicalDishAssembly.TryPlanPlacement` / `AssemblyPlacement.TryBounds` colocan
   la misma Food usando su collider funcional original, con 4 mm de separación
   inicial. El polish encogía su shell y lo anclaba a la base de esa caja sin leer
   el visual inferior. Así, Patty quedaba 125 mm por encima del Bun visible; el
   Cheese también flotaba. Después de F se ejecutaba una compactación distinta
   exclusiva de las dos recetas. El salto máximo observado fue **333,2 mm** en
   Cheeseburger y **272,4 mm** en Hamburger.
2. La mesa bajo Procurement termina en Y = **0,800 m**; COLLECT HERE / OutputMarker
   termina en **0,812 m** y carece de collider. El Cheese comprado conservaba su
   renderer activo y escala correcta; al caer su visual ocupaba Y = 0,800168 a
   0,804032 m, entero debajo del revestimiento. No era un fallo de compra/estado,
   raycast, render activation o z-fighting. Bun y Patty también penetraban 12 mm,
   pero seguían visibles por su mayor grosor.

Los recibos en `Build/FoodVisualPlacement/Captures/Before/Receipt.txt` registran
mediciones y IDs. Se reprodujo el baseline con su código original antes de editar
el runtime; no se reconstruyó una escena anterior.

## Solución y preservación

`BurgerIngredientVisual` conserva su GUID, referencia al Food original y shells.
Ahora recibe altura/diámetro como datos de presentación: Bun 45 % / base 20 %,
Patty 28 %, Cheese 7 %, diámetro 95 %. Meshes/materiales y proporciones aprobadas
permanecen. No decide por ID de Food o receta en runtime.

Para comida suelta consulta contacto físico próximo con raycasts NonAlloc; los
bounds de los meshes visibles determinan la altura del apoyo. Compacta solo el
shell y conserva XZ/rotaciones originales. La base que sostiene comida recibe ya
su corte/grosor de base, sin esperar F. Para un Dish conserva el mismo grafo de
apoyo usando los BoxCollider retirados y su marco local, sin depender de receta.
Composiciones parciales y Custom Dish siguen el mismo mecanismo. Quitar/recoger
una capa deja de aplicar su relación de apoyo; el objeto sigue siendo el original.

`FoodVisualSupportSurface`, añadido a la mesa física de Procurement, referencia
explícitamente Cladding de OutputMarker. Dentro de ese rectángulo el shell se apoya
1 mm sobre la cara visual. Fuera del rectángulo no adopta su altura. Cheese reposado
queda en **Y = 0,813 m**, visible como loncha fina, y sigue recogiéndose con el proxy
histórico. Bun y Patty usan exactamente la misma corrección.

La selección se adapta también en `InteractionDetector.Detect`: el volumen vacío
de una caja funcional grande ya no selecciona el Bun cuando se apunta a Cheese o
Patty visibles en la misma columna conectada. Lee bounds de presentación y conserva
rango, obstáculos y exclusión del cuerpo sostenido. `TryDetectHit` continúa intacto
para placement. La regresión se reprodujo en un test: Cheese visible seleccionaba
Bun; ahora el clic recoge la unidad original apuntada y un sólido bloquea el ray.

No se modifica `AssemblyPlacement`, `PhysicalDishAssembly`, `IngredientPurchaseStation`,
Pickup/Carry, FoodState/DishState, Cooking, PASS/Delivery, M14–M18, Dirt visuals,
recetas/precios, iluminación, materiales o layout. El proxy del Dish sigue siendo
alto/conservador; no se reducen colliders para acomodar arte.

`FoodVisualPlacementBuilder.Install` adapta componentes existentes de forma
incremental: tres campos de presentación por cada uno de los 18 visuales de escena
y de los tres prefabs Food; un componente de metadata en el apoyo actual. No
añade GameObjects, colliders ni transforms. Conserva también posteriores ajustes.
`VisualPolishBuilder` configura esos datos si se usa para un shell nuevo; no se
ejecutó su instalador ni se tocaron las manchas.

Auditoría `python Tools/audit_food_visual_placement.py`: **652 transforms y 117
componentes físicos originales íntegros**, referencias locales resueltas, GUID
originales estables y **1058 archivos protegidos idénticos**, incluidos todo el
arte/Dirt, Domain, el resto de Runtime, Packages y ProjectSettings. Resultado en
`Build/FoodVisualPlacement/Audit.json`.

## Validación

Suites completas ejecutadas en el proyecto real, Unity 6000.5.3f1:

- **EditMode: 391/391**, cero fallos/omitidos.
- **PlayMode: 345/345**, cero fallos/omitidos.
- Regresiones dirigidas: **14/14** PlayMode; incluidas también en la suite completa.
- Importación/compilación sin errores C#, GUID inválidos o excepciones inesperadas
  en los logs finales. Cada proceso termina con exit code 0.
- Repetición de `FoodVisualPlacementBuilder.Install`: escena idéntica, SHA256
  `A5AFED515A63B0CA4967C783AC306B99B1BFA2FA4063B5101C1E2334F550EF40`.

XML: `TestResults/FoodVisualPlacementEditMode.xml`,
`FoodVisualPlacementPlayMode.xml`, `FoodVisualPlacementFocused.xml`.
Logs homónimos en `Logs/`; idempotencia en
`Logs/FoodVisualPlacementIdempotence.log` y
`Build/FoodVisualPlacement/Idempotence.txt`.
Los logs incluyen mensajes de handshake del cliente de licencias al arrancar,
sin impedir las ejecuciones. El error de configuración Food provocado por el
test de rollback M8 es esperado mediante `LogAssert.Expect`.

Se añaden 14 casos PlayMode: montaje asistido paso a paso sin F en Plate/Prep/Grill,
Hamburger/Cheeseburger, Bun + Patty, Patty sobre Plate; gaps visuales ≤ 2 mm y
continuidad de centro/tamaño al confirmar, rotación del Dish, originales/IDs,
compras consecutivas de los tres productos y recogida inmediata/reposada, ajuste
de grosor solo visual, selección por visual/clic real sin atravesar obstáculos,
calor/contaminación M18 y pago una vez por PASS. Un caso
EditMode comprueba preservación de transforms, reglas y tuning manual durante merge.

Las comprobaciones usan compras, Pickup/PhysicalCarry, TryPlaceHeld, física,
FoodSimulation/Grill, contaminación y Delivery reales. Se pausa el input/avance
automático para staging controlado; no son sesiones de usuario con ratón.

## Capturas antes/después

Galería **`Build/FoodVisualPlacement/Captures/index.html`** con 15 pares y 18 vistas
adicionales (48 PNG),
1600 × 900, PlayerCamera en Play, mismo ojo a 1,65 m, FOV/poses y luz original.
`Before/` usa b023fe8; `After/` usa la corrección. Cada sesión conserva los originales
desde compra hasta confirmación; son sesiones distintas y sus IDs son distintos.
No se guardaron modificaciones de staging en la escena/assets. Se usa presupuesto
temporal solo para completar todas las compras de capturas; precios no cambian.

| Recorrido | PNG en Before/ y After/ |
| --- | --- |
| Hamburger, cada paso antes de F | Hamburger-Step1.png, -Step2.png, -Step3.png |
| Hamburger después de F | Hamburger-Final.png |
| Cheeseburger, cada paso antes de F | Cheeseburger-Step1.png, -Step2.png, -Step3.png, -Step4.png |
| Cheeseburger después de F | Cheeseburger-Final.png |
| Cheese comprado, inmediato/reposado | Collect-Cheese-Immediate.png, Collect-Cheese-Settled.png |
| Bun comprado, inmediato/reposado | Collect-Bun-Immediate.png, Collect-Bun-Settled.png |
| Patty comprado, inmediato/reposado | Collect-Patty-Immediate.png, Collect-Patty-Settled.png |

En ambas recetas el salto al confirmar queda en **0,000238 mm**, precisión flotante,
frente a 272–333 mm anteriores. Todas las capas contactan ya antes de F. Capturas
revisadas visualmente: Cheese sobre COLLECT HERE y montajes sin la torre flotante.

Las 18 vistas adicionales bajo After/ cubren cada paso y F de `Partial-Prep`,
`Patty-Plate`, `Cheeseburger-Prep`, `Patty-Bun-Grill` y `Cheeseburger-Grill`.
El recorrido de Patty en Grill usa el único FoodSimulation: tras 30 s llega a
156,18 °C y dosis 24,46 s equivalentes, sin avance desde presentación. El salto
al confirmar esos recorridos también permanece ≤ 0,000238 mm.

## Comprobación humana y archivos

Abrir M15 y PrototypeRestaurant sin Rebuild. Comprar y recoger Bun/Patty/Cheese,
retirando cada unidad antes de la siguiente. Montar con release asistido sobre
Plate, Prep y Grill; revisar cada paso antes de F, luego confirmar y transportar.
Probar también Bun + Patty y Patty directamente en Plate; conservar la cocción
según el contacto original y servir por PASS. La sesión manual con ratón y la
aceptación final humana quedan pendientes; las capturas y pruebas no se presentan
como esa comprobación. No se ha medido FPS ni generado build standalone.

Inventario exacto en [FOOD-VISUAL-PLACEMENT-FILES.txt](FOOD-VISUAL-PLACEMENT-FILES.txt).
Ver [ADR 0028](Decisions/0028-visual-support-independent-of-functional-proxies.md).
