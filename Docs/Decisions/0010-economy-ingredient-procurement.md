# ADR 0010: compras de unidades físicas con el saldo M6

Fecha: 2026-10-06. Estado: implementada para M8 desde `8872f0d`.

## Contexto

M6 ya dispone de un PaymentLedger de sesión, ingresos en céntimos enteros y
protección contra doble venta. M7 conserva la identidad de FoodState y utiliza
un único FoodSimulation. El siguiente paso autorizado es gastar dinero en
ingredientes físicos y reinvertir los ingresos, sin inventario ni supermercado.
La visión final comienza con 0 €, pero aún no define cómo obtener la primera
provisión. M8 no debe resolver esa deuda inventando préstamos, robo o regalos.

## Decisión

- Extender **PaymentLedger**, sin crear un segundo balance. `TrySpend` valida
  precio positivo, fondos, ID de transacción e ID de unidad antes de mutar.
  Repetir cualquiera de los dos IDs no vuelve a descontar. Ingresos, rechazo,
  snapshots de entrega, `IsSold` y retirada M6 conservan sus reglas.
- **IngredientProduct** configura prefab físico y precio en céntimos. Hay tres
  productos: Bun 35, Raw Beef Patty 80 y Cheese 25. La estación valida y copia
  precios/referencias al iniciar. Estado de partida nunca se escribe en assets.
  El coste de referencia de FoodDefinition sigue siendo evidencia de configuración
  en M6; no es un historial contable del precio de compra si se cambian los precios.
- **IngredientPurchaseStation** referencia explícitamente el mismo
  CustomerServiceLoop/FoodSimulation. Tres **IngredientPurchaseButton** reutilizan
  Interactable/E; no amplían el input ni crean selección global. Manos libres para
  comprar; recoger el resultado usa el Pickup normal.
- Cada operación síncrona comprueba fondos y salida física libre antes de
  instanciar. Un prefab raíz inactivo permite preparar su FoodState independiente
  sin exponerlo a física. `FoodItem.TryInitialize` es idempotente: Awake reutiliza
  ese mismo estado. Se registra en el reloj, se descuenta una vez y se activa.
  Un fallo de preparación/registro/pago destruye la unidad provisional y desregistra
  si procede, sin cargo. No hay callbacks, asincronía ni inventario intermedio.
- La salida comprueba tanto el volumen del próximo ingrediente como una columna
  sobre la mesa. Una unidad que ya ha caído sigue ocupando la salida: retirarla
  antes de comprar evita superposición y compras accidentales repetidas.
- **FoodSimulation.Register** admite unidades inicializadas, rechaza referencias
  repetidas y preserva el único avance por unidad. No nace otro Update alimentario.
  Prefabs de carne/queso parten de +4 °C; pan de +21 °C. Temperatura, conservación,
  contaminación, deterioro, cocción y montaje siguen siendo los mismos sistemas.
- **DevelopmentIngredientSupply**, antes del Awake de alimentos, desactiva y
  desregistra todas las fixtures gratuitas M3–M7 por defecto. Se conservan sus
  objetos y referencias para regresiones. Opt-in explícito en Inspector antes
  de Play o menú contextual de desarrollo en Play; activarlas repetidamente no
  duplica registro ni restaura estado.
- El saldo de código sigue comenzando en **0**. Solo la escena de desarrollo
  configura **1000 céntimos** en CustomerServiceLoop al comenzar la sesión.
  Rehabilitar componentes no reinicia dinero; una nueva sesión crea otro ledger.

## Consecuencias y deuda

El loop comprable ya puede probarse: 1000 → comprar dos panes y carne → 850 →
vender Hamburger → 1350 → comprar queso → 1325 céntimos. Precios de venta siguen
siendo 500/650 y calidad no bloquea aceptación/pago. No hay persistencia.

Con saldo de desarrollo **0** y fixtures desactivadas, no hay vía para comprar ni
realizar la primera venta. Es una deuda de diseño deliberada: decidir posteriormente
la obtención de provisiones iniciales conservando la visión 0 €, local vacío y sin
electricidad. El dinero de prueba y los aparatos térmicos activos son fixtures,
no mecánicas económicas/eléctricas finales. No se implementan otros milestones.

La estación es greybox y sus productos/columnas de salida usan cajas simples.
Su UI comunica precio, saldo y rechazo, sin arte final, carrito, inventario,
proveedores, préstamo, robo, empleados ni cambios de clientes.
