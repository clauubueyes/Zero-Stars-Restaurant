# Vertical slice y roadmap

Estado actual: **M0–M6 aprobados por el usuario; M6 aprobado en `9335899`**.
El [polish del slice](VERTICAL-SLICE-POLISH.md) mejora snap, agarre, transporte de
ventas y greybox en `fix/vertical-slice-polish`; su comprobación manual está pendiente.
Ver [M1](M1.md), [M2](M2.md), [M3](M3.md), [M4](M4.md), [M5](M5.md) y [M6](M6.md).
La petición aprobada de M6 amplía su alcance a Customer Order → Delivery → Payment
y sustituye la división anterior M6 pedido/M7 entrega-pago. También sustituye el
rechazo por Raw/Burnt/calidad: ahora calidad y corrección se informan separadamente,
y solo la coincidencia estructural controla aceptación/pago. Ver ADR 0007.
M7 no se ha implementado ni autorizado; su alcance deberá acordarse después de M6.

## Experiencia objetivo

**Player → Interaction → Food → Cooking → Dish Assembly → Customer Order → Delivery → Payment**

En una sala pequeña hecha de primitivas, el jugador recoge una porción, la coloca
en una estación de calor, la retira cocinada, la añade a un plato, consulta un
pedido, lo entrega a un cliente de primitivas y recibe dinero. El flujo ocurre desde
primera persona y da feedback visible para cada acción o rechazo.

## Alcance mínimo y decisiones de prueba

- Un jugador, teclado/ratón, una sala, una estación, un plato, un cliente
  placeholder y un pedido activo. Sin pathfinding ni turnos de empleados.
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

La sesión comienza siempre con saldo 0 y suministro eléctrico desconectado.
Para no bloquear la primera venta, la escena de desarrollo incluye una pequeña
provisión gratuita, un plato y una fuente de calor **no eléctrica** (por ejemplo,
hornillo de gas placeholder). Son una fixture provisional para probar el ciclo,
no una compra, un generador ni una implementación del sistema eléctrico. La fuente
de calor se representa con geometría simple, sin fuego animado ni shaders.
La luz ambiental de desarrollo permite ver el local y no representa suministro
eléctrico del negocio. En una partida nueva se restaura la fixture y el saldo cero.

La obtención real de ingredientes/equipamiento, el combustible y su coste son un
milestone posterior. Esta elección permite comprobar el slice con el estado
inicial del concepto sin regalar dinero o conectar electricidad de forma implícita.

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
| M7 · por acordar | Trabajo posterior exclusivamente con nueva autorización. | Entrega/pago básicos están en M6. No se anticipan reputación, seguridad alimentaria, colas, stock, guardado u otros sistemas. |
| M8 · `fix/vertical-slice-integration` | Ciclo completo y revisión de fallos, sin sistemas nuevos. | Ejecutar el recorrido de abajo, repetir desde una nueva sesión y comprobar que las vistas son sustituibles. Console sin errores propios; build de desarrollo local si están instalados sus módulos. |

## Recorrido de aceptación del slice

1. Abrir `PrototypeRestaurant` cuando exista; iniciar Play y comprobar 0 € y la
   condición de suministro desconectado. No debe hacer falta un sistema eléctrico
   completo para comprobar esta condición de la fixture.
2. Leer el pedido del cliente. Recoger y soltar una porción; su identidad/estado
   permanece estable y solo existe en una ubicación.
3. Dejar una caja, ingrediente o bandeja sin confirmar en DeliveryPad: se ignora,
   pedido sigue esperando y saldo cero. Entregar un Dish incorrecto o Custom: se
   rechaza, conserva plato, no cobra y el cliente sale; llega otro tras el intervalo.
4. Cocinar una porción real, retirarla Cooked y montar el pedido con clic izquierdo
   mirando bandeja/pila mientras se sostiene cada ingrediente. E/G siguen disponibles.
   Probar por separado
   Burnt, Rotten cocinado y contaminado dentro de un Dish estructuralmente correcto:
   Correct order YES; estado peligroso visible por separado, pago completo.
5. Confirmar F, recoger E, llevar físicamente al pad verde y depositar G. Correcto
   cobra 5 € o 6,50 € según menú; cliente sale llevando el mismo plato y ambos se
   retiran al llegar a salida. Consultas repetidas
   no vuelven a pagar. Retirar un rechazado antes de ofrecerlo a otro cliente.
6. Detener y volver a iniciar Play: nueva sesión con 0 €, pedido y provisiones
   restaurados, sin cambios persistentes en los ScriptableObjects.
7. Cambiar solo la geometría/material de un placeholder (por ejemplo esfera por
   cubo en Visual) y repetir una entrega válida; las reglas no cambian.

Automatizar en EditMode los casos de estado/cocción/validación/doble pago. Usar
PlayMode para integración importante y revisión manual para cámara, input,
colliders y feedback. No sustituir este recorrido por tests triviales.

## Después del slice

El orden siguiente es orientativo y se revisará con evidencia del prototipo:

1. **Bucle de supervivencia:** obtención de provisiones con 0 €, precios de compra,
   combustible, reposición y sistema mínimo de electricidad. Comprobar que existe
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
