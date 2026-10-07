# Plate opcional, montaje libre y entrega por Dish

Rama `fix/purchasable-plates-free-assembly`, desde `5fe1742`, conservando la escena
y materiales locales del usuario. Sin cambios de distribución, controles,
dependencias, electricidad ni M12. Ver [ADR 0016](Decisions/0016-purchasable-optional-plates.md).
Implementación y tests en `76b8cd5`.

## Comportamiento

Procurement añade **Plate**, precio inicial **0,50 €** configurable en
`ScriptableObjects/Economy/Plate.asset`, campo Price Cents. Reutiliza **E**, saldo y
OUTPUT. Cada compra crea un utensilio físico diferente con ID único; se recoge
con clic mantenido o E. OUTPUT ocupado y saldo insuficiente no crean ni cobran.
No es FoodItem ni DishItem y no añade otra simulación o balance.

No quedan estaciones activas AssemblySurface ni reposición gratuita. Los antiguos
soportes de Assembly se mantienen inmóviles como PrepSupport1/2/3. Se puede apilar
y confirmar **F** sobre Plate, mesa, encimera, suelo y Grill. Preview y confirmación
usan contacto y soporte físico, conservando alimentos y estados originales.
Unidades cercanas de una encimera no se mezclan. Sobre Plate se incorporan las
unidades que descansan realmente en él y sus pilas, incluso como Custom Dish.
Las caras reales de colliders, además de bounds, evitan contactos falsos por giro.

Dish con Plate conserva ese mismo utensilio dentro del agregado; Dish sin Plate
funciona igualmente. El Plate no es identidad, proxy ni condición de pedido.
Un Plate vacío en Delivery no se entrega. Al aceptar, el mismo Dish y sus hijos
quedan bloqueados, fijados al cliente y se limpian en Exit. Posición/mirada del
jugador no intervienen después de soltar.

Grill conserva calentamiento por geometría original de cada ingrediente antes y
después de F; no se usa el proxy para calentar todos los componentes. Los que no
tocan el volumen térmico no reciben calor directo. FoodSimulation sigue siendo
el único reloj, sin restaurar cocción, freshness, contamination ni IDs al montar.

El menú **Zero Star Restaurant → Prototype → Install Purchasable Plates and Free
Assembly** adapta una escena guardada y cerrada; la escena entregada ya viene
adaptada. Repetirlo es idempotente y no mueve el local. Rebuild incorpora la misma
adaptación, aunque Rebuild conserva su comportamiento histórico de reconstruir.
El prefab EmptyDishTray antiguo conserva su GUID como asset histórico sin suministro.

## Validación

Unity 6000.5.3f1, URP 17.5.0, Input System 1.19.0 y Test Framework 1.7.0 comprobados.
Se usa la copia aislada `Build/HandoffValidation` porque el usuario tiene abierto
el Editor original. Validación del 2026-10-07:

- Escena actual, con distribución local conservada: **198/198 EditMode** y
  **188/188 PlayMode**. Resultados `TestResults/PlateEditModeFinal.xml` y
  `TestResults/PlatePlayModeFinal.xml`; logs `Logs/PlateEditModeFinal.log` y
  `Logs/PlatePlayModeFinal.log`.
- Escena exacta del commit, excluyendo las ediciones locales previas: **198/198
  EditMode** y **38/38 PlayMode** de integración física, compras, montaje,
  entrega, Storage, cola y jornada. Resultados `TestResults/PlateCommitEditMode.xml`
  y `TestResults/PlateCommitPlayMode.xml`; logs con los mismos nombres bajo `Logs`.

Las cuatro pasadas terminan con código 0. Sin errores C# ni excepciones inesperadas;
el rollback M8 emite su error de configuración previsto por el test. El diagnóstico
inicial de handshake de licencia se resuelve antes de ejecutar. Las pasadas previas
de ajuste no se cuentan como correctas. Resultados, logs y copia están ignorados.

La adaptación nativa se audita por fileID/documento: todos los transforms
preexistentes conservan posición/rotación/escala. Retira nueve scripts de montaje
de estación y añade diez documentos para botón/etiqueta. Mantiene referencias de
Delivery, servicio, alimentos, productos anteriores y geometría restante. La
escena para el índice aplica solo la adaptación sobre HEAD; las ediciones locales
previas permanecen en el working tree y fuera de los commits.
Los 674 documentos finales resuelven todas las referencias locales. Los 33 archivos
de implementación, assets, metas y tests coinciden byte a byte con la copia validada.
Los dos metadatos nuevos de prefab/producto solo se normalizan para retirar espacios
finales, conservando sus GUID. Diff del índice y delta frente a la escena local
previa comprobados sin errores de whitespace; el texto preexistente del usuario
permanece fuera del commit. No se ejecuta una build.

Tests: compras únicas, precio configurable, ocupación/fondos, ausencia de respawn,
E/pickup, soporte físico/rotado, recetas/Custom fuera de estación, Grill tras F,
entrega con/sin Plate desde frente/izquierda/derecha/sin mirar, mismo Dish y Plate
hasta Exit, bloqueo de interacción/reventa, pago único y regresiones M1–M11.

## Prueba manual breve

1. Con Play detenido, recargar PrototypeRestaurant desde disco conservando
   ediciones propias. Comprobar Console. No ejecutar Rebuild para instalar.
2. En Procurement, comprar Plate con **E** y retirarlo de OUTPUT; repetir compra
   ocupada y comprobar cero cargo. Dejarlo sobre Prep. Retirarlo varias veces:
   no aparecen platos gratis en Assembly ni tras vender.
3. Apilar Bun → Patty cocinada → Bun sobre Plate soltando clic tranquilo mirando
   la pila. Mirar ingrediente y **F**: Hamburger; recoger y soltar sobre Delivery.
   Mirar hacia otro lado/alejarse: cliente lleva el mismo Dish y Plate hasta Exit.
4. Repetir sin Plate sobre encimera/suelo; añadir Cheese para Cheeseburger y una
   composición distinta para Custom. El pedido correcto debe aceptarse sin Plate;
   el incorrecto conserva su rechazo histórico y deja el Dish disponible.
5. Sobre Grill, finalizar comida en contacto y comprobar que sigue aumentando
   temperatura/cooking; retirarla pausa dosis y permite enfriamiento. Un Plate
   vacío sobre Delivery no produce venta. Comprobar las rutas/poses propias.

Aceptación visual con teclado/ratón y build pendientes; las pruebas automatizadas
no sustituyen esa comprobación manual.

## Archivos

Modificados:

- `Assets/_Project/Editor/PhysicalInteractionPolishBuilder.cs`.
- `Assets/_Project/Scenes/PrototypeRestaurant.unity` (solo adaptación en el commit).
- `Assets/_Project/Scripts/Runtime/Dishes/DishAssemblyInteraction.cs`.
- `Assets/_Project/Scripts/Runtime/Dishes/DishItem.cs`.
- `Assets/_Project/Scripts/Runtime/Dishes/DishTraySupply.cs` (componente retirado/inactivo).
- `Assets/_Project/Scripts/Runtime/Dishes/PhysicalDishAssembly.cs`.
- `Assets/_Project/Scripts/Runtime/Economy/IngredientPurchaseButton.cs`.
- `Assets/_Project/Scripts/Runtime/Economy/IngredientPurchaseStation.cs`.
- `Assets/_Project/Tests/EditMode/M5DishSceneTests.cs`.
- `Assets/_Project/Tests/EditMode/M8ProcurementSceneTests.cs`.
- `Assets/_Project/Tests/EditMode/M9RestaurantLayoutTests.cs`.
- `Assets/_Project/Tests/EditMode/PhysicalInteractionPolishSceneTests.cs`.
- `Assets/_Project/Tests/EditMode/VerticalSlicePolishSceneTests.cs`.
- `Assets/_Project/Tests/PlayMode/M10CustomerQueueTests.cs`.
- `Assets/_Project/Tests/PlayMode/M11RestaurantDayTests.cs`.
- `Assets/_Project/Tests/PlayMode/M5AssemblyFocusTests.cs`.
- `Assets/_Project/Tests/PlayMode/M6PrototypeTests.cs`.
- `Assets/_Project/Tests/PlayMode/M6ServiceTests.cs`.
- `Assets/_Project/Tests/PlayMode/M7StoragePrototypeTests.cs`.
- `Assets/_Project/Tests/PlayMode/M8ProcurementTests.cs`.
- `Assets/_Project/Tests/PlayMode/PhysicalInteractionPolishTests.cs`.
- `README.md`, `Docs/M8.md`, `Docs/ROADMAP.md`, `Docs/PHYSICAL-INTERACTION-POLISH.md`.

Creados (cada archivo bajo Assets con su `.meta` contigua):

- `Assets/_Project/Editor/PlateProcurementBuilder.cs`.
- `Assets/_Project/Prefabs/Plate.prefab`.
- `Assets/_Project/ScriptableObjects/Economy/Plate.asset`.
- `Assets/_Project/Scripts/Runtime/Dishes/PlateItem.cs`.
- `Assets/_Project/Scripts/Runtime/Economy/PlateProduct.cs`.
- `Assets/_Project/Tests/PlayMode/PurchasablePlateTests.cs`.
- `Docs/PLATES-FREE-ASSEMBLY.md`.
- `Docs/Decisions/0016-purchasable-optional-plates.md`.

Los tres materiales, `_Recovery`, SceneTemplateSettings y cambios de distribución
preexistentes quedan fuera de commits. No se modifican Domain, CustomerDishCarrier,
DeliveryZone, OrderEvaluation, configuración de paquetes/settings, input ni sus GUID.
Sin merge a main ni publicación.
