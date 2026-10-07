# Zero Star Restaurant

Prototipo de simulador sandbox de restaurante 3D en primera persona, desarrollado
en Unity. La visión final comienza con **0 €**, un local casi vacío y **sin electricidad**.
El objetivo es conseguir que el negocio sobreviva mediante sistemas combinables.

M1 permite caminar, mirar y saltar en un restaurante greybox. M2 añade interacción
genérica y cuatro cajas físicas para coger, transportar, soltar y lanzar. M3 añade
alimentos con estado independiente, deterioro temporal y temperatura, inspeccionables
con feedback de desarrollo. M4 añade una plancha física con cocción térmica;
M5 añade montaje libre, reconocimiento de platos y transporte agregado.
M6 cierra el ciclo con un cliente activo, pedido visible, entrega física,
evaluación y pago en céntimos; validado por el usuario en `9335899`.
El polish añade snap contextual, agarre coherente del plato y salida visible del
cliente con la venta. M1–M6 y polish están aprobados en `144cca5`.
M7 añade Fridge/Freezer físicos y conservación por temperatura real, con el mismo
reloj e identidad; validado por el usuario en `8872f0d`.
M8 añade compras físicas y reinversión con el mismo saldo M6, validado en `c12d408`.
M9 reorganiza cocina y zona pública, con mostrador continuo, entrada/salida
separadas y cuatro QueuePoints; validado en `aa146c3`. M10 conecta una cola
física FIFO de hasta cuatro clientes, un solo pedido activo y paciencia de debug.
M10 está validado en `bd8d33c`. M11 añade jornada y reloj del mundo: apertura,
cierre sin nuevas entradas, final al vaciarse y siguiente día conservando estados.
M11 está validado en `0c81923`. El [polish físico previo a M12](Docs/PHYSICAL-INTERACTION-POLISH.md)
añade hold/release y montaje sobre apoyos normales. El [ajuste de Plate opcional](Docs/PLATES-FREE-ASSEMBLY.md)
sustituye la reposición gratuita por compras y conserva la entrega por Dish.
M12 añade [electricidad y consumo](Docs/M12.md): suministro ON/OFF con **E**,
Grill/Fridge/Freezer dependientes y kWh acumulados, sin facturas. La escena
empieza **sin corriente**; encender en la pared norte antes de cocinar/enfriar.
Cada aparato tiene ON/OFF individual con **E** al apuntar a su cuerpo; su selección
se conserva durante cortes/restauración. Inicialmente los tres están ON.
M13 añade [costes y resultado diario](Docs/M13.md): el mismo ledger registra
compras/ventas y liquida alquiler una vez al terminar el
último cliente. El resumen permanece hasta Next Day y el saldo puede ser negativo.
La escena usa **10 € de desarrollo** configurables;
la primera provisión con 0 € sigue siendo una deuda de diseño.
La escena de plantilla
`SampleScene` permanece separada y conservada.

## Abrir el proyecto

1. En Unity Hub, añadir esta carpeta como proyecto existente.
2. Usar **Unity 6000.5.3f1**, la versión registrada en
   `ProjectSettings/ProjectVersion.txt`. No actualizar el Editor o los paquetes
   durante una tarea de gameplay.
3. Esperar a la importación y restauración de paquetes. Un clon limpio requiere
   acceso al registro de Unity o una caché válida de esos paquetes.
4. Abrir `Assets/_Project/Scenes/PrototypeRestaurant.unity` y revisar la Console.
5. Pulsar Play y enfocar Game: **WASD** mueve, **ratón** mira, **Espacio** salta,
   **Escape** libera el cursor y **clic izquierdo en Game** vuelve a capturarlo.
   Con el cursor libre se suspenden movimiento voluntario y mirada; la gravedad
   sigue activa. Detener Play también libera el cursor.
6. **Mantener clic izquierdo** recoge/sostiene objetos físicos; **soltar clic**
   libera conservando movimiento. Mover la cámara y soltar permite lanzarlos.
   **E** interactúa/recoge como alternativa y **G** suelta el agarre alternativo.
   El texto provisional muestra objeto, acción y binding. Las cajas M2 están
   conservadas en `PhysicalTestObjects`, inactivo; activarlo solo para desarrollo.
   Consultar [M2](Docs/M2.md) para probar masas, paredes y límites físicos.
7. Ir a Procurement en el pequeño anexo exterior al oeste del local, pasando por
   el hueco de la pared oeste desde cocina. Su banco está en `(−9,7; 1,25)` en XZ;
   [guía del acceso y validación](Docs/PROCUREMENT-AREA.md). Mirar un
   botón y **E** compra **Bun (0,35 €)**, **Raw Beef Patty (0,80 €)**,
   **Cheese (0,25 €)** o **Plate (0,50 € configurable)**. Aparece una unidad real en **OUTPUT**; recoger manteniendo clic y
   retirarla antes de comprar otra. Saldo y rechazo son visibles. Sin fondos o
   con salida ocupada no se cobra ni se crea comida. Ver [M8](Docs/M8.md).
   Las antiguas provisiones gratuitas están desactivadas; solo para desarrollo,
   seleccionar FoodTestZone y usar el menú contextual de DevelopmentIngredientSupply
   **Development: enable free ingredient fixtures**. Las guías M3–M7 que usan
   fixtures requieren ese opt-in explícito.
8. Llevar la carne comprada a la superficie rojiza `GrillHotSurface`, al norte de
   Prep, y soltar clic para depositarla. Temperatura y cocción avanzan por presencia física;
   retirarla Cooked pausa cocción y permite enfriamiento. El mismo FoodSimulation
   permite adelantar solo comida desde Inspector. Ver [M4](Docs/M4.md).
9. Apilar ingredientes soltándolos físicamente sobre encimera, mesa, bandeja o
   Grill. **Soltar clic con la mano tranquila apuntando a la pila** coloca y centra
   el ingrediente automáticamente; G permite soltar libremente y un gesto rápido
   conserva el lanzamiento natural. Mirar la pila muestra `Recognized: Hamburger`/`Cheeseburger`/`Custom Dish`;
   **F** confirma con manos libres. El plato final se recoge manteniendo clic.
   Los antiguos apoyos de `AssemblyStation1/2/3` son geometría fija: no regalan
   ni reponen platos. Plate se compra y es opcional; con o sin él se monta y sirve
   el mismo Dish. Al finalizar sobre Plate, el agregado incorpora ese objeto.
   Grill sigue calentando solo
   los ingredientes que toquen su zona, incluso después de confirmar.
   Ver [polish físico](Docs/PHYSICAL-INTERACTION-POLISH.md) para límites y pruebas.
10. Al llegar la cabeza de cola a Service Position, el panel derecho muestra pedido y el saldo de sesión
    (inicialmente **€10.00** en esta escena de desarrollo).
    Con el plato final sostenido, ir por detrás del mostrador, al pad verde
    `CustomerServiceZone/DeliveryPad`, colocar parte razonable del conjunto sobre el verde
    y **soltar clic** para depositarlo desde el pase de cocina. Se evalúa al reposar: Hamburger paga
    **€5.00**, Cheeseburger **€6.50** si coincide; incorrecto queda disponible.
    Calidad no afecta al cobro M6. El cliente lleva el mismo Dish delante del cuerpo;
    sale por la puerta este sin atravesar la cocina; se limpia en la salida y los demás
    avanzan una plaza. Puede entrar otro por Entrance cuando hay espacio. El panel
    de cola muestra espera y paciencia por cliente; cero solo informa.
    El resultado breve aparece antes de la cola y durante seis segundos abajo:
    aceptación/pago o rechazo con solicitado/entregado. Una colocación rechazada
    bloquea solo ese pedido; otro cliente puede evaluar el plato que siga en verde.
    Ver [M10](Docs/M10.md) para probar varias ventas y [M6](Docs/M6.md) para el pipeline.
    Seguir [las pruebas actuales del polish](Docs/PHYSICAL-INTERACTION-POLISH.md) para
    manejo físico. Ver [Plate y montaje libre](Docs/PLATES-FREE-ASSEMBLY.md) para la
    compra y entrega con/sin utensilio. Las posiciones vigentes y el
    loop comprar → almacenar → cocinar → montar → entregar → cobrar están en [M9](Docs/M9.md).
    La [entrega tolerante](Docs/DELIVERY-TOLERANCE-FIX.md) admite bordes y colocación
    parcial: basta soltar y dejar reposar; no exige centrar el plato.
    La [transferencia al cliente](Docs/DISH-CUSTOMER-HANDOFF-FIX.md) fija el agregado
    original antes de resolver la visita y lo sigue hasta Exit sin consultar al
    jugador; tras soltar se puede mirar hacia otro lado y alejarse.

11. Los gabinetes abiertos **Fridge** (azul claro, +4 °C) y **Freezer** (azul oscuro,
    −18 °C) están contra la pared oeste de cocina y abren hacia el este. **E** recoge comida,
    mirar dentro aproximadamente horizontal y **G** la deposita en el estante;
    **E** recupera normalmente. La temperatura cambia gradualmente al guardar y
    retirar, conservando el estado original.
    Deterioro por temperatura real: normal, 10× más lento refrigerado y 1000× más
    lento congelado. Edad sigue avanzando. Ver [M7](Docs/M7.md) para medirlo con
    los botones del único FoodSimulation y comprobar límites/identidad.

La zona pública queda al sur del mostrador; los clientes entran por la puerta
oeste y salen por la puerta este. Los cuatro QueuePoints forman la cola M10:
el primero es Service Position. El jugador empieza en cocina.

M11 inicia Day 1 a las 09:00, abre a las 09:00 y cierra a las 17:00; velocidad
de mundo 60 (ocho minutos de horario). El HUD muestra día/hora/fase. Solo Open
admite clientes nuevos; los que ya están dentro terminan después del cierre.
En `CustomerServiceZone`, usar el menú contextual de RestaurantDayController
**Development: Start Day / Next Day** cuando quede Closed. El mismo componente
permite pausa/resume del reloj para desarrollo. Hora del mundo y FoodSimulation
tienen velocidades independientes; Next Day no avanza una noche ni resetea comida
o saldo. Ver [M11](Docs/M11.md) para configuración y prueba de dos días consecutivos.

Configuración comprobada: **URP 17.5.0**, calidad PC activa y color Linear;
**Input System 1.19.0** como sistema de entrada activo;
**Unity Test Framework 1.7.0** instalado. El inventario completo está en
[la auditoría inicial](Docs/PROJECT_AUDIT.md).

El interruptor eléctrico está en cocina `(3.72, 1.45, 6.98)`, en la posición
guardada por el usuario: **E**
con manos libres conmuta los tres aparatos. Sin corriente, la comida vuelve
gradualmente a ambiente mediante el mismo FoodSimulation. El HUD muestra W/kWh
totales y por aparato; ON/OFF conserva energía. Next Day conserva los históricos
y reinicia solo los diarios. Ver [M12](Docs/M12.md) y [M13](Docs/M13.md).

M13 configura 3 € de alquiler/día y 30 céntimos/kWh en
`Assets/_Project/ScriptableObjects/Economy/OperatingCosts.asset`. Cuando Closing
queda vacío, Closed cobra alquiler una sola vez y muestra Sales/Purchases/Fixed costs,
Operating net y Balance. La electricidad se devenga y permanece pendiente entre días;
**Development: Settle Electricity Bill** en RestaurantOperatingCosts paga el período
una vez. [Corrección de entrega, HUD y facturación](Docs/DELIVERY-HUD-ELECTRICITY-FIX.md).
Usar **Development: Start Day / Next Day** del controlador después
del resumen: conserva saldo, objetos y estados. [Guía de prueba de beneficio,
pérdida y saldo negativo](Docs/M13.md#prueba-manual-de-un-día-completo).

## Filosofía del prototipo

Primitivas, materiales simples y placeholders. Primero demostrar el ciclo jugable;
después mejorar la presentación. Sin modelos personalizados, Blender, nuevas
texturas, animaciones, shaders complejos, arte definitivo ni assets externos
innecesarios. Las reglas y el estado deben poder sobrevivir a un cambio de modelo,
material, prefab o interfaz.

## Primer vertical slice

**Player → Interaction → Food → Cooking → Dish Assembly → Customer Order → Delivery → Payment**

Una pequeña escena permitirá coger una porción, cocinarla con una fuente de calor
eléctrica M12, montar un plato, entregarlo a un cliente placeholder y cobrar una
única vez. M8 amplía el loop: **dinero → comprar unidades físicas → conservar/cocinar
→ vender → reinvertir**. Provisiones gratuitas solo bajo opt-in de desarrollo.
Con 0 € y sin esas fixtures, la primera provisión queda pendiente de diseño.

## Documentación

- [Reglas para agentes](AGENTS.md).
- [Estado inicial y configuración comprobada](Docs/PROJECT_AUDIT.md).
- [Arquitectura, estructura y convenciones](Docs/ARCHITECTURE.md).
- [Slice, milestones y criterios verificables](Docs/ROADMAP.md).
- [Flujo Git y comprobaciones en Unity](Docs/WORKFLOW.md).
- [Decisión arquitectónica inicial](Docs/Decisions/0001-prototype-foundation.md).
- [Inventario de cambios y validación de esta entrega](Docs/FOUNDATION.md).
- [M1: pruebas, controles, configuración y reconstrucción del greybox](Docs/M1.md).
- [M2: interacción física, validación y pruebas manuales](Docs/M2.md).
- [Decisión sobre interacción y agarre físico](Docs/Decisions/0003-physical-interaction.md).
- [M3: alimentos, definiciones, deterioro, pruebas y límites](Docs/M3.md).
- [Decisión sobre estado de alimento y tiempo](Docs/Decisions/0004-food-state-and-time.md).
- [M4: plancha, cocción térmica, validación y límites](Docs/M4.md).
- [Decisión sobre fuente térmica y cocción](Docs/Decisions/0005-thermal-cooking.md).
- [M5: montaje, reconocimiento, transporte y validación](Docs/M5.md).
- [Decisión sobre composición física e identidad](Docs/Decisions/0006-physical-dish-assembly.md).
- [M6: cliente, pedido, entrega, evaluación, pago y comprobación](Docs/M6.md).
- [Decisión sobre transacción y separación de calidad](Docs/Decisions/0007-customer-delivery-payment.md).
- [Polish del slice: controles, distribución, archivos, tests y validación](Docs/VERTICAL-SLICE-POLISH.md).
- [Decisión sobre snap, agarre y retirada de ventas](Docs/Decisions/0008-vertical-slice-polish.md).
- [M7: Fridge/Freezer, conservación, pruebas y límites](Docs/M7.md).
- [Decisión sobre almacenamiento y política térmica](Docs/Decisions/0009-food-storage-refrigeration.md).
- [M8: economía, compras físicas, pruebas y recorrido manual](Docs/M8.md).
- [Decisión sobre adquisiciones y deuda del inicio con 0 €](Docs/Decisions/0010-economy-ingredient-procurement.md).
- [M9: distribución, recorridos, pruebas y comprobación manual](Docs/M9.md).
- [Decisión sobre distribución y servicio](Docs/Decisions/0011-restaurant-layout-service-flow.md).
- [M10: cola física, paciencia, pruebas y secuencia manual](Docs/M10.md).
- [Decisión sobre reservas de cola e identidad](Docs/Decisions/0012-customer-queue-and-patience.md).
- [M12: electricidad, consumo, tests y comprobación manual](Docs/M12.md).
- [Decisión sobre suministro y medición](Docs/Decisions/0017-electricity-and-utilities.md).
- [M11: jornada, reloj, pruebas y dos días consecutivos](Docs/M11.md).
- [Decisión sobre tiempo del mundo y simulación](Docs/Decisions/0013-restaurant-day-and-world-time.md).
- [M13: contabilidad diaria, liquidación, tests e inventario](Docs/M13.md).
- [Decisión sobre facturas y costes diarios](Docs/Decisions/0018-daily-bills-and-operating-costs.md).

`main` apunta al último estado aprobado `144cca5` al comenzar M7. La foundation está en
`feature/project-foundation`; M1 parte de ella en `feature/player-controller`.
M2 parte de M1 aprobado en `feature/world-interaction`.
M3 parte de M2 aprobado (`14e0575`) en `feature/food-state`.
M4 parte de M3 aprobado (`307174a`) en `feature/basic-cooking`.
M5 parte de M4 aprobado (`d7eb2bf`) en `feature/dish-assembly`.
M6 parte de M5 y feedback aprobados (`6a3ca72`) en `feature/customer-service-loop`.
Su polish parte de M6 aprobado (`9335899`) en `fix/vertical-slice-polish`.
M7 parte del slice/polish aprobado (`144cca5`) en `feature/food-storage-refrigeration`.
M8 parte de M7 aprobado (`8872f0d`) en `feature/economy-procurement`.
M9 parte de M8 validado (`c12d408`) en `feature/restaurant-layout-service-flow`.
M10 parte de M9 validado (`aa146c3`) en `feature/customer-queue`: hasta cuatro
clientes físicos, un pedido activo, paciencia de debug y avance FIFO al completar Exit.
M11 parte de M10 validado (`bd8d33c`) en `feature/restaurant-day`.
Hay remoto `origin` configurado. Esta tarea no hace merge a main ni publica cambios.

M12 parte de `ec2ba3e` en `feature/electricity-utilities`, con worktree aislado
`C:/Users/Usuario/Zero Stars Restaurant M12`; las ediciones locales previas y el
Editor original se conservan.

M13 parte del último M12 validado `6258bad` en `feature/bills-operating-costs`,
worktree `C:/Users/Usuario/Zero Stars Restaurant M13`. Ambos Editores previos y
los cambios de la carpeta original se conservan; sin merge ni publicación.
