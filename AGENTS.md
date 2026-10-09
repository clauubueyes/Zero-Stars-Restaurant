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
  M10 está validado en `bd8d33c`. M11 añade tiempo del mundo y jornada en
  `feature/restaurant-day`; ver `Docs/M11.md` y ADR 0013.
  M11 está validado en `0c81923`. Polish físico previo a M12 en
  `fix/physical-interaction-polish`; ver `Docs/PHYSICAL-INTERACTION-POLISH.md` y
  ADR 0014: hold/release, montaje sobre apoyos y reposición de bandejas.
  M12 implementa electricidad y consumo desde `ec2ba3e` en
  `feature/electricity-utilities`; ver Docs/M12.md y ADR 0017.
  M12 está validado en `6258bad`. M13 añade facturas y costes diarios en
  `feature/bills-operating-costs`; ver Docs/M13.md y ADR 0018.
  M1–M13 y correcciones de servicio están aprobados en `cf12f9d`.
  M14 implementa completitud de pedidos y pago parcial; ver Docs/M14.md y ADR 0020.
  M14 está validado en `8751d34`. M15 añade causas de calidad, reacción por visita
  y estadísticas acumuladas; ver Docs/M15.md y ADR 0021.
  Código M15 validado en `c0698d9`: 304/304 EditMode y 284/284 PlayMode en
  `C:/Users/Usuario/Zero Stars Restaurant M14`, rama `feature/food-quality-consequences`.
  M16 añade reputación e incidentes diferidos desde M15 `3799710` en
  `feature/reputation-delayed-consequences`; ver Docs/M16.md y ADR 0022.
  M16 está validado en `1609a29`. M17 añade suciedad por superficie y limpieza
  física en `feature/hygiene-cleaning`, proyecto entregado
  `C:/Users/Usuario/Zero Stars Restaurant M15`; ver Docs/M17.md y ADR 0023.
  M17 validado automáticamente: 357/357 EditMode y 309/309 PlayMode;
  aceptación manual de visual/controles pendiente.
  M18 añade contaminación sanitaria por contacto Food ↔ Surface desde M17 `9ee1360`
  en `feature/cross-contamination-food-safety`, en la misma carpeta M15; ver
  Docs/M18.md y ADR 0024. Food → Food queda pendiente por física del agregado.
  M18 validado automáticamente en `8641de3`: 384/384 EditMode y 323/323 PlayMode;
  aceptación visual/de controles humana pendiente. Cooking no sanitiza y Dirt
  sigue independiente; Next Day conserva ambos estados.
  VP1A añade iluminación y ambiente sobre M18 validado `0cdaab3`, en
  `visual/lighting-atmosphere-pass-1`; ver Docs/VP1A.md y ADR 0025. Es presentación:
  las luminarias leen el suministro M12; no cambian reglas, materiales anteriores
  ni transforms existentes. Preservar también los nuevos ajustes manuales del pass.
  El instalador incremental conserva luminarias ya instaladas; no usar Rebuild.
  VP1B/C añade visual shells y materiales PBR propios sobre VP1A `db39056`, en
  `visual/realistic-restaurant-art-pass-1`, por autorización expresa del usuario.
  Ver Docs/VP1BC.md y ADR 0026. Preservar también estos hijos visuales/materiales
  y los nuevos ajustes manuales. No regenerar un pass instalado ni ejecutar Rebuild.
  El polish visual desde VP1B/C `e4fb2fd`, rama `fix/visual-burger-dirt-polish`,
  añade BurgerIngredientVisual al shell y overlays orgánicos en DirtSurfaceView.
  Ver Docs/VISUAL-POLISH-BURGER-DIRT.md y ADR 0027. Conserva también estos visuales
  y sus ajustes manuales: nunca reducir colliders/poses de Food para imitar su apariencia.
  El bugfix desde `b023fe8`, rama `fix/food-visual-placement-regressions`, desacopla
  apoyo visual de las cajas funcionales antes/después de F y sobre COLLECT HERE.
  Ver Docs/FOOD-VISUAL-PLACEMENT-REGRESSIONS.md y ADR 0028. Conservar metadata y
  tuning visual; no introducir decisiones por receta/ID en el placement runtime.
  M19 y los sistemas posteriores no están implementados.
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

## User-authored Unity changes

Los cambios manuales realizados por el usuario en Unity tienen prioridad y **no
deben sobrescribirse ni revertirse** durante milestones, fixes, rebuilds o
regeneraciones.

Esto incluye especialmente:

- distribución/posición/rotación/escala de objetos en escenas;
- cambios manuales en `PrototypeRestaurant`;
- materiales;
- prefabs;
- modelos, texturas y otros assets;
- configuración visual;
- nuevos objetos/assets añadidos manualmente.

Antes de modificar o regenerar una escena/prefab/asset existente, inspecciona el
estado actual y preserva los cambios del usuario.

Los generadores/Editor tools no deben reconstruir automáticamente toda la escena
si eso destruye modificaciones manuales. Prefiere cambios incrementales y por
componentes.

No uses una versión anterior de la escena como fuente de verdad si la versión
actual contiene cambios manuales del usuario.

Si una tarea requiere inevitablemente sobrescribir un cambio manual, **no lo hagas
silenciosamente**: indícalo antes.

## Latest version in the user's Unity

- Antes de empezar y antes de entregar, identificar la carpeta real abierta en el
  Unity del usuario y comprobar su rama, commit y cambios locales frente a la
  última implementación validada. No asumir que el Editor usa el worktree del agente.
- Al terminar un milestone o fix, asegurar que el proyecto que usa el usuario
  contiene la última versión validada y autorizada. Un worktree separado sirve para
  desarrollo/tests, pero por sí solo no actualiza el Unity del usuario.
- Sincronizar código y dependencias de forma revisable y aplicar componentes de
  escena incrementalmente. Preservar siempre los cambios manuales actuales del
  usuario; nunca sustituirlos con una escena, prefab o asset anterior.
- Antes de sincronizar una escena abierta, comprobar cambios sin guardar y Play.
  Si no pueden inspeccionarse o preservarse con seguridad, solicitar que el usuario
  guarde, salga de Play y cierre/reabra el Editor según sea necesario. No cerrar sus
  procesos ni lanzar otro Editor sobre la misma carpeta abierta.
- Después de actualizar, comprobar importación/compilación, referencias y errores
  del proyecto real, además de las pruebas del worktree. Informar de la carpeta y
  versión entregadas y de cualquier recarga o verificación pendiente. No afirmar
  que su Unity está actualizado si solo se actualizó otra carpeta.
- «Última versión» se refiere al juego/proyecto entregado, no a actualizar Unity,
  paquetes o pipeline. No integrar en main ni publicar sin autorización.

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
  Excepción autorizada VP1B/C: geometría modular, meshes modestos de Food/Plate,
  materiales URP/Lit y texturas procedurales locales, sin nuevas dependencias.
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
  y termina la visita. Una colocación rechazada se bloquea solo para ese pedido;
  el siguiente cliente puede evaluar el mismo Dish que siga en el pad y recogerlo
  si coincide con su pedido. Nunca trasladar el rechazo a todas las visitas futuras.
- Polish histórico desde M6 aprobado `9335899`: clic izquierdo PlaceIngredient coloca la
  Food sostenida sobre la pila con bounds, sin clones ni cambios de estado. Snap
  solo pertenece a AssemblySurface; E/G y Throw derecho siguen siendo genéricos.
  Finalizar usa un proxy único; el agarre elevado configurable evita depender de
  qué ingrediente se enfoca. Ver Docs/VERTICAL-SLICE-POLISH.md y ADR 0008.
- El polish físico desde `0c81923` sustituye ese clic por hold/release exclusivo
  de Pickup; E conserva acciones contextuales y F confirma. Las APIs de snap/Throw
  se conservan; Throw queda para desarrollo. La colocación asistida actúa al soltar clic con
  mano tranquila mirando apoyo/pila cercana; gesto rápido y G liberan libremente.
  El press solo agarra y F confirma. PhysicalDishAssembly
  reconoce una pila conectada con apoyo físico fuera de AssemblySurface, conserva
  estados originales y reutiliza DishItem/M6. DishTraySupply repone un prefab vacío
  solo con salida libre; nunca clonar comida vendida. FoodItem conserva geometría
  local antes de retirar colliders y Grill consulta cada ingrediente, sin otro driver.
  Ver Docs/PHYSICAL-INTERACTION-POLISH.md y ADR 0014. M12 está autorizado aparte.
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
  GenerateScene rechaza reemplazar una escena existente. Usar instaladores incrementales; conservar cambios manuales, objetos, transforms, materiales y referencias.
  Evitar service locators, singletons globales, buses de eventos generales,
  contenedores DI, jerarquías y abstracciones para sistemas que aún no existen.
- Crear interfaces y assemblies solo cuando una dependencia real lo necesite.
  No añadir código NUnit a las carpetas reservadas sin su assembly de tests.
- M11: GameTime/RestaurantDay son Domain sin Unity. RestaurantDayController
  avanza únicamente el reloj del mundo antes del servicio; velocidad/pausa del
  mundo no afectan FoodSimulation, clientes ni Time.timeScale. FoodSimulation
  conserva su único Update/driver y tiempo de simulación M3–M10: nunca avanzar
  FoodState desde la jornada ni aplicar velocidad del mundo a alimentos.
  Solo Open admite; Closing conserva clientes/pedidos hasta Exit, y ocupación
  cero termina el día. Next Day cambia número/hora y delay de admisión, sin
  recargar/resetear dinero, comida, platos, recibos o contador de clientes.
  No simular una noche, calendario, facturas, sueño o guardado sin autorización.
- M12: RestaurantElectricity es el único driver de consumo, con segundos de
  simulación independientes del mundo/comida de debug. ElectricitySupplyState y
  ElectricityMeter son Domain; potencia nominal W, acumulados kWh por aparato y
  total, sin otro ledger. HeatSource requiere ElectricalAppliance explícito en
  escena; OFF retira su entorno y FoodSimulation resuelve ambiente sin tocar
  FoodState. ON/OFF, disable/enable y Next Day no reinician energía histórica ni alimentos.
  Potencia configurable antes de Play; consumo continuo vacío si fuente operativa.
  Fixtures térmicas aisladas sin requisito mantienen comportamiento histórico;
  requisito sin referencia falla apagado. M13 añade devengo eléctrico y liquidación diaria de costes fijos;
  no implementar impagos, generadores ni averías. Ver Docs/M12.md y ADR 0017.
  Cada aparato tiene ElectricalApplianceState ON/OFF y ApplianceSwitch con E;
  Funciona = suministro general ON && aparato ON, con validaciones térmicas
  existentes. Cortes/restauración y Next Day conservan selección y medidor.
  Inicialmente suministro OFF y aparatos ON; selección configurable antes de Play.
- M13/corrección: PaymentLedger es el único saldo, con transacciones por día y
  snapshots DailySummary inmutables. Closed cobra solo costes fijos una vez tras
  vaciarse la cola; Next Day exige resumen. Electricidad diaria devengada y período
  pendiente salen del mismo ElectricitySupplyState; no cobrar en Next Day ni sumar
  redondeos diarios. La factura de desarrollo paga un período exactamente una vez,
  conserva recibo y resetea solo pendiente; permite saldo negativo. Separar neto
  operativo de caja y factura pagada. Un pago tras Closed reproyecta evidencia real
  en un nuevo snapshot sin volver a liquidar alquiler. Preservar diarios/histórico,
  IDs, dinero, alimentos, platos, estado y selección ON/OFF. Solo Next Day resetea
  diarios. Sin calendario mensual ni deuda avanzada. Ver ADR 0019.
- M14 sustituye la aceptación binaria M6: DeliveryContents referencia Dish y/o
  unidades originales sin requerir finalización para comida suelta. OrderSatisfaction
  compara cantidades por ID (Bun repetido exige unidades distintas), separado de
  receta reconocida y de toda calidad/seguridad. Pesos positivos por posición de
  receta, iguales para IDs repetidos; pago floor(precio máximo × peso esperado
  recibido / peso esperado total), multiplicando antes de dividir. Extras no pagan.
  Algún ingrediente esperado permite aceptar; ninguno rechaza y conserva objetos.
  Registrar céntimos reales y todos los IDs en el único ledger una sola vez;
  FoodState.IsSold impide recomponer/revender. El carrier transporta exactamente
  Dish/FoodItem/Plate originales hasta Exit, donde desregistra y limpia. Preservar
  evidencia inmutable de temperatura/edad/frescura/deterioro/cocción/contaminación.
  Montar fuera de PASS: se resuelve una entrega, sin rondas de completado posteriores.
  No implementar reputación, intoxicación ni inspección. M15 está autorizado aparte. Ver ADR 0020.
- M15 evalúa el mismo snapshot inmutable de M14, con causas por unidad, sin score
  único ni cambios de pago. CustomerVisit.Resolve crea una consecuencia solo tras
  aceptar/comer provisionalmente en Pay. Statistics del servicio deduplica visita
  y pedido; conserva evidencia tras Exit, cancelación y Next Day. Raw/Undercooked
  de carne cocinable, Spoiled/Rotten y Contaminated producen Health Incident;
  Burnt Complaint; Acceptable/Overcooked Unhappy; resto Good/Satisfied. Prioridad
  sanitaria > queja > descontento > satisfacción, causas completas y contadores
  exclusivos. Temperatura sigue como hecho. Sin enfermedad diferida, reputación,
  inspección, devolución ni descuento por calidad. M16 está autorizado aparte. Ver ADR 0021.
- M16 consume CustomerConsequence original, sin sustituir estadísticas M15 ni
  modificar pagos M14. RestaurantReputationState conserva rango, eventos, riesgos
  y resoluciones por visita/pedido; no reiniciar al salir, cancelar o cambiar día.
  Riesgo al servir; plazo solo tras Exit (o cancelación de una visita ya consumida).
  Satisfied +1, Unhappy -1, Complaint -3 al finalizar; Health Incident sin penalización
  inmediata, confirmado -8, todo configurable. Probabilidad máxima de causas reales:
  carne Raw/Undercooked .35, Spoiled/Rotten .55, Contaminated .75; una tirada por visita.
  GameTime.ElapsedWorldSeconds acumula solo avance efectivo, no noche ni Next Day;
  RestaurantDayController llama ResolveDue, sin otro Update/reloj. Retraso 7200s.
  RNG inyectable en Domain y seed de sesión en Runtime, evidencia inmutable y
  resolución una vez. Herramientas de desarrollo adelantan/confirmar/descartar solo
  riesgos de clientes que ya salieron. Feedback en HUD central con scroll existente.
  Persistencia de sesión entre días, sin guardado en disco. No estrellas, reputación
  sobre clientes/precios, inspecciones, multas, enfermedades concretas ni higiene.
  Ver Docs/M16.md y ADR 0022.
- M17: CleanableSurface posee DirtState independiente de sesión (0–1); umbrales
  en HygieneSettings, tipos/origen/Changed consultables. FoodSimulation notifica
  exclusivamente la dosis real de cocción ganada a la fuente seleccionada, sin
  otro driver de comida. SurfaceFoodContact registra entrada por unidad, no dirt
  por frame de apoyo. CleaningTool reutiliza Pickup/PhysicalCarry; E mantenida
  limpia solo la superficie apuntada y próxima con tiempo explícito. DirtSurfaceView
  modifica únicamente manchas nuevas sin colliders, nunca materiales originales.
  Prompt en InteractionFeedback/HUD único. Next Day no limpia ni recrea estado.
  Higiene no cambia FoodState, completitud/pago M14, calidad/reacción M15,
  reputación/probabilidad M16. Instalador incremental/idempotente; no reconstruir
  la escena actual ni restaurar valores manuales de higiene. Sin save/load,
  inspecciones, contaminación por contacto ni M18. Ver Docs/M17.md y ADR 0023.
- DeliveryZone es la única puerta espacial: todas las APIs de servicio deben pasar
  por apoyo/overlap del pad verde actual, sin posición/orientación/input del jugador.
  El carrier recibe el mismo Dish con o sin Plate, bloquea todo Pickup/colisión y
  conserva originales hasta Exit. LateUpdate solo sincroniza representación; no
  avanza simulación. DebugHudPresenter es el único OnGUI; feedback produce texto y
  DebugHudLayout asigna regiones disjuntas con clipping/scroll.
- Convenciones: C# y nombres técnicos en inglés; documentación en español;
  namespace `ZeroStarRestaurant`, con subnamespaces según responsabilidad;
  PascalCase para tipos/métodos, `_camelCase` para campos privados.
- Documentar decisiones relevantes en `Docs/Decisions` y mantener el roadmap y
  las instrucciones de comprobación alineados con lo realmente implementado.

## Verificación y entrega

- M18: ContaminationState y DirtState son independientes. FoodSafetyPolicy emite
  riesgo Raw/Undercooked Meat y contaminación existente por entrada física M17;
  máximo por categoría, procedencia dominante y último donante, sin RNG ni suma
  por frame. La superficie previa se captura antes de transferir entradas simultáneas.
  FoodItem conserva geometría individual al retirar colliders: no atribuir el proxy
  Dish completo a cada ingrediente. IsContaminated es la API normal M15; snapshots
  guardan trazas inmutables y M16 conserva sus responsabilidades. CleaningTool solo
  limpia Dirt, sanitización Inspector es explícita/configurable, cooking no elimina
  contaminación y Next Day conserva ambos estados. Un contacto continuo exige
  salida/reentrada para transferir cambios nuevos. Sin Food → Food, otro driver,
  HUD nuevo, patógenos concretos, kill temperatures, save/load ni M19. Instalador
  incremental preserva escena actual y solo añade referencias/componentes faltantes.

- Validar cada milestone con sus criterios de `Docs/ROADMAP.md`.
- Probar reglas con casos de éxito y rechazo en EditMode cuando existan. Usar
  PlayMode y comprobaciones manuales para integración, física y presentación.
- No añadir tests vacíos ni tests que solo repitan la implementación.
- Revisar errores de Console/importación, referencias y GUID, y el diff final.
  No afirmar que se han pasado pruebas o compilaciones que no se ejecutaron.
- Al terminar, indicar qué cambió, exactamente qué archivos se crearon o
  modificaron, rama/commits, validación realizada, límites y pasos para
  comprobarlo desde Unity. No ocultar comprobaciones pendientes.
