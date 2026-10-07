# Contenido de Zero Star Restaurant

Todo el contenido propio del juego se incorpora aquí. La plantilla existente en
`Assets/Scenes`, `Assets/Settings`, `Assets/TutorialInfo` y el asset de input se
conservan en sus ubicaciones actuales para mantener sus GUID y referencias.

| Carpeta | Responsabilidad |
| --- | --- |
| `Scripts/Domain` | Estado y reglas en C# sin dependencias de Unity. |
| `Scripts/Runtime` | Componentes de Unity, input, física, vistas y composición. |
| `Scenes` | Escenas propias: `PrototypeRestaurant` para M1–M11. |
| `Prefabs` | Objetos reutilizables con primitivas y componentes. |
| `ScriptableObjects` | Datos de configuración compartidos, sin estado de partida. |
| `Materials` | Materiales simples de color compatibles con URP. |
| `Tests/EditMode` | Pruebas de reglas y validación de datos. |
| `Tests/PlayMode` | Pruebas de integración con escenas y componentes. |
| `Editor` | Herramientas que solo se ejecutan en el Editor. |

Las carpetas vacías contienen `.gitkeep` para que existan también tras clonar.
M1 incluye `Scenes/PrototypeRestaurant.unity`, tres materiales greybox, controlador
FPS en `Scripts/Runtime/Player`, generador de escena en Editor y tests EditMode y
PlayMode. En M1, Prefabs quedó reservado para tareas futuras.
M2 añade `Scripts/Runtime/Interaction`, cuatro materiales de cajas y extiende el
generador/escena de M1. Usa el asset de input existente con Interact, Drop y Throw.
Ver `Docs/M2.md` en la raíz.
M3 incorpora reglas en `Scripts/Domain/Food`, adaptadores en `Scripts/Runtime/Food`,
tres definiciones en `ScriptableObjects/Food`, tres materiales y una zona con cuatro
alimentos. Estado, Pickup y representación siguen separados.
Ver `Docs/M3.md` y ADR 0004 en la raíz.
M4 añade `Scripts/Domain/Cooking`, `Scripts/Runtime/Cooking`, plancha de primitivas,
cuatro carnes adicionales y un material de superficie. FoodSimulation es el único
driver temporal; fuentes térmicas solo describen el entorno por contacto físico.
Ver `Docs/M4.md` y ADR 0005 en la raíz.
M5 añade `Scripts/Domain/Dishes`, `Scripts/Runtime/Dishes`, dos definiciones de
reconocimiento en `ScriptableObjects/Dishes`, tres bandejas, diez provisiones y
un material de color. El plato final mantiene FoodItems/estados originales con
un proxy físico único. Ver `Docs/M5.md` y ADR 0006 en la raíz.
M6 añade `Scripts/Domain/Customers`, `Orders`, `Economy` y adaptadores en
`Scripts/Runtime/Customers` y `Orders`. `ScriptableObjects/Customers/CustomerService`
configura menú, precios y tiempos; dos materiales simples y `CustomerServiceZone`
añaden cliente de primitivas, ruta y pad de entrega. Sin generación automática de
comida ni otro reloj. Ver `Docs/M6.md` y ADR 0007.
Polish del slice desde M6 aprobado `9335899`: colocación rápida en AssemblySurface
con clic izquierdo, agarre del Dish desde cualquier parte y CustomerDishCarrier
transportando la misma venta hasta la salida. Greybox compacto Prep → Grill →
Assembly → Delivery, sin cambios Domain. Ver `Docs/VERTICAL-SLICE-POLISH.md` y
ADR 0008. Los tests ya tienen sus assemblies; no añadir NUnit al código Runtime.
M7 añade ColdStorage para Fridge/Freezer abiertos, FoodPreservationProfile en
Domain y FoodPreservationSettings en `ScriptableObjects/Food`. El único reloj
integra conservación por temperatura real durante enfriamiento/calentamiento;
Pickup e input se conservan. Ver `Docs/M7.md` y ADR 0009. Sin electricidad.

M8 añade `Runtime/Economy`, tres `IngredientProduct` en ScriptableObjects/Economy,
tres prefabs físicos inactivos en Prefabs/Food y estación de compra greybox.
Compras y ventas comparten el PaymentLedger M6; cada unidad comprada crea su
FoodState y se registra en el único FoodSimulation M7. Las 18 provisiones gratuitas
quedan bajo opt-in explícito de DevelopmentIngredientSupply, desactivadas por defecto.
La escena configura 1000 céntimos de desarrollo; primera provisión con 0 € pendiente
de diseño. Ver `Docs/M8.md` y ADR 0010. No hay inventario ni otro milestone.

M8 está validado en `c12d408`. M9 separa cocina y zona pública con mostrador
continuo, puertas de entrada/salida y cuatro QueuePoints pasivos. Storage,
Procurement, Prep, Grill, Assembly y pase forman un recorrido de trabajo.
El único cliente usa una salida explícita; las fixtures/cajas de tests quedan
apartadas o bajo opt-in. Ver `Docs/M9.md` y ADR 0011. No hay lógica de cola.

M9 está validado en `aa146c3`. M10 conecta cuatro plazas FIFO con movimiento
determinista: la primera es Service Position, solo ella tiene pedido. Cada
cliente tiene identidad y paciencia independientes; cero solo informa. Se
reutilizan entrega, evaluación, ledger y transporte M6. Ver `Docs/M10.md` y
ADR 0012. Sin pedidos simultáneos ni navegación compleja.

M10 está validado en `bd8d33c`. M11 añade `Domain/Time` y `Runtime/Time` para
GameTime/RestaurantDay, apertura y cierre de admisiones. La cola existente
termina antes de Closed. Next Day conserva ledger, comida y resto del restaurante;
FoodSimulation no cambia ni recibe tiempo del mundo. Ver `Docs/M11.md` y ADR 0013.

Consulta `README.md`, `AGENTS.md` y `Docs/ROADMAP.md` en la raíz antes de trabajar.

M11 está validado en `0c81923`. El polish físico previo a M12 añade
PhysicalDishAssembly para pilas apoyadas fuera de estaciones, DishTraySupply y
Prefabs/EmptyDishTray para reposición sin clonar alimentos. Clic izquierdo usa
hold/release, E acciones contextuales y F confirma. FoodItem conserva geometría
local de colliders retirados para Grill; FoodSimulation permanece único.
El release tranquilo sobre apoyo/pila cercana conserva la colocación asistida;
G y gestos rápidos liberan libremente. No se auto-finaliza una receta.
Ver `Docs/PHYSICAL-INTERACTION-POLISH.md` y ADR 0014. No implementa M12.

M12 incorpora `Domain/Utilities` y `Runtime/Utilities`: suministro general OFF al
iniciar, interruptor E y consumo nominal W/kWh por aparato/total. Grill/Fridge/Freezer
requieren corriente; OFF retira el entorno térmico y el único FoodSimulation usa
ambiente sin recrear alimentos. Next Day conserva suministro y medidores.
Ver `Docs/M12.md` y ADR 0017; M13 extiende su medición con costes diarios.

M13 reutiliza PaymentLedger y RestaurantDay: transacciones inmutables, alquiler
configurable y electricidad por kWh real, liquidación única al cerrar sin clientes.
Next Day conserva dinero/objetos/estados, resetea solo diarios y retira el resumen.
OperatingCosts.asset configura 300 céntimos de Rent y 30 céntimos/kWh. Ver
`Docs/M13.md` y ADR 0018. Sin consecuencias de deuda ni guardado.

M14 añade DeliveryContents y OrderSatisfaction en Domain/Orders. Evalúa Dish y
FoodItem originales sueltos en el único PASS sin exigir F. Expected/Received/
Missing/Extra y receta se separan de snapshots de seguridad; pesos del menú
determinan céntimos reales, extras no aumentan máximo. Ledger y FoodState.IsSold
impiden pago/reventa duplicados. El carrier conserva todas las unidades y Plate
hasta Exit; Summary M13 usa ingresos reales. Ver `Docs/M14.md` y ADR 0020.
