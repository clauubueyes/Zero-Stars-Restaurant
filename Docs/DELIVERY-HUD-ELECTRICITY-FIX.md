# Corrección de entrega, HUD y facturación eléctrica

Rama: `fix/delivery-hud-electricity-billing`, basada en M13 (`1ce2d02`).
No añade un milestone.

## Referencia visual conservada

La escena de referencia es la versión actual del proyecto del usuario en
`C:/Users/Usuario/Zero Stars Restaurant`, capturada antes de editar. Se conservan
sus materiales y todos sus objetos, transforms, componentes y referencias.
No se utiliza la distribución anterior de M13 para reemplazar esa escena.
El proyecto original abierto en Unity permanece intacto; la corrección se
desarrolla y valida en un worktree separado.

Se incorporan también las correcciones de `13b7c45` para almacenamiento y fixtures
de desarrollo. La instalación de los componentes eléctricos, contabilidad y HUD
será incremental sobre esta referencia; no se ejecuta Rebuild.

## Resultado y arquitectura

- DeliveryZone es la única puerta espacial. La API del servicio también exige el
  pad actual, apoyo tolerante y Pickup libre. La geometría del Dish usa la pose
  nativa del Rigidbody, evitando comprobar un transform visual interpolado atrasado.
  El trigger previo solo aporta altura/habilitación: mover el pad no deja atrás otra
  posición de entrega. No hay dependencia del jugador ni de Plate.
- El carrier recibe el mismo Dish, bloquea el agregado inmediatamente y conserva
  ingredientes/Plate hasta Exit. Mantiene escala y reafirma poses tras física en
  LateUpdate, sin avanzar clientes ni comida. Rechazo recuperable y pago único.
- DebugHudPresenter es el único OnGUI. Feedback produce texto y DebugHudLayout
  asigna zonas disjuntas; clipping y scroll contienen textos largos. Resumen y
  contexto comparten región de forma mutuamente excluyente. Mensajes durante seis
  segundos, separados de estado, saldo y consumo; no cambia controles FPS.
- ElectricitySupplyState acumula histórico, diario y período pendiente. Next Day
  no cobra electricidad. Closed cobra solo costes fijos; el resumen separa neto
  operativo, caja, devengo y facturas efectivamente pagadas.
- RestaurantOperatingCosts ofrece **Development: Settle Electricity Bill** en el
  menú contextual del Inspector, solo en Play. PaymentLedger registra un recibo
  inmutable por ID de período y permite saldo negativo. Tras éxito se resetea solo
  ese pendiente; sin consumo no cobra ni crea otro recibo. Pagar tras Closed
  actualiza el snapshot económico del día con evidencia real sin repetir alquiler.
- El instalador añade componentes/referencias faltantes sobre la escena actual,
  rechaza referencias conflictivas y es idempotente. GenerateScene/Rebuild se
  niega a reemplazar una escena existente, también en batch.

Valores configurables antes de Play: **30 céntimos/kWh**, **300 céntimos de
alquiler/día**, **1000 céntimos iniciales de desarrollo**. Se mantienen vatios M12
2000/150/200 para Grill/Fridge/Freezer. No se añade calendario mensual ni M14.
El período se redondea una vez: dos días de 2,35 kWh devengan 71 céntimos cada
uno como información diaria, pero una única factura de 4,70 kWh cobra **141**.
No se suma el redondeo diario ni se utiliza texto del HUD para calcular.

Ver [ADR 0019](Decisions/0019-pad-delivery-shared-hud-periodic-electricity.md).

## Tres pruebas manuales

1. **Entrega y alejamiento:** en el worktree Fixes, abrir PrototypeRestaurant y
   Play. Preparar el pedido activo, confirmar con F y dejarlo apoyado en el pad
   verde con hold/release. Alejarse inmediatamente y mirar al otro lado. Debe
   pagarse una vez y viajar el mismo agregado con el cliente hasta Exit. Repetir
   con Plate opcional; un Dish claramente fuera del pad debe permanecer disponible.
2. **HUD:** mirar Food y después Dish, activar electricidad y usar Procurement.
   Day/Time queda arriba; economía a la izquierda; electricidad debajo; servicio
   a la derecha; prompt/contexto cerca del crosshair; mensajes abajo. Cambiar
   resolución de Game y usar scroll en listas largas. Al cerrar, Summary ocupa
   la región central y sigue visible hasta Next Day, sin invadir otros bloques.
3. **Dos días y factura:** encender suministro y consumir al menos un minuto para
   observar céntimos. Cerrar, terminar los clientes y comprobar alquiler cobrado
   una vez, electricidad devengada y pendiente sin cobro. Iniciar Next Day: diario
   cero, pendiente/saldo/objetos conservados. Consumir otra vez. En CustomerServiceZone
   / RestaurantOperatingCosts, menú de tres puntos: **Development: Settle Electricity
   Bill**. Balance baja por la factura, pendiente cero y diario intacto; repetir
   sin nuevo consumo cobra cero. Volver a consumir crea otro pendiente. El consumo
   mientras se consulta Summary también pertenece al período; no hay noche simulada.

## Validación

Unity **6000.5.3f1**, URP 17.5.0, Input System 1.19.0, Test Framework 1.7.0.
Instalación incremental en batch: salida **0**. Suites completas finales:

| Suite | Pasadas | Fallos | Omitidas | Salida Unity |
| --- | --- | --- | --- | --- |
| EditMode | 256/256 | 0 | 0 | 0 |
| PlayMode | 237/237 | 0 | 0 | 0 |

XML locales: `TestResults/FixEditModeFinal.xml`, `TestResults/FixPlayModeFinal.xml`.
Logs: `Logs/FixInstall.log`, `Logs/FixEditModeFinal.log`, `Logs/FixPlayModeFinal.log`.
Las primeras pasadas detectaron supuestos antiguos de fixtures/layout, un precio
de test incorrecto y lectura visual atrasada del Rigidbody; se corrigieron antes
de estas pasadas completas. No se modificó la escena para hacer pasar esos tests.

Se cubren las seis condiciones del jugador, con/sin Plate, transporte físico
original hasta Exit, pago único, fuera del pad, sensor antiguo desalineado, rechazo
recuperable, estados de aparatos, cortes, diarios, período entre días, redondeo total,
factura antes/después de Closed, recibo único, nuevos consumos, saldo negativo y
continuidad de comida/objetos. Layout numérico comprobado en siete resoluciones,
único OnGUI y reinstalación idempotente con objetos/transforms/materiales/referencias
personalizados. Regresiones completas M1–M13 incluidas.

Auditoría: los 674 documentos originales de escena y sus transforms se conservan;
solo se añaden 23 documentos y los campos/componentes necesarios. Materiales actuales
idénticos al original, metas/GUID existentes intactos, GUIDs únicos y todas las
referencias externas/locales de escena resueltas. Regla permanente de AGENTS.md
idéntica a la añadida en el proyecto original. Este último conserva exactamente su
escena y cambios locales, sin ejecutar otro Editor sobre su carpeta.

La comprobación visual manual en Game sigue pendiente: batch/nographics no sustituye jugar el recorrido.
El bug manual descrito por el usuario no se da por reproducido por haber pasado tests;
se verifican explícitamente la puerta espacial, propiedad y poses del agregado.

Commits de implementación:

- `e3db244`: referencia actual del usuario y regla de preservación.
- `a099654`: devengo entre días y liquidación única del período eléctrico.
- `241ec5f`: entrega exclusiva por pad y poses del mismo agregado hasta Exit.
- `b96313e`: HUD común e instalación incremental con protección de escenas.

El commit de documentación final actualiza este informe, ADR 0019, M13, roadmap,
arquitectura, README y AGENTS.md. Sin integración en main ni publicación.

## Inventario de archivos

Raíz de las rutas de código siguientes: `Assets/_Project/`. Los `.meta` existentes
conservan sus GUID; cada archivo/carpeta nueva de Assets incorpora su `.meta`.

Nuevos:

- `Editor/DeliveryHudBillingBuilder.cs`.
- `Scripts/Domain/Economy/ElectricityBillReceipt.cs`.
- `Scripts/Runtime/Presentation/DebugHudLayout.cs` y `DebugHudPresenter.cs`.
- `Tests/EditMode/ElectricityBillingTests.cs` y `DebugHudTests.cs`.
- `Docs/DELIVERY-HUD-ELECTRICITY-FIX.md` y ADR `Docs/Decisions/0019-pad-delivery-shared-hud-periodic-electricity.md`.

Modificados:

- `Editor/M1GreyboxBuilder.cs` y `M9RestaurantLayoutBuilder.cs` (este último incorpora la corrección existente `13b7c45`).
- `Scenes/PrototypeRestaurant.unity`: snapshot actual del usuario; después solo 23
  documentos nuevos y adición de dependencias/componentes, sin modificar transforms
  existentes. 674 documentos originales conservados.
- `Materials/GreyboxFloor.mat`, `GreyboxVolume.mat`, `GreyboxWall.mat`: copias exactas
  de los materiales actuales del usuario, sin edición posterior.
- `Scripts/Domain/Economy/PaymentLedger.cs`, `DailySummary.cs` y
  `Scripts/Domain/Utilities/ElectricitySupplyState.cs`.
- `Scripts/Runtime/Customers/CustomerServiceLoop.cs`, `CustomerDishCarrier.cs`;
  `Scripts/Runtime/Orders/DeliveryZone.cs`, `OrderFeedback.cs`.
- `Scripts/Runtime/Economy/RestaurantOperatingCosts.cs`, `OperatingCostsFeedback.cs`,
  `IngredientPurchaseStation.cs`; `Scripts/Runtime/Utilities/ElectricityFeedback.cs`.
- `Scripts/Runtime/Time/RestaurantDayFeedback.cs`,
  `Scripts/Runtime/Interaction/InteractionFeedback.cs`,
  `Scripts/Runtime/Food/FoodInspectionFeedback.cs`, `DevelopmentIngredientSupply.cs`,
  `Scripts/Runtime/Dishes/DishInspectionFeedback.cs`.
- `Tests/EditMode/OperatingCostsTests.cs`, `M3FoodSceneTests.cs`, `M4CookingSceneTests.cs`,
  `M5DishSceneTests.cs`, `M9RestaurantLayoutTests.cs`.
- `Tests/PlayMode/M5AssemblyFocusTests.cs`, `M6ServiceTests.cs`, `M6PrototypeTests.cs`,
  `M8ProcurementTests.cs`, `M12ElectricityTests.cs`, `M13OperatingCostsTests.cs`.
- `AGENTS.md`, `README.md`, `Docs/ARCHITECTURE.md`, `Docs/ROADMAP.md`, `Docs/M12.md`,
  `Docs/M13.md` y `Docs/Decisions/0018-daily-bills-and-operating-costs.md`.

Se adaptan tests de fixtures para incluir unidades de desarrollo inactivas y tests
de transporte para usar el pasillo actual, sin modificar layout ni reglas de agarre.
Los archivos locales `_Recovery` y SceneTemplateSettings del proyecto original
permanecen intactos y no se incorporan a commits de gameplay. Sin cambios de input,
paquetes, pipeline, prefabs, recetas o configuración económica existente.

## Seguimiento de entrega sobre el pad (2026-10-07)

Primera investigación, anterior a la corrección entre clientes descrita abajo.

El usuario sigue observando una hamburguesa sin recoger sobre el verde y confirma
que pulsó F y había pedido. Falta conocer la condición bloqueante y distinguir
Order de Wait; la captura tampoco permite medir apoyo o velocidades. El caso
exacto todavía no está reproducido; las pruebas automáticas no lo dan por resuelto.

DeliveryZone expone `PlacementMessage` desde la misma detección y las mismas
condiciones que autorizan entregar. OrderFeedback lo muestra junto al pedido,
antes de la cola, mediante el único DebugHudPresenter. Explica si falta confirmar
la pila con F, soltar el plato, apoyarlo, introducirlo más en el pad, esperar a que
se asiente o esperar al cliente. No modifica geometría, umbrales, input, recetas,
pagos ni la necesidad de confirmar un Dish; no convierte alimentos automáticamente.

Se reproduce una pila reconocible sobre el pad sin confirmar: no paga, explica F
y, al finalizarla en el mismo sitio, el cliente recibe ese Dish y cobra una vez.
También se prueba que altura/velocidad bloqueantes se explican sin cobrar y que
recolocar correctamente permite la misma entrega. Dos pruebas nuevas recorren
con agarre y caída física ambos lados del pad en PrototypeRestaurant.

Validación nueva en `Zero Stars Restaurant Delivery`: **68/68 PlayMode** de
M6ServiceTests/M6PrototypeTests y **256/256 EditMode**, sin fallos ni omitidas.
XML: `TestResults/PadFeedback.xml` y `TestResults/PadEditMode.xml`.
La primera exploración pasó **7/7** pruebas físicas de PrototypeRestaurant.
No se repite aquí la suite PlayMode completa histórica de M1–M13.

Aplicado a la carpeta que el usuario abrió, `C:/Users/Usuario/Zero Stars Restaurant
Fixes`, rama `fix/delivery-pad-feedback`, commits `4e63e74` y `aead582`.
Los otros 439 archivos versionados de Assets se conservan byte a byte, incluidas
escenas, materiales y metas. El Editor abierto tiene cambios sin guardar; no se
sobrescribe su escena ni se inicia otro Editor en esa carpeta. La compilación del
Editor interactivo estaba pendiente al aplicar los scripts.

Archivos modificados: `Scripts/Runtime/Orders/DeliveryZone.cs`,
`Scripts/Runtime/Orders/OrderFeedback.cs`, `Tests/PlayMode/M6PrototypeTests.cs`,
`Tests/PlayMode/M6ServiceTests.cs` (bajo `Assets/_Project`) y este documento.

Comprobación pendiente: salir de Play, enfocar Unity y esperar compilación; usar
Assets / Refresh si hace falta. Volver a Play y repetir la colocación de la
captura. Leer la línea **Delivery:** junto al pedido. El usuario ya confirmó F;
ese mensaje permite distinguir geometría, apoyo y velocidad de disponibilidad
del cliente o una colocación previamente rechazada.

## Corrección del bloqueo entre clientes (2026-10-07)

Se reproduce un fallo concreto de entrega tras un rechazo: el cliente anterior
sale, la cola libera su reserva y el siguiente llega a atención con un pedido
que coincide con el plato restante, pero el registro de colocaciones bloquea ese
Dish para todas las visitas mientras siga en el verde. El pad no detecta comida
como un cliente; el bloqueo estaba en DeliveryZone, separado de la reserva FIFO.
Las dos pruebas nuevas del comportamiento solicitado fallaron antes de corregirlo.

El registro de colocaciones vincula ahora Dish y Order.InstanceId. Una colocación
rechazada se procesa una vez para ese pedido; un pedido posterior puede evaluar
el mismo plato apoyado, sin exigir retirarlo previamente. Si coincide, el único
PaymentLedger cobra una vez y el carrier recibe ese mismo Dish con sus FoodState
originales. Se mantienen apoyo/overlap, F, Pickup libre y baja velocidad; los platos
vendidos no se pueden revender. Un pedido incorrecto sigue sin pagar y el plato
sigue disponible. Esta corrección sustituye la exigencia histórica de retirar el
rechazado antes del siguiente cliente de M6/M10 y ADR 0019.

El resultado breve queda antes del detalle de la cola y aparece seis segundos en
la región de mensajes del único DebugHudPresenter. Indica aceptación y dinero
pagado, o rechazo con solicitado/entregado; conserva el número del cliente que
recibió ese resultado. La revisión cambia solo al resolver una transacción real,
sin cobrar ni evaluar desde UI. El recibo completo permanece consultable.

Se comprueba también con Update automático y pasos físicos reales que dos
clientes consecutivos pagan, llevan exactamente sus ingredientes hasta Exit,
retiran esos objetos del único FoodSimulation y dejan paso al siguiente cliente.
La prueba incluye dos posiciones/yaws en el pad y verifica una venta por visita.
Dos rechazos seguidos de un pedido correcto conservan el mismo Dish y
producen un único pago; repetir Poll no publica otro resultado ni otra venta.

Validación enfocada: **76/76 PlayMode** de M6ServiceTests, M6PrototypeTests y
M10CustomerQueueTests. La reproducción previa del bloqueo falló **2/2**;
la prueba física de dos visitas automáticas pasó **1/1** antes del cambio del
registro. También pasó la prueba automática de rechazo, salida y recogida del mismo
plato por el siguiente cliente, sin recolocación ni llamadas manuales a Poll.

Suites completas finales: **244/244 PlayMode** y **256/256 EditMode**, sin fallos
ni omitidas; Unity devuelve **0** en ambas. Incluyen regresiones M1–M13.
XML: `TestResults/ExitQueuePlayModeFinal.xml` y `TestResults/ExitQueueEditModeFinal.xml`;
logs homónimos bajo Logs. La primera suite completa pasó 242/243: una prueba nueva
avanzaba incompletamente la salida antes de comprobar el siguiente pedido. Se
corrigió esa secuencia de test y se repitió la suite completa hasta obtener verde.

Aplicado a `C:/Users/Usuario/Zero Stars Restaurant Fixes`, rama
`fix/customer-delivery-and-queue`, commit de implementación `5829204`. Los seis
archivos de código/tests coinciden byte a byte con los validados; los otros **437**
archivos versionados de Assets conservan sus bytes, incluidas escenas, materiales
y metas. Las dos reglas permanentes del usuario en AGENTS.md siguen idénticas.
No se cierra su Editor ni se ejecuta otro en esa carpeta. La importación del código
en el Editor interactivo seguía pendiente al sincronizar: enfocar Unity fuera de
Play, usar Assets / Refresh si hace falta y esperar a compilar antes de probar.

Archivos modificados bajo `Assets/_Project`: `Scripts/Runtime/Orders/DeliveryZone.cs`,
`Scripts/Runtime/Customers/CustomerServiceLoop.cs`, `Scripts/Runtime/Orders/OrderFeedback.cs`,
`Scripts/Runtime/Presentation/DebugHudPresenter.cs`, `Tests/PlayMode/M6ServiceTests.cs`
y `Tests/PlayMode/M10CustomerQueueTests.cs`. Documentación: AGENTS.md, README.md,
Docs/ARCHITECTURE.md, Docs/M6.md, Docs/M10.md, ADR 0019 y este informe.
No se modifican escenas, materiales, prefabs, metas, paquetes o configuración.

Prueba manual: salir de Play, esperar compilación y empezar otra prueba. Preparar
el pedido que muestra el cliente, confirmar F y soltar sobre el verde. Ver pago
y plato siguiendo al cliente hasta Exit; el siguiente debe llegar a atención.
Para comprobar el fallo reproducido, ofrecer Cheeseburger al primer pedido
Hamburger: rechaza sin cobrar y sale. Dejar ese plato sobre el verde; el siguiente
pedido Cheeseburger debe recogerlo y pagar una vez sin tener que moverlo.
Confirmar en Game que se ve el mensaje breve y que los cambios manuales de escena
siguen conservados. Las capturas originales sin HUD no identifican el resultado
de aquella visita; no se afirma que se haya reproducido toda posible causa visual.

## Versión aprobada para main (2026-10-07)

El usuario confirmó que la entrega ya funciona en su Unity de `Zero Stars
Restaurant Fixes` y autorizó publicar esta versión en main. El Editor interactivo
ha compilado el código corregido. Se conserva íntegra la escena que ha guardado:
los 697 documentos YAML mantienen sus IDs, sin objetos ni componentes eliminados
o añadidos respecto de la última escena versionada. El único cambio de valores
es la posición del interruptor eléctrico, ahora `(3.72, 1.45, 6.98)`; el resto del
diff es orden y formato de serialización del Editor.

Se mantienen los resultados completos de la implementación: 256/256 EditMode y
244/244 PlayMode. Una prueba M12 usaba la posición anterior fija del jugador;
ahora sitúa al jugador respecto del interruptor real. Con la escena guardada
pasaron además **31/31 M12ElectricityTests PlayMode** y **4/4
M12ElectricitySceneTests EditMode**, con salida 0 en ambas. XML bajo TestResults:
`MainIntegrationElectricityPlay.xml` y `MainIntegrationElectricityScene.xml`.
No se repiten las suites completas. No se regenera la escena ni se modifica la
carpeta original más antigua. Unity continúa utilizando la carpeta Fixes.
