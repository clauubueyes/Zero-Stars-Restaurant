# ADR 0005: cocción térmica y fuente física M4

Fecha: 2026-10-06. Estado: implementada; aceptación manual pendiente.

## Contexto

M3 aprobado (`307174a`) separa definición, unidad y representación. ADR 0004 exige
una única responsabilidad por avanzar tiempo/temperatura. La petición de M4
amplía el plan inicial: varias porciones simultáneas, potencia y temperatura
configurables, progresión continua e independiente de deterioro.

## Decisión

- `FoodProfile.Cooking` es configuración inmutable opcional. Ausente significa
  no cocinable; `FoodDefinition` permite habilitarla y configurar temperaturas
  y umbrales. Beef está habilitado; pan y queso permanecen deshabilitados.
  No hay comparaciones de IDs/tipos en la plancha ni referencias a representación.
- Cada FoodState conserva su CookingState independiente. La dosis acumulada es
  tiempo equivalente a temperatura de referencia. Los estados se derivan:
  Raw con dosis cero; Undercooked con dosis positiva; Cooked/Overcooked/Burnt
  al alcanzar sus umbrales. La dosis se satura en Burnt; no se restablece al retirar.
  Progreso 100% significa Cooked y puede superar 100% hasta Burnt.
- Mientras hay una fuente efectiva que permite cocinar, la tasa es
  `max(0, (foodTemperature - minimum) / (reference - minimum))`.
  El tiempo por sí solo no cocina. Un alimento no cocinable puede calentarse.
  Frescura, edad, exposición al deterioro y contaminación conservan sus reglas M3.
- `ThermalEnvironment` describe temperatura objetivo, multiplicador de respuesta
  y permiso de cocción. El ambiente ordinario no permite cocinar. M4 pausa la
  dosis al retirar la pieza aunque aún esté caliente: elección de prototipo
  explícita, sin cocción residual; su temperatura sí baja gradualmente.
- `FoodState.Advance` calcula temperatura exponencial y la integral analítica de
  la tasa durante la parte del intervalo por encima del mínimo. Considera el
  calentamiento inicial y el cruce del umbral; no usa la temperatura final para
  todo el intervalo ni un bucle por segundo. Con entorno constante, un salto
  grande y pasos pequeños dan resultados equivalentes. Cambios de entorno
  requieren segmentar los intervalos. Se validan parámetros antes de mutar.
- `FoodSimulation` sigue siendo el único driver de escena. Resuelve un entorno
  y llama una vez al Advance de cada unidad registrada, descartando referencias
  duplicadas. Temperatura y cocción no tienen otro Update. Mantiene envejecimiento
  M3 para unidades inicializadas inactivas, pero estas no reciben calor de la fuente.
- `HeatSource` es el contrato Runtime mínimo para proporcionar un entorno local.
  `GrillHeatSource` consulta solapamiento físico de colliders sólidos de FoodItem
  con un BoxCollider trigger fino sobre la superficie sólida. Usa el volumen
  orientado actual; no mantiene registros OnTriggerEnter/Exit ni controla pickup.
  Consultas repetidas/colliders compuestos no producen múltiples avances.
  Fuente/comida/zona desactivada o destruida deja de aportar calor.
- Fuentes explícitas en el driver; si se solapan, gana la de mayor multiplicador
  de transferencia; empates por orden serializado. No se mezclan temperaturas ni
  se suma energía. Potencia cero equivale a no aportar entorno; una plancha fría
  con potencia positiva sí es un entorno que puede enfriar. No implementamos
  frigoríficos ni otras estaciones.
- Generador existente ampliado con plancha, zona invisible, mesa y cuatro carnes
  adicionales. Se habilita explícitamente el asset Beef preexistente preservando
  GUID/identidad. Reconstrucciones reutilizan assets y no pisan ajustes personalizados.
  Editor ofrece multiplicador/botón temporal; temperatura/potencia del componente
  se editan en Inspector durante Play. Sin controles nuevos del jugador ni `[E] Cook`.

## Consecuencias y límites

M5 deberá utilizar la misma FoodItem/FoodState física: cocinar no sustituye prefab,
ID de definición, Rigidbody, Pickup ni referencia del estado. CookingStage y
FoodCondition son ejes separados; Cooked no implica Fresh ni ausencia de contaminación.
No se introduce ahora una regla de comestibilidad o aceptación de pedidos.
Un futuro guardado debe conservar también dosis de cocción; el enum es derivado.
No reordenar valores serializados de enums ni reutilizar IDs de definiciones.

Transferencia y dosis son aproximaciones configurables; no dependen de masa,
cara en contacto o reparto energético entre piezas. El contacto es un volumen
efectivo fino: permite un pequeño espacio de aire y una pieza sostenida que
realmente intersecte el volumen puede calentarse. No modela calor a distancia.
La plancha no pierde temperatura al añadir comida; el color es referencia visual
estática y no confirma su temperatura. Sin electricidad, combustible ni fuego.

El driver tiene referencias locales y un único propietario por unidad; futuras
unidades deberán añadirse a esa composición. No registrar la misma unidad en
dos drivers distintos. No se crea descubrimiento global o infraestructura de
persistencia. Los saltos manuales mantienen fija la colocación actual; no
reproducen trayectorias físicas durante el tiempo adelantado.
