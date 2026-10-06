# Vertical slice y roadmap

Estado actual: **M0, M1 y M2 aprobados; M3 implementado, aceptación manual pendiente**.
Ver [M1](M1.md), [M2](M2.md) y [M3 y su validación](M3.md). M4–M8 siguen pendientes.

## Experiencia objetivo

**Player → Interaction → Food → Cooking → Dish Assembly → Customer Order → Delivery → Payment**

En una sala pequeña hecha de primitivas, el jugador recoge una porción, la coloca
en una estación de calor, la retira cocinada, la añade a un plato, consulta un
pedido, lo entrega a un cliente estático y recibe dinero. El flujo ocurre desde
primera persona y da feedback visible para cada acción o rechazo.

## Alcance mínimo y decisiones de prueba

- Un jugador, teclado/ratón, una sala, una estación, un plato, un cliente
  placeholder y un pedido activo. Sin pathfinding ni turnos de empleados.
- Un tipo de ingrediente basta para demostrar identidad y estado; cada porción
  tiene estado propio. M3 incluye deterioro básico Fresh/Acceptable/Spoiled/Rotten,
  independiente de los estados de cocción previstos para M4: Raw, Cooked, Burned.
  Factores de deterioro más detallados llegarán después; no confundir los dos ejes.
- Cocción por tiempo acumulado con una fuente activa, no por simulación térmica.
  La cocina física inicial valida colocar/retirar objetos; no implementa todavía
  fluidos, cortes, transferencia de calor o contactos de precisión.
- Montaje libre por contenido: un plato admite porciones que el jugador deposita;
  no exige una secuencia de receta, slots exactos ni una malla concreta. Para este
  pedido basta contener una porción cocinada del tipo solicitado y que todas las
  porciones del plato sean comestibles. Raw y Burned se rechazan.
- Pedido fijo de desarrollo, con importe configurable; valor inicial de prueba
  **500 céntimos**. Entrega válida cobra una vez; los rechazos no consumen el plato
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
| M4 · `feature/basic-cooking` | Estación no eléctrica para una porción y progreso de cocción. | Raw → Cooked → Burned en umbrales configurados; retirar pausa el calor acumulado; volver a colocar continúa sin resetear; no cocina lejos de la estación. Tests con tiempo explícito antes/en/después del umbral y comprobación PlayMode de colocación. |
| M5 · `feature/dish-assembly` | Plato con colección de porciones y montaje por interacción. | Una porción pasa de mano/estación al plato sin duplicarse; retirar conserva estado; plato vacío no entregable. Montar dos porciones en distinto orden conserva los mismos datos. Tests de pertenencia y composición. |
| M6 · `feature/customer-order` | Cliente estático y un pedido legible con requisito e importe. | Consultar pedido desde primera persona; validar por datos: vacío/Raw/Burned fallan y contenido comestible que satisface el pedido pasa. No requiere navegación ni IA. Tests de predicado de aceptación. |
| M7 · `feature/delivery-payment` | Entrega, cierre del pedido y saldo visible. | Inicio 0; entrega válida suma 500 céntimos, consume la entrega y cierra el pedido; volver a pulsar no duplica pago. Rechazo conserva plato/pedido/saldo. Test de entrega/pago y reentrada o doble solicitud. |
| M8 · `fix/vertical-slice-integration` | Ciclo completo y revisión de fallos, sin sistemas nuevos. | Ejecutar el recorrido de abajo, repetir desde una nueva sesión y comprobar que las vistas son sustituibles. Console sin errores propios; build de desarrollo local si están instalados sus módulos. |

## Recorrido de aceptación del slice

1. Abrir `PrototypeRestaurant` cuando exista; iniciar Play y comprobar 0 € y la
   condición de suministro desconectado. No debe hacer falta un sistema eléctrico
   completo para comprobar esta condición de la fixture.
2. Leer el pedido del cliente. Recoger y soltar una porción; su identidad/estado
   permanece estable y solo existe en una ubicación.
3. Intentar entregar un plato vacío o con porción cruda: feedback de rechazo,
   pedido activo y saldo 0. El plato y su contenido permanecen disponibles.
4. Cocinar una porción en el hornillo; retirarla al estado Cooked y montarla en el
   plato. En una porción distinta, esperar hasta Burned y comprobar rechazo.
5. Entregar el plato correcto: pedido finalizado y saldo 5 €. Repetir la acción
   inmediatamente varias veces; el saldo sigue siendo 5 €.
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
