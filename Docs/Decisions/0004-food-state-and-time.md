# ADR 0004: definición, estado y tiempo de alimentos M3

Fecha: 2026-10-06. Estado: aceptada; M3 validado por el usuario en `307174a`.
M4 amplía térmica/cocción mediante [ADR 0005](0005-thermal-cooking.md).

## Contexto

M2 aprobado (`14e0575`) ya recoge objetos físicos sin conocer comida. M3 necesita
alimentos con deterioro e inspección, preservando independencia entre piezas y
assets y permitiendo avanzar tiempo explícito. No incluye cocción, refrigeración,
platos, inventario, pedidos, economía ni guardado.

## Decisión

- Introducir `ZeroStarRestaurant.Domain` con `noEngineReferences: true`. Contiene
  `FoodProfile` inmutable, `FoodState` por unidad y enums de categoría/condición.
  Runtime, Editor y tests lo referencian por necesidades reales; no hay servicios,
  registro global, DI ni nuevos paquetes.
- `FoodDefinition` es un ScriptableObject de autoría: ID estable, nombre, categoría,
  coste en céntimos, vida de frescura, respuesta térmica y tres umbrales. No contiene
  estado de partida ni mesh/material/collider. IDs de fixture son texto estable
  (`food.raw_beef_patty`, `food.bun`, `food.cheese`); renombrar el asset no los cambia.
  Para nuevas definiciones, asignar un ID único y no reutilizarlo para otro tipo.
- Cada `FoodItem` crea en Awake un `FoodState` nuevo y una instantánea inmutable del
  asset. Editar la definición afecta a instancias nuevas, no a las ya creadas.
  No se recrea estado en OnEnable; recoger, soltar y lanzar conservan su referencia.
- Separar edad cronológica de exposición equivalente al deterioro. Con tasa 1,
  ambas aumentan un segundo por segundo simulado. La frescura decrece linealmente
  de 100 a 0 durante `FreshnessLifetimeSeconds`; exposición se satura en esa vida.
  Edad continúa, con saturación defensiva ante overflow. La frescura no se recupera.
- Condición derivada, sin segundo estado mutable: Fresh si frescura >= umbral
  fresco; Rotten si <= umbral podrido; Spoiled si <= umbral deteriorado; Acceptable
  en el intervalo restante. Inicialmente 80%, 30% y 5%. No son estados de cocción.
- `FoodState.Advance(elapsedSeconds, ambientTemperatureCelsius, deteriorationMultiplier)`
  recibe tiempo y entorno; rechaza valores inválidos antes de mutar. Una tasa 0
  pausa deterioro, sin pausar edad ni temperatura. No consulta Time ni una escena.
- Temperatura en °C por unidad, con aproximación exponencial analítica al ambiente:
  `T = ambient + (Tprevious - ambient) * exp(-seconds / responseSeconds)`.
  Sin overshoot ni bucle por segundo para saltos grandes. `SetTemperature` permite
  a un futuro adaptador modificarla; M3 no contiene fuente de calor ni congelación.
- El multiplicador permite tasas externas futuras. En M3 la temperatura NO cambia
  automáticamente el deterioro: no se inventa una curva biológica. El caller de
  refrigeración/cocina podrá definir esa política y segmentar cambios de entorno.
- Contaminación es un booleano independiente, inicializable y marcable de forma
  idempotente. No hay propagación, detección de suelo ni regla de comestibilidad.
  Un alimento Fresh puede estar contaminado; su evaluación sanitaria vendrá después.
- `FoodSimulation` conecta tiempo Unity a las cuatro referencias explícitas de la
  fixture. Su método Advance también admite tiempo manual. Un multiplicador y un
  botón de Inspector solo de desarrollo aceleran comida sin cambiar Time.timeScale,
  locomoción ni física. Piezas ya inicializadas e inactivas siguen envejeciendo
  mientras el driver permanece activo; destruidas se omiten.
- `FoodInspectionFeedback` presenta estado real del alimento enfocado o sostenido
  debajo del feedback M2. Se mantiene aparte: detector, orquestador, Pickup y portador
  no referencian Food. Materiales y escala se asignan en el generador, no en Domain.
- Ampliar el generador existente preservando sus referencias/GUID, M1 y las cuatro
  cajas M2. Crear tres definiciones, tres materiales de color y cuatro alimentos
  Cube; reutilizar los assets existentes al reconstruir, sin sobrescribirlos.

## Consecuencias para M4 y posteriores

`FoodCondition` describe deterioro; Raw/Cooked/Burned deberán modelar cocción por
separado. Una plancha podrá cambiar temperatura y su propio progreso conservando
la misma unidad, sin convertir a Pickup en un alimento ni mutar el ScriptableObject.
El driver futuro debe tener una única responsabilidad por avanzar cada estado
para evitar contar dos veces el tiempo al añadir calor o mover alimentos.

Platos/consumidores/inspecciones deberán consultar las instancias reales, sin
copiar datos desde el color, nombre o condición visual. El coste es dato de
referencia; no implica compras, ventas o saldo. Categorías serializadas se amplían
sin reordenar valores existentes.

No se implementa persistencia ni identidad de guardado. Se conserva identidad por
referencia en la sesión. Un futuro snapshot deberá conservar definición, edad,
exposición, temperatura y contaminación; reconstruir frescura solo desde edad
perdería el historial de tasas distintas. La API de tiempo permite procesar un
intervalo fuera de escena sin depender de frames, pero M3 no inventa timestamps,
escenas descargadas o políticas de catch-up.

La aproximación térmica y el deterioro lineal son modelos de prototipo, no
simulación biológica ni seguridad alimentaria real. La exposición saturada limita
detalle después de 0% de frescura. No hay recuperación ni efectos del suelo.
