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
