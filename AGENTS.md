# Zero Star Restaurant: instrucciones para agentes

## Contexto y alcance

- Videojuego Unity 3D en primera persona: simulador sandbox de restaurante.
- Inicio de partida: 0 €, local prácticamente vacío, sin electricidad.
- Sistemas futuros: cocina física, ingredientes con estado y deterioro, creación
  libre de platos, clientes, economía, reputación, electricidad, inspecciones
  sanitarias, crimen, policía, empleados y eventos dinámicos.
- Primer vertical slice:
  `Player → Interaction → Food → Cooking → Dish Assembly → Customer Order → Delivery → Payment`.
- M0–M4 están aprobados. M1 añade jugador FPS y escena greybox; ver `Docs/M1.md`.
  M2 añade interacción genérica y Pickup físico; ver `Docs/M2.md` y ADR 0003.
  M3 añade alimentos, estado independiente, deterioro y temperatura; ver `Docs/M3.md`
  y ADR 0004. M4 añade plancha física y cocción térmica independiente; ver `Docs/M4.md`
  y ADR 0005. M5 añade composición libre, reconocimiento y plato agregado; ver
  `Docs/M5.md` y ADR 0006. M6 y los sistemas posteriores no están implementados.
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
- Referencias explícitas en Inspector o por inicialización; componentes pequeños.
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
