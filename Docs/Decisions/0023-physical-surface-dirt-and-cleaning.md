# ADR 0023: suciedad local y limpieza física

## Estado

Implementado en M17 desde `1609a29`, rama `feature/hygiene-cleaning`.

## Decisión

Cada `CleanableSurface` posee un `DirtState` de sesión independiente. El Domain
es C# sin Unity: cantidad continua 0–1, perfil inmutable de umbrales,
contribuciones por `DirtKind`, identidad propia y `DirtChange` inmutable con
evento `Changed`. El último cambio conserva origen y unidad de comida cuando
corresponde. No se acumula un historial por frame.

`FoodSimulation` sigue avanzando cada alimento exactamente una vez. Después
notifica al único `HeatSource` seleccionado la diferencia efectiva de dosis de
cocción. La plancha configura una referencia a su superficie; el resto de fuentes
puede conservarla vacía. La higiene observa ese uso sin avanzar ni contaminar
`FoodState`. Los contactos de preparación y suelo los detecta un componente
opcional con referencias explícitas y deduplicación por unidad: ensucia al entrar
en contacto, no durante todos los frames que permanece apoyada.

`CleaningTool` utiliza `Pickup`/`PhysicalCarry`. `InteractionInput` mantiene la
acción E mientras se sostiene la herramienta. `CleaningInteraction` consulta
el raycast sólido existente, ignora el objeto sostenido y solo permite limpiar
el área apuntada si la herramienta está cerca. Soltar E, soltar la herramienta,
perder control, salir de alcance o interponer geometría detiene el progreso.
No hay un segundo Update de input ni reloj de higiene: la limpieza recibe tiempo
explícito de interacción. `Next Day` no modifica los estados de suciedad.

La visualización vive en `DirtSurfaceView`: seis primitivas nuevas sin collider
por superficie, sobre el soporte real, aumentan su área según la cantidad.
Nunca escribe en el estado ni en materiales/renderers originales. El texto
contextual amplía `InteractionFeedback`, dentro de la región desplazable del
único `DebugHudPresenter`; no hay nuevos OnGUI ni bloques superpuestos.

## Consecuencias

La escena se adapta incrementalmente. Se conservan los transforms, materiales,
objetos, GUID y configuración actuales. Dos regiones de suelo comparten su
collider existente y poseen estados independientes. El instalador no reconstruye
una instalación existente ni restablece sus valores modificados por el usuario.

M14 decide completitud/pago; M15 decide calidad/reacción; M16 conserva reputación
y riesgos diferidos. M17 no modifica ninguna de esas reglas, probabilidades o
datos. Tampoco crea contaminación de comida, consumibles, inspecciones, plagas,
higiene global ni guardado. Las APIs de consulta/adición y el evento local son
suficientes para futuras conexiones; no se introduce un bus global.
