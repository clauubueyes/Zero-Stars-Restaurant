# ADR 0029: calendario de sesión y ciclo diario explícito

Fecha: 2026-10-10. Base validada: `e93510e`.

## Decisión

Ampliar RestaurantDay M11 sin otra máquina de estados:
Preparation → Open → Closing → Closed → EndOfDay → Preparation del día siguiente.
Ready solo conserva la opción de no iniciar Day 1 automáticamente.

GameTime posee RestaurantCalendar; RestaurantDay/Controller exponen esa misma
instancia. CurrentDay empieza en 1, TotalDaysElapsed es CurrentDay − 1. Solo empezar
desde EndOfDay incrementa +1. HasReachedDay consulta umbrales sin reglas de eventos.

Preparation fija la hora inicial configurable (08:00 en escena); el reloj del mundo
permanece detenido, igual que en Closed/EndOfDay. OpenRestaurant selecciona Opening
Hour (09:00) sin sumar el intervalo al tiempo efectivo. No hay apertura automática.
Solo Open/Closing avanzan GameTime con velocidad/pausa M11. Horario de cierre o
Force Close bloquean admisiones; todas las reservas existentes deben terminar hasta
Exit para alcanzar Closed. Force Close no expulsa visitas ni adelanta relojes.

Closed conserva interacción física y Procurement. EndCurrentDay aplica costes fijos
M13 una vez, congela electricidad diaria y crea EndOfDaySummary antes de publicar
DayEnded Runtime. Esta frontera sustituye settlement automático en Closed de ADR
0018/0019. Next Day exige EndOfDay y contabilidad válida, incrementa día, reinicia
contabilidad/medidores/estadísticas diarios antes de DayStarted. Toda la sesión,
historiales, objetos y estados originales sobreviven sin recargar escena.

## Evidencia

EndOfDaySummary proyecta DailySummary, CustomerServiceStatistics y reputación reales,
en un snapshot inmutable. M15 añade Daily* sin borrar totales/history ni deduplicación.
Los peligros alimentarios M15 se distinguen de confirmaciones/descartes M16, calculados
por DayNumber del evento de resolución. Una exposición Day 1 puede confirmarse Day 2
sin inventar otra reacción. Sin proveedor de reputación: null/Unavailable.

Devengado != pagado. End Day cobra los costes fijos existentes, nunca electricidad.
Preparation/Open/Closing/Closed registran kWh diarios; EndOfDay congela el diario y
sigue acumulando consumo histórico/período. Next Day solo reinicia diarios, también
en aparatos OFF. El pago eléctrico explícito M13 reproyecta el snapshot de caja del
HUD sin volver a cobrar alquiler; el historial M19 conserva la evidencia al terminar.

## Tiempo e incidentes

GameTime.ElapsedWorldSeconds sigue monotónico, con avance efectivo Open/Closing.
Cambiar día, abrir, forzar cierre o consultar resumen no añade una noche ni resuelve
riesgos. M16 conserva exactamente PendingHealthRisk, DueWorldSeconds, RNG, consecuencias
y registros. ResolveDue continúa tras el avance normal del mundo en el controlador
existente. Start Next Day no llama al resolver. Preparation no consume los plazos.

FoodSimulation sigue siendo el único driver de FoodState, con tiempo independiente.
Comida, física, conservación, cocina, limpieza y electricidad funcionan en todas las
fases. Las transiciones no avanzan alimentos; tiempo real jugado entre ellas sí sigue
simulándolos. Sin noche, deterioro offline o disco.

## APIs y presentación

Domain y Runtime exponen DayStarted, RestaurantOpened, RestaurantClosing,
RestaurantClosed y DayEnded con int de día. Usar Runtime para observar hooks completos:
DayEnded ve settlement/resumen; DayStarted ve diarios nuevos. El inicio se publica
desde Start después de los Awake de proveedores. OpenRestaurant, ForceClose,
EndCurrentDay y StartNextDay devuelven bool sin mutar ante fase inválida.
Inspector usa esas APIs y explica rechazos; no hay salto arbitrario que omita hooks.

Gameplay HUD conserva su única región de día/resumen y OnGUI. Enter ejecuta la acción
visible de Preparation/Closed/EndOfDay. Desarrollo queda en Inspector, sin Force Close
en gameplay ni cambios del asset de input compartido.

## Límites

Persistencia durante Play; sin save/load, semanas/meses, nueva facturación/alquiler,
inspecciones/M20, eventos, empleados, progreso o arte. No se ejecuta rebuild/instalador.
PrototypeRestaurant conserva todos sus registros, objetos, transforms, materiales y
referencias; cambia exclusivamente Starting Hour 9 → 8.
