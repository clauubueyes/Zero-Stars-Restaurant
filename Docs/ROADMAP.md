# Vertical slice y roadmap

El [ajuste de Plate y montaje libre](PLATES-FREE-ASSEMBLY.md) añade un utensilio
comprable opcional, elimina la reposición gratuita y confirma por soporte físico
sin AssemblySurface. No cambia la distribución ni avanza a M12.

La [zona independiente de Procurement](PROCUREMENT-AREA.md) sitúa la estación M8
en un pequeño anexo oeste con acceso directo. Conserva funcionalidad y referencias;
Storage/Prep/Grill/Assembly y servicio no cambian. No añade tienda ni milestone.

Estado actual: **M0–M6 aprobados por el usuario; M6 aprobado en `9335899`**.
La [corrección de transferencia](DISH-CUSTOMER-HANDOFF-FIX.md) refuerza la entrega
Dish/DeliveryZone: aceptación y fijación al cliente en una operación, seguimiento
de todos los cuerpos originales hasta Exit y tests con mirada desviada y jugador
en distintas posiciones. Sin nuevo milestone; aceptación manual pendiente.
El [polish del slice](VERTICAL-SLICE-POLISH.md) mejora snap, agarre, transporte de
ventas y greybox en `fix/vertical-slice-polish`.
La prueba manual del slice detectó precisión excesiva en entrega; la
[corrección de tolerancia](DELIVERY-TOLERANCE-FIX.md), desde `17bacd9`, permite
solapamiento parcial razonable y conserva apoyo, manos libres y pago único.
Ver [M1](M1.md), [M2](M2.md), [M3](M3.md), [M4](M4.md), [M5](M5.md) y [M6](M6.md).
La petición aprobada de M6 amplía su alcance a Customer Order → Delivery → Payment
y sustituye la división anterior M6 pedido/M7 entrega-pago. También sustituye el
rechazo por Raw/Burnt/calidad: ahora calidad y corrección se informan separadamente,
y solo la coincidencia estructural controla aceptación/pago. Ver ADR 0007.
M1–M6, polish y entrega tolerante aprobados en **`144cca5`**. La petición de M7
autoriza exclusivamente **Food Storage & Refrigeration**, implementado en
`feature/food-storage-refrigeration` y validado en **`8872f0d`**; ver [M7](M7.md).
**M8: Economy & Ingredient Procurement** está validado en **`c12d408`**.
Ver [M8](M8.md) y ADR 0010. **M9: Restaurant Layout & Service Flow** está validado
en **`aa146c3`**; ver [M9](M9.md) y ADR 0011. **M10: Customer Queue** está validado
en **`bd8d33c`**; ver [M10](M10.md) y ADR 0012. **M11:
Restaurant Day & Game Time** está validado en `0c81923`: jornada, apertura,
cierre de nuevas admisiones, final al vaciarse y siguiente día sin resetear
restaurante. Ver [M11](M11.md) y ADR 0013. El polish físico posterior y Plate
opcional están en la base `ec2ba3e`. **M12: Electricity & Utilities** introduce
suministro eléctrico ON/OFF, dependencia térmica de Grill/Fridge/Freezer y consumo
acumulado configurable; validado en `6258bad`. Ver [M12](M12.md) y ADR 0017.
**M13: Bills & Operating Costs** añade contabilidad de compras/ventas, alquiler y
electricidad devengada, liquidación única de costes fijos al vaciarse después del cierre, resumen y
Next Day conservando estado. Ver [M13](M13.md) y ADR 0018. Aceptación visual pendiente.
No se autorizan préstamos, intereses, consecuencias de deuda, game over, impuestos,
personal, agua/gas, mantenimiento, averías, guardado ni M14.

## Experiencia objetivo

**Player → Interaction → Food → Cooking → Dish Assembly → Customer Order → Delivery → Payment**

En una sala pequeña hecha de primitivas, el jugador recoge una porción, la coloca
en una estación de calor, la retira cocinada, la añade a un plato, consulta un
pedido, lo entrega a un cliente de primitivas y recibe dinero. El flujo ocurre desde
primera persona y da feedback visible para cada acción o rechazo.

## Alcance mínimo y decisiones de prueba

- Un jugador, teclado/ratón, una sala y un pedido activo. M10 añade hasta cuatro
  clientes físicos FIFO, incluida atención; sin pathfinding ni turnos de empleados.
- Un tipo de ingrediente basta para demostrar identidad y estado; cada porción
  tiene estado propio. M3 incluye deterioro básico Fresh/Acceptable/Spoiled/Rotten,
  independiente de cocción M4: Raw, Undercooked, Cooked, Overcooked, Burnt.
  Factores de deterioro más detallados llegarán después; no confundir los dos ejes.
- Cocción por exposición térmica acumulada con una fuente física efectiva,
  dependiente de temperatura y parámetros configurados. M4 amplía el plan inicial
  por petición del usuario: varias piezas, temperatura/potencia y etapas adicionales.
  Se aproxima el intercambio exponencial, sin fluidos, cortes, energía conservada
  o contactos de precisión; ver [ADR 0005](Decisions/0005-thermal-cooking.md).
- Montaje libre por contenido: un plato admite porciones que el jugador deposita;
  no exige receta, slots exactos ni una malla concreta. M5 reconoce pilas aproximadas
  Hamburger/Cheeseburger por datos, y permite Custom Dish para cualquier otra
  composición no vacía; orden/pila y transporte según [ADR 0006](Decisions/0006-physical-dish-assembly.md).
  Reconocer no implica seguridad. En M6 el ID de plato reconocido debe coincidir
  con el solicitado; Raw/Burnt/Rotten/contaminado no invalidan la coincidencia ni pago.
- Menú configurable Hamburger (**500 céntimos**) y Cheeseburger (**650 céntimos**),
  alternados por visita con override de desarrollo para el siguiente cliente.
  Entrega válida cobra una vez; los rechazos no consumen el plato
  ni cambian el saldo. Entrega/pago deben ser una operación coherente sin estados
  parciales si se repite el input.

La visión final comienza con saldo **0 €**, local vacío y sin electricidad.
M8 permite un saldo inicial configurable **exclusivamente de desarrollo** (1000
céntimos en la escena), sobre el único ledger M6. Los ingredientes normales se
compran como unidades físicas; toda provisión gratuita anterior requiere opt-in
explícito de desarrollo/testing. Una nueva sesión restaura solo el saldo configurado;
no existen compras ni estado persistentes en assets.

M12 inicia la escena sin corriente: el interruptor E en la pared norte habilita
Grill/Fridge/Freezer. La iluminación ambiental permanece de desarrollo. La primera
provisión jugable con 0 € queda pendiente de diseño; no se inventa financiación
ni desbloqueo comercial de electricidad. Combustible sigue fuera del alcance.

## Milestones verificables

Cada milestone debe poder revisarse con su propio commit o serie pequeña de
commits. La dependencia es el milestone anterior, salvo M0. Si un milestone crece,
dividirlo manteniendo un resultado comprobable en cada paso.

| ID / rama propuesta | Resultado acotado | Criterios de aceptación y comprobación |
| --- | --- | --- |
| M0 · `feature/project-foundation` | Git, carpetas, documentación y plan. | Metas completas, fuentes y caches separados, sin cambios en gameplay/paquetes/configuración. Abrir SampleScene y comprobar carpetas/Console. |
| M1 · `feature/player-controller` | Escena `PrototypeRestaurant`, suelo/paredes de primitivas, cámara y jugador. | WASD mueve y ratón mira; no atraviesa paredes; el cursor se captura/libera; entrar/salir de Play dos veces no duplica input. Cámara y velocidad configurables. Sin interacción todavía. |
| M2 · `feature/world-interaction` | Contrato genérico, raycast de alcance limitado y feedback; coger/transportar/soltar/lanzar cuatro cajas físicas. | Mostrar prompt y bindings solo con control/objetivo válido; paredes y alcance bloquean la acción; un agarre exclusivo; caminar/mirar sosteniendo; comparar masas/tamaños; limitar velocidades y liberar si falta espacio seguro o desaparece el objeto. Interact pasa de Hold a pulsación; Drop/Throw configurables. Tests deterministas, física PlayMode y aceptación manual según M2.md. |
| M3 · `feature/food-state` | Tres definiciones y cuatro alimentos físicos; Domain independiente de Unity, deterioro/temperatura e inspección provisional. | Dos porciones comparten definición pero no estado; recoger/soltar/lanzar conserva identidad y no muta el asset. Frescura 0–100 y transiciones por umbrales; temperatura por unidad y grandes saltos temporales deterministas. Fixture fresca/envejecida/fría/contaminada y aceleración solo de desarrollo. Tests EditMode/PlayMode y regresión M1/M2 según M3.md; sin cocción. |
| M4 · `feature/basic-cooking` | Plancha física con varias porciones, temperatura y cocción continua separada de deterioro. | Raw → Undercooked → Cooked → Overcooked → Burnt por dosis térmica configurable; carne cocinable, pan/queso no. Calentamiento hacia plancha y enfriamiento hacia ambiente; retirar pausa dosis, volver conserva estado. Zona física sin registros obsoletos/duplicados; tests de saltos grandes, umbrales, contaminación, carne podrida, ciclo de vida y regresiones M1–M3 según M4.md. Sin platos. |
| M5 · `feature/dish-assembly` | Montaje físico libre, identidad y estado real por ingrediente; reconocimiento data-driven y plato final manipulable. | Añadir/retirar/reorganizar por colocación, pertenencia exclusiva e IDs propios; pila ordenada reconoce Hamburger/Cheeseburger, otras combinaciones son Custom Dish. Confirmar no permite vacío ni duplicación; conserva referencias, estado, orden y datos agregados vivos. Proxy físico transporta sin dispersar ingredientes. Tests Domain/PlayMode e integración Pickup→Cooking→Assembly según M5.md; sin clientes/pedidos/pagos. |
| M6 · `feature/customer-service-loop` | Un cliente móvil por ruta, pedido, entrega física, evaluación separada y pago. | Enter → Order → Wait → Receive → Evaluate → Pay/Reject → Leave; pedido con ID independiente y menú configurable/forzable. Solo Dish final suelto/intacto apoyado se evalúa. Coincidencia paga 500/650 céntimos una vez; con el polish el vendido viaja visible con el cliente, se desregistra y destruye al llegar a salida. Incorrecto/Custom conserva plato y cierra visita sin cobrar. Calidad no afecta pago. Recibo con IDs/ingredientes/frescura/cocción/contaminación/coste, saldo 0 inicial y siguiente cliente con delay. Tests Domain, físicos e integración/regresiones según M6.md y VERTICAL-SLICE-POLISH.md. |
| Polish · `fix/vertical-slice-polish` | Corrección del flujo M1–M6, desde `9335899`. | Clic contextual conserva la Food sostenida y la apila con bounds; E permite retirar. F sigue confirmando; todas las partes resuelven el mismo Pickup del Dish, también Custom. Venta conserva mismo agregado y recibo; impide recogida/reventa/doble pago, limpia al salir. Prep/Grill/Assembly próximos y pasillo despejado hacia Delivery. Generador reproducible. Sin cambios Domain ni M7. |
| M7 · `feature/food-storage-refrigeration` | Fridge y Freezer físicos, entornos fríos y conservación por temperatura real. | Enfriamiento progresivo a +4/−18 °C y calentamiento al retirar; tasas 1/0.1/0.001 según temperatura real. Edad, ID, FoodState, frescura, contaminación y cocción se conservan. E/G/E usa física existente; único FoodSimulation, integración analítica de exposición y regresiones M1–M6. Sin inventario, puertas funcionales, electricidad, compras ni M8; ver M7.md y ADR 0009. |
| M8 · `feature/economy-procurement` | Compras físicas y loop económico con el saldo M6. | Comprar Bun/Raw Beef Patty/Cheese en céntimos configurables: cargo único, unidad real con ID/FoodState propio y registro en el único FoodSimulation M7. Fondos insuficientes o salida ocupada: no crear/cobrar. Fixtures gratuitas solo desarrollo/testing; saldo de desarrollo configurable y deuda de 0 € documentada. Conservar/cocinar/montar/vender unidades compradas, pago M6 único y reinversión. Tests Domain, PlayMode, escena y regresiones M1–M7; ver M8.md y ADR 0010. Sin otros milestones. |
| M9 · `feature/restaurant-layout-service-flow` | Distribución espacial de restaurante y recorrido del cliente único. | Cocina al norte: Storage → Prep → Grill → Assembly → Delivery, con Procurement física y pase accesibles. Mostrador sólido separa zona pública: Entrance → futura Queue → Counter → Exit, por puertas y ruta sin atravesar cocina. Cuatro QueuePoints pasivos; fixtures de tests conservadas fuera del gameplay normal. Mantener referencias/GUID y sistemas M1–M8, ejecutar regresiones y comprobar manualmente comprar → almacenar → cocinar → montar → entregar → cobrar; ver M9.md y ADR 0011. Sin múltiples clientes ni arte definitivo. |
| M10 · `feature/customer-queue` | Cola física FIFO de cuatro clientes y paciencia de debug. | Entrance → Queue → Service Position → Order/Wait → Receive → Exit. Reservas únicas desde entrada hasta salida; al terminar avanza el resto y admite otro. Solo la cabeza llegada tiene pedido/recibe comida; IDs independientes y pipeline M6 de evaluación/pago único/transporte intacto. Paciencia configurable por unidad, cero sin penalización. Movimiento determinista público sin NavMesh; tests de aforo, avance, salida, relevo, identidad, pago y regresiones M1–M9; ver M10.md y ADR 0012. |
| M11 · `feature/restaurant-day` | Tiempo del mundo y jornada con apertura/cierre. | Día/hora/minuto, horario y velocidad configurables, pausa/resume de desarrollo. Solo Open admite; Closing permite terminar visitas admitidas y finaliza con restaurante vacío. Next Day conserva dinero/comida/estados; único driver FoodSimulation independiente del mundo, sin salto nocturno. HUD mínimo, tests de límites temporales, entrada bloqueada, clientes terminando, dos jornadas consecutivas y regresiones M1–M10; ver M11.md y ADR 0013. |
| M12 · `feature/electricity-utilities` | Suministro general, ON/OFF individual y consumo sin facturas. | E conmuta Grill, Fridge y Freezer individualmente; funcionan con suministro ON y aparato ON. Cortes/restauración conservan selecciones individuales. Sin corriente o con aparato OFF, FoodSimulation aproxima alimento a ambiente y pausa calor/cocción sin recrear estado. Vatios configurables y kWh por aparato/total, sin consumo OFF ni duplicación. Prompts al apuntar, HUD, conservación en Next Day, tests de ocho combinaciones, cortes/restauración, tres aparatos, consumo, identidad y regresiones completas M1–M11; ver M12.md y ADR 0017. Sin facturas, impagos, generadores, averías ni M13. |
| M13 · `feature/bills-operating-costs` | Facturas, costes fijos configurables y resultado diario con el ledger existente. | kWh reales M12 × tarifa, redondeo final determinista en céntimos; ventas/compras reales sin doble cobro; alquiler una vez. Closing termina clientes y Closed liquida exactamente una vez; Next Day exige resumen y no repite cargos. Saldo negativo permitido. Summary conciliado hasta Next Day; consumo eléctrico devengado separado de pagos, período pendiente entre días, herramienta Inspector para pagar exactamente una vez. Ver también ADR 0019. Reset solo diario, históricos/dinero/objetos/estados intactos. Dos días consecutivos, apagado individual/corte, éxito/rechazo/duplicados/overflow e integración/regresiones M1–M12. Ver M13.md y ADR 0018; sin M14. |

## Recorrido de aceptación del slice

M10 añade la comprobación de cola: cuatro reservas únicas, atención exclusiva
de la cabeza, avance al completar Exit y entrada del siguiente sin atravesar
cocina. Ver la secuencia de dos ventas, rechazo y paciencia cero en [M10](M10.md).
M11 añade horario y dos jornadas consecutivas en [M11](M11.md): cerrar admisiones,
terminar clientes y empezar otro día conservando saldo y estados alimentarios.
M13 añade [liquidación y resultado real](M13.md#prueba-manual-de-un-día-completo):
al terminar el último cliente, alquiler se cobra una vez y electricidad queda pendiente; se consulta
Summary y se inicia el siguiente día conservando todo salvo acumuladores diarios.

1. Abrir `PrototypeRestaurant`; iniciar Play y comprobar 10 € de desarrollo
   (o el saldo configurado) y provisiones gratuitas desactivadas. Comprobar electricidad
   OFF y 0 kWh; encender con E en la pared norte para cocinar/enfriar.
2. Leer el pedido del cliente. Comprar ingredientes con E en la estación,
   retirándolos de OUTPUT antes de la siguiente compra. Comprobar el gasto.
   Recoger y soltar una porción; su identidad/estado
   permanece estable y solo existe en una ubicación.
3. Dejar una caja, ingrediente o bandeja sin confirmar en DeliveryPad: se ignora,
   pedido sigue esperando y saldo sin cambios. Entregar un Dish incorrecto o Custom: se
   rechaza, conserva plato, no cobra y el cliente sale; llega otro tras el intervalo.
4. Cocinar una porción real, retirarla Cooked y montar el pedido soltando cada
   ingrediente sobre una pila apoyada en encimera, bandeja o Grill. Mantener clic
   recoge/sostiene; soltar clic libera. E/G siguen disponibles como alternativas.
   Probar por separado
   Burnt, Rotten cocinado y contaminado dentro de un Dish estructuralmente correcto:
   Correct order YES; estado peligroso visible por separado, pago completo.
5. Confirmar F, recoger manteniendo clic, llevar al pad verde y soltar clic. Se detecta
   también en bordes con parte razonable del conjunto sobre el pad; no exige centrar.
   Al reposar, un pedido correcto cobra 5 € o 6,50 € según menú; cliente sale
   llevando el mismo plato y ambos se
   retiran al llegar a salida. Consultas repetidas
   no vuelven a pagar. Retirar un rechazado antes de ofrecerlo a otro cliente.
6. Detener y volver a iniciar Play: saldo de desarrollo configurado y sin
   provisiones gratuitas. No persisten compras ni ventas. Poner saldo inicial 0:
   compras fallan sin crear objetos; la primera provisión sigue pendiente de diseño.
   No cambian los ScriptableObjects.
7. Cambiar solo la geometría/material de un placeholder (por ejemplo esfera por
   cubo en Visual) y repetir una entrega válida; las reglas no cambian.

Automatizar en EditMode los casos de estado/cocción/validación/doble pago. Usar
PlayMode para integración importante y revisión manual para cámara, input,
colliders y feedback. No sustituir este recorrido por tests triviales.
La ubicación vigente y el recorrido de compras/almacenamiento están en [M9](M9.md);
las posiciones de guías anteriores son históricas.

## Después del slice

El orden siguiente es orientativo y se revisará con evidencia del prototipo:

1. **Bucle de supervivencia:** obtención de provisiones con 0 €, balance de precios M8,
   combustible, reposición y desbloqueo comercial del suministro M12. Comprobar que existe
   una vía jugable para realizar la primera venta sin dinero inicial.
2. **Profundidad de cocina:** más ingredientes, factores de deterioro, almacenamiento, herramientas
   físicas y criterios de plato más expresivos, manteniendo creación libre.
3. **Servicio y negocio:** múltiples pedidos, clientes con navegación, reputación,
   balances y empleados. Introducir guardado cuando el progreso lo requiera.
4. **Presión y emergentes:** inspecciones sanitarias, crimen, policía y eventos
   dinámicos que se apoyen en los sistemas existentes.
5. **Presentación:** sustituir placeholders, incorporar arte/animación/audio y
   optimizar a partir de medidas. No adelantar este trabajo al prototipo funcional.

Multijugador, arquitectura distribuida y assets definitivos no son requisitos
del slice. Los paquetes de plantilla disponibles no convierten esas features
en compromisos de implementación.
