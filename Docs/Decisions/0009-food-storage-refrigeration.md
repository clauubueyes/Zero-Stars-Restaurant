# ADR 0009: almacenamiento físico y conservación térmica M7

Fecha: 2026-10-06. Estado: implementada; pendiente de aceptación manual de M7.
Base aprobada: `144cca5`, M1–M6 y polish. Rama `feature/food-storage-refrigeration`.

## Contexto

M3 separa edad y exposición al deterioro; M4 resuelve un ThermalEnvironment por
unidad y avanza temperatura/cocción una sola vez desde FoodSimulation. Su API
permite una tasa externa, pero las escenas anteriores siempre suministraban 1.
M7 debe conservar físicamente comida, incluyendo el calentamiento gradual después
de retirarla, sin sustituir estado, añadir relojes o alterar reglas de pedidos.

## Decisión

- Reutilizar el contrato Runtime HeatSource existente. Aunque su nombre procede
  de M4, ya permite objetivos fríos: ColdStorage aporta aire a +4 °C o −18 °C,
  con multiplicador de transferencia 2, sin permiso de cocción. No crear jerarquías
  separadas para Fridge/Freezer ni renombrar/migrar la fuente M4 y sus referencias.
- ColdStorage consulta la posición actual de cada FoodItem en un BoxCollider
  interior orientado. El origen físico de la unidad debe estar dentro del interior;
  aproximar un alimento por fuera no basta. No mantiene listas OnTriggerEnter/Exit,
  poses guardadas, flags isInFridge ni inventario. Una pieza sostenida realmente
  dentro también intercambia calor, como en M4.
- La consulta por posición de unidad permite conservar los FoodItems originales
  de un Dish finalizado, cuyos colliders individuales M5 están desactivados. No
  consulta Renderer, receta, nombre, DishState o representación para decidir frío.
  El volumen es aire interior, no contacto de precisión con estantes.
- FoodSimulation referencia explícitamente plancha, Fridge y Freezer. Conserva
  deduplicación por unidad y la selección M4: transferencia mayor, empate por
  orden serializado. Solo el entorno elegido se usa en el único Advance de FoodState.
  No se suma energía ni se promedian temperaturas cuando coinciden fuentes.
- FoodPreservationSettings es un ScriptableObject de configuración compartida.
  Crea FoodPreservationProfile inmutable, sin Unity, que relaciona temperatura real
  y tasa relativa de deterioro: **T > 5 °C → 1**, **0 < T ≤ 5 °C → 0.1**,
  **T ≤ 0 °C → 0.001**. Umbrales/tasas configurables y validados; frío nunca recupera
  frescura, limpia contaminación, elimina edad o reinicia dosis de cocción.
- La política se aplica a todas las unidades del reloj, también fuera del gabinete.
  Una pieza caliente recién guardada conserva tasa normal hasta enfriarse; una
  congelada retirada conserva tasa muy baja mientras sigue bajo cero. No depende
  de la temperatura objetivo del contenedor ni de un booleano de almacenamiento.
- FoodState.Advance acepta el perfil opcional. Integra analíticamente el tiempo a
  cada lado de los dos umbrales durante su trayectoria exponencial: a lo sumo dos
  cruces, sin bucle por segundo. Saltos grandes y pasos pequeños equivalen con
  entorno constante; movimientos o cambios de objetivo deben segmentarse.
  El multiplicador externo M3 sigue multiplicando exposición; edad avanza completa.
- Sin perfil, la API conserva exactamente la tasa constante M3/M4. La escena M7
  asigna explícitamente el asset a su único FoodSimulation. El perfil se copia
  una vez por Advance, permitiendo comprobar cambios de configuración en Play;
  no se almacena progreso en el asset ni se reconstruyen FoodState.
- Cabinets de primitivas con suelo/estante, dos paredes, fondo y techo; frente
  abierto, etiquetas de desarrollo con fuente incluida en Unity. E recoge, G
  deposita y E recupera. Sin snap de almacenamiento, tecla nueva o puerta funcional.
  Se conservan las 18 unidades, layout anterior, inputs, GUID y fileIDs existentes.
- Deshabilitar ColdStorage, su interior o poner transferencia a cero retira el
  entorno y permite calentamiento gradual al ambiente. Ese es el punto de
  integración de un futuro suministro eléctrico; no añadir ahora consumo, estado
  powered, cables, compras ni lógica de electricidad. Los dos aparatos activos son
  fixtures de desarrollo, como la plancha del slice.

## Consecuencias y límites

La conservación es un modelo de balance del prototipo. No representa fases del
agua, hielo físico, daño por congelación, humedad, contaminación cruzada, masa,
circulación del aire, capacidad térmica del aparato o puertas abiertas. La etiqueta
indica objetivo nominal de la fixture; la temperatura real del alimento es la del
feedback M3. Los umbrales de tasa son discontinuos y editables.

La colocación es física y conserva todos los límites de PhysicalCarry. El centro
de la unidad dentro del aire interior define almacenamiento; no es la misma regla
de solapamiento tolerante de DeliveryZone. No se exige centrar una pieza en el
estante. Desactivados inicializados siguen envejeciendo según M3, con ambiente
ordinario y conservación según su temperatura real, sin una ocupación persistente.

El reloj sigue usando referencias locales. No registrar una unidad en dos drivers.
Adelantar tiempo manual congela la colocación actual y no simula trayectorias/física.
Electricidad y adquisiciones deberán alimentar estos componentes sin duplicar el
reloj ni copiar estados. La economía básica M6 existente queda intacta; M8 no entra
en esta tarea.
