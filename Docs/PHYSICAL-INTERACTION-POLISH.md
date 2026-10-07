# Polish de interacción física antes de M12

La reposición descrita en esta entrega histórica queda sustituida por
[Plate comprable y montaje libre](PLATES-FREE-ASSEMBLY.md). Los tres apoyos siguen
en sus posiciones, sin AssemblySurface ni identidad Dish; no se generan platos
gratis. Plate es opcional para montar, transportar y entregar.

Base `0c81923`, rama `fix/physical-interaction-polish`. M11 está validado;
este cambio no implementa M12, electricidad, inventario ni recetas nuevas.

## Manejo

- Mantener **clic izquierdo** sobre Pickup recoge y sostiene el objeto físico.
  Soltarlo libera el objeto, restaura gravedad/constraints/colisiones y conserva
  su velocidad física. Mover jugador/cámara permite lanzarlo sin impulso añadido.
- A petición del usuario, se conserva **colocación asistida al soltar**: con mano
  tranquila, mirar una bandeja, superficie horizontal o pila cercana y soltar
  clic coloca el mismo ingrediente encima, alineado con la pila. El HUD indica
  `Release LMB to place on surface/stack`. **F** continúa confirmando el Dish;
  no se finaliza automáticamente al completar una receta.
- Un gesto rápido mantiene la liberación con impulso. **G** siempre suelta libremente,
  incluso ante una pila. La ayuda no actúa con obstáculos, salida ocupada, pared,
  ingrediente ajeno/no registrado ni destino a más de 1 m del ingrediente sostenido.
  Límite configurable `Maximum Assist Distance`; mano tranquila hasta 1,5 m/s,
  configurable en `PhysicalCarry/Maximum Assisted Hand Speed`.
- Con la mano quieta, la velocidad de liberación queda limitada a 2 m/s para que
  la corrección inicial del agarre no se convierta en un lanzamiento. Si el
  destino de la mano se mueve, conserva hasta 6 m/s (límites de PhysicalCarry).
  El movimiento se mide en FixedUpdate y se suaviza; no modifica Time.timeScale.
- **E** sigue disponible para compras, interacción contextual y agarre alternativo;
  **G** libera el agarre alternativo. Un clic sobre un botón de compras no compra.
  **F** confirma la composición con manos libres. El clic de recapturar el cursor
  se consume; pérdida de control/disable libera el agarre de ratón.
- El botón derecho ya no dispara Throw desde el adaptador de interacción. La API
  histórica y su binding se conservan para herramientas/tests. PlaceIngredient
  y TryPlace se conservan por compatibilidad de desarrollo. El press izquierdo
  solo agarra; el release puede colocar con ayuda. Compras y confirmación mantienen
  sus acciones contextuales propias.

## Montaje y calor

PhysicalDishAssembly encuentra ingredientes activos, registrados, no sostenidos,
con física propia, conectados verticalmente por contacto (tolerancia 2,5 cm).
Comprueba apoyo sólido debajo de la base y normal vertical suficiente; vale una
encimera, mesa, bandeja o plancha sin añadirles AssemblySurface. Mirar cualquier
ingrediente de la pila muestra reconocimiento y permite **F**. Pila flotante,
pared o ingrediente sostenido no forman un montaje válido. Pilas separadas no se
mezclan. El reconocimiento conserva los perfiles Hamburger/Cheeseburger existentes;
las otras composiciones son Custom Dish.

El preview usa un DishState temporal que libera todas sus pertenencias al terminar.
Confirmar vuelve a validar y crea un root de transporte sin arte ni comida nueva,
con los FoodItem/FoodState originales. Reutiliza DishItem, proxy físico, aceptación,
evaluación, pago único y CustomerDishCarrier M6. Las estaciones anteriores siguen
funcionando y sus referencias/GUID se conservan.

Al finalizar, FoodItem conserva la geometría local de sus colliders retirados.
GrillHeatSource comprueba esa geometría por ingrediente contra su volumen térmico
orientado; mover/rotar el plato mueve la geometría. El proxy del plato no calienta
automáticamente todos sus componentes. Para primitivas BoxCollider el volumen es
la caja original; otros colliders usan bounds conservadores. ColdStorage ya
comprueba la posición de cada ingrediente y mantiene su comportamiento.
**FoodSimulation sigue siendo el único driver**: no se duplica edad, deterioro,
temperatura ni cocción; GameTime/RestaurantDay permanecen independientes.

## Reposición

La causa era la referencia fija de AssemblySurface al Dish que se finalizaba,
desparentaba y finalmente destruía al salir el cliente. Cada estación ahora tiene
DishTraySupply con un prefab **inactivo y vacío**, independiente del plato vendido.
Cuando sale el plato del espacio original, repone una sola bandeja y sustituye la
referencia del draft. Un objeto que bloquee el espacio aplaza la reposición.
No clona alimentos, no cambia FoodSimulation ni saldo y no reinicia el plato anterior.
También recupera una bandeja destruida; desactivar estación/supply detiene reposición.
Las bandejas vacías de las estaciones siguen siendo apoyos fijos del greybox.

La escena viene instalada. El menú **Zero Star Restaurant → Prototype → Install
Physical Interaction Polish** adapta una escena guardada y cerrada; Rebuild incluye
el polish. Repetir instalación no duplica componentes ni cambia GUID.

## Validación manual breve

1. Abrir/recargar PrototypeRestaurant desde Project, iniciar Play y enfocar Game.
   Comprar ingredientes con **E**;
   retirarlos de OUTPUT manteniendo clic izquierdo. Soltar quieto debe dejar caer;
   desplazar cámara y soltar durante el movimiento debe conservar impulso.
   Escape libera control y objeto; el clic de recaptura no debe recoger ni colocar.
2. Cocinar la carne sobre Grill. Sostener Bun, apuntar a Prep/bandeja y soltar;
   para Patty y el Bun superior apuntar a la pila y soltar con la mano tranquila:
   se centran automáticamente encima. Comprobar Hamburger y confirmar **F**. Repetir en
   otra encimera/bandeja, con Cheese para Cheeseburger y una composición Custom.
3. Apilar y confirmar sobre Grill. El ingrediente dentro de la zona roja sigue
   calentándose/cocinándose, el que queda por encima no recibe calor nuevo por
   formar parte del mismo plato. Retirar el plato detiene el aporte de Grill.
4. Entregar el pedido correcto en Delivery manteniendo clic y soltándolo sobre
   el pad verde. Comprobar cobro único y salida del mismo Dish. Usar una estación
   para **más de tres pedidos**: al retirar cada plato debe aparecer una bandeja
   vacía. Bloquear el hueco, retirar bloqueo y comprobar una sola reposición.
5. Cerrar jornada y empezar Next Day; comprobar cola, saldo y alimentos conservados.
   Revisar Console. La aceptación manual de Game queda pendiente del usuario;
   la validación automatizada no sustituye la valoración del tacto del ratón.

## Validación ejecutada

Unity **6000.5.3f1**, proyecto aislado `Temp/M9Validation` para conservar los
Editores del usuario. Compilación/instalación completadas; **194 EditMode y
152 PlayMode correctos**, incluyendo regresiones M1–M11. Tras la protección final
del adaptador desactivado, se repitieron las **56 pruebas** de interacción,
montaje y servicio afectadas, todas correctas. Los XML quedan en
`TestResults/PhysicalPolishEditMode.xml`, `PhysicalPolishPlayModeFinal.xml` y
`PhysicalPolishGuarded.xml`; logs correspondientes en `Logs/` (ignorados por Git).
La ampliación de colocación asistida pasa **194 EditMode y 160 PlayMode** en
`AssistedAssemblyEditModeFinal.xml` y `AssistedAssemblyPlayMode.xml`; incluye
centrado de Hamburger/Cheeseburger, bandeja real, rechazo por bloqueo/distancia/
pertenencia, calor sobre Grill, gesto rápido y G simultáneo con mouse release.
Sin errores de compilación/importación ni excepciones inesperadas en esas pasadas;
el licenciamiento registra el diagnóstico de handshake de la máquina y continúa.

Se verifican seis reposiciones consecutivas, salida bloqueada y bandeja destruida;
Hamburger/Cheeseburger en cuatro apoyos, Custom, pertenencia exclusiva e identidad;
calor por ingrediente antes/después de confirmar y al retirar; input virtual,
recaptura, hold/release y velocidad por movimiento de cámara. Un Dish montado en
encimera completa entrega, pago único, transporte y limpieza M6. La escena real
también comprueba reposición después de una venta con transporte físico nativo.
Se conservan **636 registros originales de escena**, añadiendo cuatro componentes;
referencias locales/GUID nuevos resuelven y las metas anteriores no cambian.
No se ha hecho build ni aceptación manual de la ventana Game.

## Inventario de archivos

Creados (assets con `.meta`):

- `Assets/_Project/Scripts/Runtime/Dishes/DishTraySupply.cs`, `PhysicalDishAssembly.cs`.
- `Assets/_Project/Prefabs/EmptyDishTray.prefab`.
- `Assets/_Project/Editor/PhysicalInteractionPolishBuilder.cs`.
- `Assets/_Project/Tests/PlayMode/PhysicalInteractionPolishTests.cs`.
- `Assets/_Project/Tests/EditMode/PhysicalInteractionPolishSceneTests.cs`.
- Este documento y `Docs/Decisions/0014-physical-interaction-and-surface-assembly.md`.

Modificados:

- `Assets/_Project/Scripts/Runtime/Interaction/CarryPhysics.cs`, `PhysicalCarry.cs`,
  `InteractionInput.cs`, `PlayerInteraction.cs`, `InteractionFeedback.cs`, `InteractionDetector.cs`.
- `Assets/_Project/Scripts/Runtime/Dishes/AssemblySurface.cs`, `DishItem.cs`,
  `DishAssemblyInteraction.cs`, `DishInspectionFeedback.cs`, `AssemblyPlacement.cs`.
- `Assets/_Project/Scripts/Runtime/Food/FoodItem.cs`,
  `Assets/_Project/Scripts/Runtime/Cooking/GrillHeatSource.cs`.
- `Assets/_Project/Editor/M1GreyboxBuilder.cs`, `Assets/_Project/Scenes/PrototypeRestaurant.unity`.
- `Assets/_Project/Tests/PlayMode/M5AssemblyFocusTests.cs`, `M6PrototypeTests.cs`, `M6ServiceTests.cs`.
- `README.md`, `AGENTS.md`, `Assets/_Project/README.md`, `Docs/ARCHITECTURE.md`,
  `Docs/ROADMAP.md`, `Docs/M11.md`, `Docs/VERTICAL-SLICE-POLISH.md`.

Sin cambios de paquetes, input asset, settings ni metas anteriores. `_Recovery`
permanece fuera de commits. Sin merge a main ni publicación.
