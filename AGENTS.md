# Zero Star Restaurant: instrucciones para agentes

## Contexto y alcance

- Videojuego Unity 3D en primera persona: simulador sandbox de restaurante.
- Inicio de partida: 0 €, local prácticamente vacío, sin electricidad.
- Sistemas futuros: cocina física, ingredientes con estado y deterioro, creación
  libre de platos, clientes, economía, reputación, electricidad, inspecciones
  sanitarias, crimen, policía, empleados y eventos dinámicos.
- Primer vertical slice:
  `Player → Interaction → Food → Cooking → Dish Assembly → Customer Order → Delivery → Payment`.
- M0–M5 y la corrección de feedback están aprobados (`6a3ca72`).
  M1 añade jugador FPS y escena greybox; ver `Docs/M1.md`.
  M2 añade interacción genérica y Pickup físico; ver `Docs/M2.md` y ADR 0003.
  M3 añade alimentos, estado independiente, deterioro y temperatura; ver `Docs/M3.md`
  y ADR 0004. M4 añade plancha física y cocción térmica independiente; ver `Docs/M4.md`
  y ADR 0005. M5 añade composición libre, reconocimiento y plato agregado; ver
  `Docs/M5.md` y ADR 0006. M6 implementa un cliente activo, pedido, entrega física,
  evaluación independiente de calidad y pago; ver `Docs/M6.md` y ADR 0007.
  M1–M6, polish y entrega tolerante están aprobados en `144cca5`.
  M7 añade almacenamiento físico y conservación térmica; ver `Docs/M7.md` y ADR 0009.
  M7 está validado en `8872f0d`. M8 implementa economía y compras físicas en
  `feature/economy-procurement`; ver `Docs/M8.md` y ADR 0010.
  M8 está validado en `c12d408`. M9 reorganiza el greybox y el recorrido del
  cliente único en `feature/restaurant-layout-service-flow`; ver `Docs/M9.md`
  y ADR 0011. M9 está validado en `aa146c3`. M10 implementa cuatro reservas FIFO
  físicas (incluida atención) en `feature/customer-queue`; ver `Docs/M10.md` y
  ADR 0012. Solo Service Position tiene pedido; paciencia cero es debug.
  Los sistemas posteriores no están implementados.
  Implementar el siguiente milestone solo cuando forme parte de la tarea
  solicitada, sin anticipar todos los sistemas del roadmap.

## Inspeccionar antes de editar

- Leer `README.md` y los documentos relevantes de `Docs`.
- Comprobar `git status --short --branch`, historial y cambios locales. Conservar
  trabajo ajeno; no sobrescribir, descartar, limpiar ni borrar sin justificarlo.
- Verificar la versión en `ProjectSettings/ProjectVersion.txt`, las dependencias
  en `Packages/manifest.json` y `Packages/packages-lock.json`, y el pipeline en
  Graphics/Quality Settings. No asumir versiones a partir de esta documentación.
- Baseline inspeccionado: Unity 6000.5.3f1, URP 17.5.0, Input System 1.19.0 y
  Test Framework 1.7.0. Estos valores describen la preparación inicial.
- Antes de modificar código, escenas, prefabs o configuración importante, leer
  lo existente y comprobar sus referencias y usos.
- Si Unity tiene abierto el proyecto, no lanzar otro Editor sobre esa misma
  carpeta ni cerrar procesos del usuario. Indicar qué comprobación queda pendiente.

## Git disciplinado

- Mantener `main` estable. Para trabajo nuevo crear ramas `feature/...`,
  `fix/...`, `refactor/...`, `docs/...` o `chore/...`, según el alcance.
- Hacer commits pequeños, coherentes y descriptivos. No mezclar features
  independientes. Formato recomendado: `tipo: descripción concreta`.
- Revisar el diff y el contenido del índice antes de cada commit; añadir rutas
  explícitas cuando haya trabajo previo del usuario.
- Versionar `Assets` junto con sus `.meta`, `Packages` y `ProjectSettings`.
  Nunca versionar `Library`, `Temp`, `Logs`, `obj`, `UserSettings`, builds,
  soluciones/proyectos del IDE ni otros archivos generados.
- Conservar Force Text y Visible Meta Files. Al mover un asset, mover también su
  `.meta`, preferentemente desde Unity. No regenerar GUID existentes.
- No hacer `reset --hard`, `clean`, force push ni reescribir historial ajeno.
  No integrar en main ni publicar cambios salvo que la tarea lo autorice.
- La única creación inicial en main fue el commit del proyecto preexistente y sus
  exclusiones; toda preparación posterior se hace en una rama.

## Prototipo y arquitectura

- Usar primitivas Unity, materiales simples y placeholders. En esta etapa no
  crear modelos 3D personalizados, usar Blender, añadir texturas, animaciones,
  shaders complejos, arte definitivo ni assets externos innecesarios.
- No añadir ni actualizar dependencias sin una razón clara y documentada. No
  retirar paquetes de la plantilla de forma incidental.
- Contenido propio bajo `Assets/_Project`. Preservar por ahora la escena, input,
  ajustes URP y tutorial de la plantilla en sus ubicaciones originales.
- M2 amplía el asset de input existente: Interact por pulsación, Drop y Throw.
  Preservar sus GUID y bindings de M1/UI. Detección, input, interacción y agarre
  físico son componentes separados en `Scripts/Runtime/Interaction`.
- Separar estado/reglas de representación. `Scripts/Domain` contiene C# sin
  UnityEngine/UnityEditor; `Scripts/Runtime` adapta input, física, componentes,
  vistas y composición. `Editor` solo contiene herramientas del Editor.
- ScriptableObjects para configuración compartida; estado mutable de partida en
  instancias independientes. No guardar progreso en assets compartidos.
  FoodSimulation es el único driver de alimentos. HeatSource solo describe entorno;
  no añadir otro Update que avance edad, temperatura o cocción. FoodCondition y
  CookingStage son ejes separados. M4 pausa dosis fuera de la fuente, conserva
  identidad y no elimina contaminación; respetar ADR 0004/0005 al preparar M5.
- FoodDefinition crea perfiles inmutables; FoodItem posee FoodState por unidad.
  Pickup no conoce comida. FoodCondition describe deterioro, no cocción. Domain
  recibe tiempo explícito; evitar avanzar una unidad dos veces desde drivers distintos.
  Preservar IDs de definiciones y no reordenar valores de categoría serializados.
  FoodState.InstanceId identifica unidades concretas, separado de definición/Unity.
  DishState contiene referencias originales, con pertenencia exclusiva y orden.
  Confirmar bloquea composición pero conserva estado vivo; el agregado físico no
  recrea alimentos. Reconocimiento no implica comestibilidad. Respetar ADR 0006.
- M6: pedido correcto compara ID de DishDefinition; calidad/cocción no bloquean
  aceptación ni pago. Precios de configuración en céntimos enteros; saldo de sesión
  empieza en cero. OrderDelivery cierra/paga una vez, DishState.IsSold evita reventa.
  Evaluación conserva evidencia inmutable de entrega con IDs originales; el plato
  vivo M5 mantiene sus referencias incluso después de la venta hasta la salida.
  CustomerDishCarrier vincula el mismo Dish a un anchor del cliente, retira Pickup
  y colisiones; desregistrar del único FoodSimulation y destruir solo al llegar a
  salida (o cancelar servicio). Rechazo conserva el plato
  y termina la visita; retirar/recoger antes de ofrecerlo al siguiente cliente.
- Polish desde M6 aprobado `9335899`: clic izquierdo PlaceIngredient coloca la
  Food sostenida sobre la pila con bounds, sin clones ni cambios de estado. Snap
  solo pertenece a AssemblySurface; E/G y Throw derecho siguen siendo genéricos.
  Finalizar usa un proxy único; el agarre elevado configurable evita depender de
  qué ingrediente se enfoca. Ver Docs/VERTICAL-SLICE-POLISH.md y ADR 0008.
  M7 reutiliza HeatSource con ColdStorage; FoodSimulation sigue siendo el único reloj.
  FoodPreservationSettings crea una política inmutable por temperatura real, también
  fuera del almacenamiento. FoodState integra cruces de umbrales; nunca usar flags
  isInFridge/isFrozen para deterioro, recrear estado o restaurar frescura/contaminación.
  La API sin perfil conserva tasa M3; asignar explícitamente el perfil de conservación
  al reloj de escenas con almacenamiento. Ver Docs/M7.md y ADR 0009.
- M8 extiende el mismo PaymentLedger M6 con TrySpend en céntimos e IDs de compra/unidad,
  sin duplicar balances. IngredientProduct configura precio/prefab inactivo;
  la estación prepara FoodState propio, registra en el único FoodSimulation,
  cobra una vez y activa la unidad. Fondos insuficientes o salida ocupada no
  crean objetos ni mutan saldo. TryInitialize no restaura estado existente.
  DevelopmentIngredientSupply desactiva/desregistra toda provisión gratuita por
  defecto; solo habilitarla explícitamente para desarrollo/testing.
  Saldo de código 0; escena configura 1000 céntimos de desarrollo. La primera
  provisión con 0 € es deuda de diseño, sin mecánica inventada. No implementar
  electricidad, supermercado, inventario, robo, préstamos ni otro milestone
  sin autorización expresa. Ver Docs/M8.md y ADR 0010.
- Referencias explícitas en Inspector o por inicialización; componentes pequeños.
- M10: CustomerQueueState reserva plazas antes de entrar y libera la cabeza solo
  al completar Exit. Un único CustomerServiceLoop avanza clientes en pasos fijos;
  no añadir Update a la cola. El cliente tiene ID propio desde admisión y
  CustomerVisit conserva ese ID; OrderState tiene otro ID. Solo la cabeza llegada
  a Service Position recibe/paga por M6. Conservar rutas públicas M9, reservas
  exclusivas, plato original hasta Exit y limpieza al cancelar sin resetear ledger.
  Patience cuenta espera detenida/Order/Wait, se pausa andando y en resultado/salida;
  cero no abandona ni penaliza. No ampliar a pedidos simultáneos, empleados,
  reputación o navegación sin autorización.
- M9 conserva los objetos/referencias M1–M8: cocina al norte del mostrador
  continuo, clientes al sur, entrada oeste y salida este. CustomerMovement admite
  departurePath explícito y conserva el retorno M6 si está vacío. QueuePoints
  contiene cuatro marcadores sin scripts; M10 los referencia desde su controlador.
  Cajas/obstáculos de tests permanecen inactivos y recuperables; el opt-in de
  DevelopmentIngredientSupply activa también el soporte de fixtures apartado.
  Rebuild aplica M9 y M10; sus instaladores adaptan una escena cerrada conservando objetos.
  Evitar service locators, singletons globales, buses de eventos generales,
  contenedores DI, jerarquías y abstracciones para sistemas que aún no existen.
- Crear interfaces y assemblies solo cuando una dependencia real lo necesite.
  No añadir código NUnit a las carpetas reservadas sin su assembly de tests.
- Convenciones: C# y nombres técnicos en inglés; documentación en español;
  namespace `ZeroStarRestaurant`, con subnamespaces según responsabilidad;
  PascalCase para tipos/métodos, `_camelCase` para campos privados.
- Documentar decisiones relevantes en `Docs/Decisions` y mantener el roadmap y
  las instrucciones de comprobación alineados con lo realmente implementado.

## Verificación y entrega

- Validar cada milestone con sus criterios de `Docs/ROADMAP.md`.
- Probar reglas con casos de éxito y rechazo en EditMode cuando existan. Usar
  PlayMode y comprobaciones manuales para integración, física y presentación.
- No añadir tests vacíos ni tests que solo repitan la implementación.
- Revisar errores de Console/importación, referencias y GUID, y el diff final.
  No afirmar que se han pasado pruebas o compilaciones que no se ejecutaron.
- Al terminar, indicar qué cambió, exactamente qué archivos se crearon o
  modificaron, rama/commits, validación realizada, límites y pasos para
  comprobarlo desde Unity. No ocultar comprobaciones pendientes.
