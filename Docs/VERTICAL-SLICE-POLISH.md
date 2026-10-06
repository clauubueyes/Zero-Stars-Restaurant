# Polish del vertical slice M1–M6

Guía histórica de M6. El [polish físico desde M11](PHYSICAL-INTERACTION-POLISH.md)
sustituye el clic de snap/Throw por hold/release y permite montaje fuera de
AssemblySurface; también repone bandejas. Para controles actuales usar esa guía.

Fecha: 2026-10-06. Rama **`fix/vertical-slice-polish`**, desde M6 aprobado
**`9335899`**, con árbol limpio antes de editar. Sin merge/push; main conserva
`e552029`. M6 fue validado manualmente por el usuario; esta corrección requiere
su propia comprobación manual. **M7 no implementado**.

La [corrección de entrega posterior](DELIVERY-TOLERANCE-FIX.md) amplía el pad y
admite solapamiento parcial; las instrucciones de entrega de esta guía lo reflejan.

Se inspeccionaron AGENTS, documentación, historial, referencias, input, generador
y Runtime M1–M6. Comprobados Unity **6000.5.3f1**, URP **17.5.0**, Input System
**1.19.0**, Test Framework **1.7.0**, URP PC/Linear. No cambia Domain, paquetes,
ProjectSettings, definiciones, materiales ni GUID existentes.

## Colocación e interacción

**E** recoge; **G** suelta; **botón derecho** lanza. Sosteniendo un FoodItem,
mirar una bandeja o ingrediente perteneciente a su pila, a menos de **3 m**, muestra
**[Left Button] Place Bun on stack** (nombre/binding real). **Clic izquierdo**
PlaceIngredient libera correctamente el agarre M2 y coloca esa misma unidad.
F confirma con manos libres; E sobre ingredientes sin confirmar permite retirarlos.
El clic que recupera cursor después de Escape no coloca. E/G/Throw ganan ante
solicitudes simultáneas. No se habilitan Move/Look en la copia de input de montaje.

`DishAssemblyInteraction` adapta intención y revalida el primer sólido del raycast:
no salta paredes, cajas ajenas ni alcance. `AssemblySurface` valida Food activo,
registrado, sensor disponible, hueco libre y pertenencia exclusiva. El helper local
`AssemblyPlacement` calcula bounds por colliders a la orientación de la bandeja;
centra XZ y coloca sobre el máximo Y de bandeja/pila con **4 mm** de separación.
Solo después del preflight/claim se llama PhysicalCarry.Drop, se coloca, anulan
velocidades y duerme el Rigidbody. Sigue dinámico y se puede reorganizar con E/G.
No genera comida, no copia estado y no modifica identidad, frescura, edad,
temperatura, contaminación o dosis. FoodSimulation continúa siendo el único reloj.

Después de finalizar, el proxy BoxCollider existente abarca todo el agregado;
los Pickup/colliders individuales quedan retirados. Detector resuelve el ancestro
interactuable activo del primer hit. Top bun, patty, cheese y bottom bun muestran
**[E] Pick up Hamburger/Cheeseburger/Custom Dish** según estado real. El problema
de agarre dependía además del destino bajo al mirar distintos ingredientes:
Pickup configura elevación mínima **5°** para platos, default **-90°** mantiene
cajas/comida M2. PhysicalCarry sigue comprobando obstáculos/cápsula y conserva
fuerza/velocidad; permite aproximación inicial desde el suelo con tolerancia de
distancia decreciente hasta el tether normal de **1,5 m**. Sin teleporte de transporte.

## Venta visible hasta salida

La transacción Domain conserva sus reglas y evidencia inmutable. Tras aceptación
y pago, `CustomerDishCarrier` vincula **el mismo Dish** a
`CustomerPlaceholder/DishCarryAnchor`, local `(0, 1.2, 0.65)`, con base a altura
del anchor y rotación local identidad. Retira Pickup/colisiones, queda cinemático
y visible durante Pay y Leave. No hay manos, animación ni una hamburguesa nueva.

Los FoodState originales siguen vivos, registrados y envejeciendo en el único
FoodSimulation hasta llegar al punto de salida. Su deterioro posterior no altera
el recibo de entrega. IsSold, el pedido terminado y el ledger evitan reventa/doble
pago; Pickup desactivado evita recogida. Al alcanzar salida, el coordinador
desregistra unidades (y caché de avance), desactiva/destruye el Dish y oculta el
cliente reutilizable. El siguiente ciclo conserva saldo, usa nueva visita/pedido
y el delay existente. Rechazo conserva plato, Pickup y reloj; no usa el anchor.
Cancelar/desactivar servicio también limpia la venta y oculta el cliente.

## Greybox vigente

Mismos 18 alimentos, 3 bandejas, 4 cajas, materiales y nombres. Se conservan los
**406 fileIDs originales**; se añaden **3 documentos** para componente/anchor.
La escena entregada tiene **409 documentos** y equivale al generador nativo.
Las posiciones de instrucciones anteriores M4–M6 se conservan como historial;
este es el recorrido actual:

| Estación | Centro X/Z, metros | Uso |
| --- | --- | --- |
| CookingPrepBench | -5.2 / 2 | Cuatro carnes de pruebas, usar Fresh para primera venta. |
| AssemblySupplyBench | -5.2 / 3.7 | Pan y queso, junto a preparación. |
| GrillStation | -2.5 / 3.7 | Plancha rojiza, misma configuración térmica. |
| AssemblyStation1/2/3 | -0.4, 0.7, 1.8 / 3.7 | Bandejas en banco compacto. |
| DeliveryPad | 0 / 0.5 | Pad verde de 1.8 × 1.0 m sobre mostrador, top 1.13 m. |

Pasillo de trabajo alrededor de **Z 2.0–2.25** entre montaje y mostrador. Divider
se mueve a X 4.5; mesa, escalón y bloque alto al lateral derecho. Queda espacio
para cajas/física, banco de inspección M3 y ruta de cliente existentes.
El generador conserva menú/configuración/materiales; no se ejecuta al importar.
Para reconstruir: detener Play, guardar trabajo, cerrar PrototypeRestaurant,
abrir SampleScene y ejecutar **Zero Star Restaurant > Prototype > Rebuild Greybox
Scene**; confirmar su sustitución solo si se quiere perder ajustes manuales de
esa escena. No hace falta reconstruir para probar la escena entregada.

## Validación ejecutada

El proyecto original estaba abierto en Unity: no se cerró ni se lanzó otro Editor
sobre esa carpeta. Se copiaron Assets/Packages/ProjectSettings a
`Build/PolishValidation`, con Library nueva, y se ejecutó Unity exacto en batch
oculto, sin gráficos. Resultados/logs locales ignorados bajo Build.

- **EditMode: 108/108**, 0 fallos, 0 omitidos, todas las reglas Domain y escenas.
- **PlayMode: 76/76**, 0 fallos, 0 omitidos, regresiones M1–M6 y físicas nativas.
  Nuevos casos: snap conserva estado/ID y no duplica; pila Cheeseburger estable;
  retirar/recolocar/finalizar; oclusión/alcance/sensor/cajas/tamaño/hueco/ownership;
  rayos a todos los ingredientes, Custom de 3 y 6 unidades; ciclo de input;
  venta con mismo agregado, transporte visible, doble pago, recibo inmutable,
  retirada al salir, rechazo, siguiente cliente y cancelación de servicio.
- Integración en **PrototypeRestaurant real**: snap de tres unidades originales,
  confirmación, transporte físico, G sobre pad, pago, venta visible y siguiente
  cliente. La cocción determinista se prepara en el test; M4 conserva sus pruebas
  de calor nativo. Sigue habiendo test de montaje manual sin snap.
- Se detectó y corrigió regresión al levantar el agregado desde el suelo: el
  destino elevado superaba el tether normal antes de aproximarse. La tolerancia
  inicial decreciente resuelve ese caso; el test original vuelve a pasar.
- Dos rebuilds nativos del layout final comparados por jerarquía, componentes y
  referencias, ignorando fileIDs generados: equivalencia de 409 documentos.
- Auditoría de GUID/referencias/metas, input previo preservado, diff y exclusiones.
  Domain, Packages, ProjectSettings, materiales y definiciones permanecen iguales.
- **Build Windows x64 Development: Succeeded**, **0 errores, 0 advertencias**,
  171456330 bytes. Incluye PrototypeRestaurant explícitamente, backend/configuración
  existentes. Artefacto local ignorado:
  `Build/PolishValidation/Build/PolishSmoke/ZeroStarRestaurant.exe`.

Resultados: `Build/PolishEditFinalResults.xml`, `Build/PolishPlayFinalResults.xml`;
logs `Build/PolishEditFinal.log`, `Build/PolishPlayFinal.log` y
`Build/PolishDeterminism.log`. Build: `Build/PolishBuild.log` y
`Build/PolishValidation/Build/PolishBuildResult.txt`, sin cambiar Build Settings
del proyecto original. No se ha ejecutado el jugador compilado. Batch sin gráficos
no certifica presentación ni comodidad
con teclado/ratón; esas comprobaciones quedan para el usuario.

## Prueba manual exacta

1. Detener Play y guardar cambios propios aparte. Esperar compilación, **Assets >
   Refresh**, reabrir desde disco `Assets/_Project/Scenes/PrototypeRestaurant.unity`.
   No guardar encima una versión antigua abierta. Console sin errores propios.
2. Seleccionar CustomerServiceZone, **Force Next Offer Index = 0**; Play, enfocar
   Game. WASD/ratón; saldo €0.00 y pedido Hamburger cuando llegue el cliente.
   Rodear el extremo izquierdo del mostrador para acceder a la cocina del fondo.
3. E sobre **Raw Beef Patty - Fresh cooking fixture**, en mesa de preparación.
   G sobre plancha rojiza; esperar Cooked, E retirar. Para acelerar solo comida,
   Inspector FoodTestZone/FoodSimulation → **Advance food by 10 simulated seconds**;
   pasos pequeños cerca de Cooked y retirar antes de quemar. El clic de snap solo
   funciona en montaje, nunca coloca sobre plancha. Recordar el ID de esa carne.
4. Dejar la carne cocinada temporalmente con G en banco libre. E sobre **Bun -
   Assembly supply**, caminar por delante de la fila hacia una bandeja vacía.
   Mirar bandeja desde ~1.5–2 m: aparece **[Left Button] Place Bun on stack**.
   Clic izquierdo: pan centrado, manos libres. E retirar/recolocar para verificar.
5. E recuperar la carne cocinada; mirar pan/bandeja y clic izquierdo. Repetir con
   otro Bun. Tras el tercer clic ver **Recognized: Hamburger** y **[F] Finalize
   Hamburger**. Sin luchar con alineación; si se interpone caja/pared o no cabe,
   el snap se rechaza y se conserva lo sostenido.
6. F con manos libres. Mirar top bun, patty, bottom bun y caras laterales: siempre
   **[E] Pick up Hamburger**, nunca Bun. E recoge una sola unidad; se eleva incluso
   mirando hacia abajo. G y E permiten soltar y volver a recoger el agregado.
7. Caminar por el pasillo a detrás del mostrador, aproximadamente **(0, 0, 2.25)**,
   mirando al pad verde `(0, 1.8, 0.5)`. El plato se mantiene elevado sin tener que
   buscar un ángulo preciso. Colocar parte razonable sobre el verde, incluso en
   bordes, esperar frenar y **G**. Sostenido
   no se vende; al reposar se procesa automáticamente.
8. Ver **Correct order: YES**, **Payment: €5.00**, **Balance: €5.00** y carne/ID
   originales en recibo. El plato se mueve delante del cliente, permanece visible
   durante Pay (~4 s) y luego viaja con él al salir. Apuntar/E no permite recogerlo;
   repetir input no incrementa saldo. Solo al llegar a salida desaparecen ambos.
   Tres segundos después aparece otra visita; el recibo anterior permanece.
9. Sesión nueva, Force Next Offer Index **1**: Bun → Patty → Cheese → Bun usando
   clic; **Recognized: Cheeseburger**, F, todas las partes dan el mismo Pickup,
   entrega €6.50. Verificar también Custom con 3 carnes: nombre y unidad agregada
   correctos, rechazo ante pedido Hamburger/Cheeseburger, plato aún recogible.
10. Escape sosteniendo Food: clic recupera cursor sin colocar; siguiente clic sí.
    Probar G/manual sin snap, Throw derecho, retirar ingrediente con E, oclusión,
    alcance, cajas M2, salto/colisiones M1 y reiniciar Play: saldo cero/fixture nueva.
11. **Window > General > Test Runner**, Run All en EditMode y después PlayMode;
    ambos deben pasar. No ejecutarlos mientras se hacen pruebas manuales en Play.

## Límites

- Snap conservador por bounds, en bandejas horizontales y dentro de sensor; puede
  rechazar objetos/pilas grandes o geometría irregular aunque parezcan caber. Se
  mantiene física libre y puede alterarse una pila empujándola antes de finalizar.
- Proxy de plato y esfera de transporte conservadores; espacios estrechos o
  movimientos bruscos pueden liberar agarre. Al depositar, usar G y esperar apoyo.
- Plato del cliente sin colisiones, ruta cinemática sin evasión de objetos; no es
  navegación/animación definitiva. Si se cancela servicio se limpia antes de salida.
- Suministros limitados, sin reposición; reiniciar Play restaura fixture/saldo.
  Recibos históricos no se actualizan con deterioro posterior; reglas de calidad
  y pago M6 intactas. No se añade M7, inventario ni otros sistemas.

## Archivos de esta corrección

### Creados (8)

```text
Assets/_Project/Scripts/Runtime/Dishes/AssemblyPlacement.cs
Assets/_Project/Scripts/Runtime/Dishes/AssemblyPlacement.cs.meta
Assets/_Project/Scripts/Runtime/Customers/CustomerDishCarrier.cs
Assets/_Project/Scripts/Runtime/Customers/CustomerDishCarrier.cs.meta
Assets/_Project/Tests/EditMode/VerticalSlicePolishSceneTests.cs
Assets/_Project/Tests/EditMode/VerticalSlicePolishSceneTests.cs.meta
Docs/Decisions/0008-vertical-slice-polish.md
Docs/VERTICAL-SLICE-POLISH.md
```

### Modificados (26)

```text
AGENTS.md
README.md
Assets/InputSystem_Actions.inputactions
Assets/_Project/README.md
Assets/_Project/Editor/M1GreyboxBuilder.cs
Assets/_Project/Scenes/PrototypeRestaurant.unity
Assets/_Project/Scripts/Runtime/Dishes/AssemblySurface.cs
Assets/_Project/Scripts/Runtime/Dishes/DishAssemblyInteraction.cs
Assets/_Project/Scripts/Runtime/Dishes/DishItem.cs
Assets/_Project/Scripts/Runtime/Interaction/InteractionDetector.cs
Assets/_Project/Scripts/Runtime/Interaction/PhysicalCarry.cs
Assets/_Project/Scripts/Runtime/Interaction/Pickup.cs
Assets/_Project/Scripts/Runtime/Customers/CustomerServiceLoop.cs
Assets/_Project/Scripts/Runtime/Food/FoodSimulation.cs
Assets/_Project/Tests/EditMode/M4CookingSceneTests.cs
Assets/_Project/Tests/EditMode/M6ServiceSceneTests.cs
Assets/_Project/Tests/PlayMode/M5AssemblyTests.cs
Assets/_Project/Tests/PlayMode/M5AssemblyFocusTests.cs
Assets/_Project/Tests/PlayMode/M6ServiceTests.cs
Assets/_Project/Tests/PlayMode/M6PrototypeTests.cs
Docs/ARCHITECTURE.md
Docs/ROADMAP.md
Docs/WORKFLOW.md
Docs/M5.md
Docs/M6.md
Docs/Decisions/0007-customer-delivery-payment.md
```

Los archivos bajo Build son evidencias/herramientas locales ignoradas; no se
versionan. Se mantienen todos los metas preexistentes.

## Commits

Base aprobada: `9335899`. Commits separados por responsabilidad:

- `53044d1` — `feat: add assembly snap and consistent dish pickup`.
- `b7000ee` — `fix: keep sold dishes visible until customer exit`.
- `a56835d` — `refactor: compact prototype kitchen workflow`.
- La documentación/decisión y esta validación se cierran en
  `docs: record vertical slice polish and validation`.

Consultar hashes con `git log --oneline 9335899..HEAD` y cambios con
`git diff --name-status 9335899..HEAD`. No se integra en main.
