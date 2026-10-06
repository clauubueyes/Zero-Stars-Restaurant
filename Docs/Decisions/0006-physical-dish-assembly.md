# ADR 0006: composición física, identidad y plato agregado M5

Fecha: 2026-10-06. Estado: implementada; aceptación manual pendiente.

## Contexto

M4 aprobado (`d7eb2bf`) conserva cada FoodItem/FoodState durante pickup y cocción.
M5 necesita platos libres con ingredientes concretos, reconocimiento configurable
y transporte estable. No incluye consumidores, pedidos, entrega, pagos o guardado.
La petición amplía el plan inicial de montaje: orden aproximado de pila y plato
finalizado manipulable como unidad, sin destruir ingredientes para generar otro prefab.

## Decisión

- `FoodState.InstanceId` es un Guid independiente del ID de FoodDefinition y del
  InstanceID de Unity. Se crea una vez por unidad; constructor admite identidad
  explícita no vacía para una futura reconstrucción. No se escribe en el asset,
  y se conserva en desactivación/recogida/cocción/montaje. No hay Save System ni
  registro global: quien reconstruya unidades deberá verificar unicidad global.
- `DishState` en Domain contiene una colección ordenada de las referencias
  FoodState originales, publicada como lectura. No copia frescura, temperatura,
  contaminación, cocción o coste. Consultas agregadas leen datos actuales:
  mínimo/media de frescura (null si vacío), contaminación, Spoiled/Rotten y coste
  total en céntimos long. Estados/temperaturas individuales se consultan en Components.
- El plato reserva un token Guid privado al crear el borrador y lo publica como
  InstanceId al finalizar. Cada unidad tiene una reclamación de montaje Domain;
  TryAdd rechaza duplicados por ID y pertenencia a otro plato. Retirar libera la
  reclamación. TrySetOrder valida una permutación exacta antes de modificar nada.
  No modelamos inventario o contenedores futuros mediante una jerarquía genérica.
- `DishDefinition` y su instantánea inmutable `DishProfile` describen reconocimiento,
  separados de DishState. Una secuencia exacta de IDs de definiciones y el requisito
  configurable RequiresStack bastan para M5. Primer match por orden de assets.
  Hamburger: Bun/Patty/Bun. Cheeseburger: Bun/Patty/Cheese/Bun. Sin condiciones
  sanitarias/térmicas de aceptación; reconocer un nombre no significa comestible.
  Cualquier otra composición no vacía puede finalizar como Custom Dish.
- `AssemblySurface` adapta un volumen físico local: consulta colliders sólidos
  de unidades registradas en FoodSimulation, excluye inactivas, agarradas y cuerpos
  kinematic. Actualiza altas/bajas y orden; sin registro permanente de triggers.
  No escanea toda la escena ni permite coger/confirmar a través de paredes: E usa
  detector y alcance M2 existentes. Se añade solo `Pickup.IsHeld`, consulta genérica
  del agarre; no se introduce Food/Dish en controlador, input o portador M2.
- Corrección de aceptación manual: `DishAssemblyInteraction` resuelve la estación
  del ingrediente del primer hit por pertenencia real y referencias explícitas a
  superficies. No atraviesa geometría para buscar bandejas. El panel destaca
  reconocimiento/confirmación fuera del scroll. Player/FinalizeDish (F) es intención
  secundaria de montaje: E conserva retirada/reorganización de ingredientes y
  también confirma apuntando directamente a bandeja/pestaña. Los nombres de estas
  acciones usan el reconocimiento actual. M2 sigue sin conocer Food/Dish.
- Orden físico: centro de masa del Rigidbody proyectado al espacio local del sensor,
  altura ascendente en bandas de 2 cm; empate por x, z e ID de unidad. Para reconocer
  una pila, los centros laterales deben quedar a <=18 cm del componente inferior y
  cada altura consecutiva aumentar >=2 cm. Una fila o una pila desalineada conserva
  sus unidades/orden pero normalmente es Custom Dish. Valores configurables.
  No inferimos Bottom/Top Bun desde un modelo distinto; son unidades Bun distintas.
- El volumen admite una pila completa; no obliga receta, slots, cantidad fija,
  snapping ni secuencia de colocación. Retirar con M2 excluye inmediatamente la
  unidad agarrada del siguiente refresh; recolocarla puede incorporarla otra vez.
  Desactivar sensor/superficie libera borrador; unidades desactivadas/destruidas
  salen de la composición en refresh. Confirmar vuelve a consultar posiciones.
- `DishItem` conserva DishState y las FoodItems originales. Confirmación valida
  todas las unidades antes de bloquear composición. Luego desparenta la bandeja
  de la estación, conserva poses de alimentos como hijos visuales, desactiva sus
  Pickup/colliders y retira físicas individuales con Rigidbody kinematic sin CCD,
  interpolación ni detección de colisiones. Siguen existiendo sus estados reales.
  Una caja conservadora engloba bandeja y componentes; Rigidbody raíz dinámico
  con masa de bandeja + alimentos y Pickup M2 lleva el conjunto sin piezas sueltas.
  No hay joints, prefab genérico, clon de alimentos o soldadura geométrica.
- FoodSimulation mantiene exactamente sus referencias originales y continúa
  envejeciendo/enfriando alimentos finalizados una vez. No aparece otro reloj.
  Colliders individuales desactivados impiden calentar componentes del agregado
  en M4; no se modela transmisión de calor a través de un plato.
- Finalizado: composición/orden/nombre reconocido quedan fijos, estados vivos.
  No se desarma en M5. `DishItem.IsIntact` detecta pérdida de componentes físicos;
  `DishState.Dispose` libera reclamaciones e invalida operaciones al destruir el
  plato; conserva referencias como historial. No entregar estados disposed/incompletos
  en sistemas futuros. Desactivar y reactivar conserva identidad y composición.

## Consecuencias para M6

El futuro pedido/entrega puede identificar DishState.InstanceId y consultar las
unidades concretas, incluyendo carne podrida cocinada o contaminación. El coste
agregado es dato de referencia, no saldo, precio de venta ni una transacción.
No se inventa qualityScore ni políticas comerciales/sanitarias.

Un guardado necesitará ID de plato, IDs de unidades, orden, reconocimiento y los
estados de ingredientes completos (incluyendo exposición y dosis), restaurando
referencias compartidas y pertenencia una vez. No guardar solo el nombre del plato.
GUIDs actuales son de sesión y no persisten entre nuevas partidas sin ese guardado.

El proxy sólido incluye huecos entre ingredientes y puede ser conservador para
composiciones grandes. El agarre mantiene límites de espacio/velocidad M2; puede
soltar automáticamente una bandeja que no quepa. Se finaliza una sola bandeja por
estación; tres fixtures permiten comparar platos. Restaurar fixture requiere nueva
sesión. Física y detección aproximadas no garantizan una pila perfecta ni capas
físicamente conectadas; la identificación de pila es una política de prototipo.
