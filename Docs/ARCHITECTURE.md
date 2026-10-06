# Arquitectura y convenciones

Estado: M1 implementa jugador FPS; M2 interacción genérica y agarre físico;
M3 añade definiciones y estado de alimentos, deterioro y temperatura en Domain,
adaptadores Unity e inspección. M4 añade fuente física y cocción térmica.
M5 añade montaje libre y transporte de platos; M5 y su feedback están aprobados.
M6 añade un cliente activo, pedidos, entrega física, evaluación y pago; aprobado en
`9335899`. El polish añade snap de montaje y transporte visible de ventas.
M1–M6/polish están aprobados en `144cca5`. M7 añade almacenamiento físico y
conservación térmica; validado en `8872f0d`. M8 añade compras físicas y
reinversión con el mismo ledger, sin inventario ni otro reloj.
M9 está validado en `aa146c3`; separa cocina y zona pública. M10 usa sus cuatro
QueuePoints para clientes FIFO físicos con un único pedido activo y paciencia de debug.
M10 validado en `bd8d33c`. M11 añade GameTime/RestaurantDay para la jornada;
el reloj del mundo controla únicamente el horario y no avanza alimentos.

## Dependencias y responsabilidades

```text
Input / componentes de escena / física / vistas (Runtime)
                         ↓
            Estado y reglas del juego (Domain)

Configuración (ScriptableObjects) → datos iniciales → instancias de partida
Editor y Tests → código del juego; el juego no depende de Editor o Tests
```

**Domain** contiene estado y reglas que pueden evaluarse sin una escena. No usa
`GameObject`, `Transform`, `Renderer`, `MonoBehaviour`, `Time` ni APIs del Editor.
Ejemplos implementados: progreso de cocción de una porción, contenido de un plato,
validación de pedido y saldo. El tiempo se pasa explícitamente a las operaciones;
las reglas no dependen de un Update oculto para poder probar umbrales.

**Runtime** traduce entrada y física a operaciones y representa sus resultados.
Una vista puede colorear una esfera según su estado, pero el color o el nombre
del objeto no determinan si es comestible. La interacción puede usar raycast y
colliders; la aceptación de un plato se decide por sus datos.

El estado de una porción pertenece a su instancia, y sigue siendo el mismo al
recogerla, colocarla, cocinarla y montarla. Un ingrediente no puede estar en dos
contenedores a la vez. Los adaptadores trasladan pertenencia sin clonar estado.
El modelo controla validez; Unity controla contacto, pose y presentación.

**ScriptableObjects** son definiciones compartidas editables: identidad del
ingrediente, umbrales de cocción, precio u otros parámetros. Crear una definición
cuando haya datos que configurar; no crear una clase base universal de assets.
El progreso, el plato actual, el pedido y el dinero viven en instancias de partida,
nunca se escriben en el asset compartido. No poner mallas o materiales en el núcleo
de reglas; asociarlos en el prefab o en un adaptador de presentación.

**Composición** comienza con referencias serializadas y una inicialización local
de escena. Solo si hace falta, un componente pequeño crea el estado de sesión y
lo entrega a sus adaptadores. No construir un framework de arranque, DI, service
locator ni bus de eventos general. Las comunicaciones directas o eventos C#
locales bastan; suscripciones con un ciclo de alta/baja claro.

## Estructura creada

```text
Assets/
  _Project/
    README.md
    Scripts/
      Domain/
      Runtime/
    Scenes/
    Prefabs/
    ScriptableObjects/
    Materials/
    Tests/
      EditMode/
      PlayMode/
    Editor/
  Scenes/SampleScene.unity        # plantilla conservada
  Settings/                      # URP conservado
  InputSystem_Actions.inputactions
  TutorialInfo/                  # bienvenida conservada
```

Crear subcarpetas por feature (Player, Interaction, Food, etc.) cuando exista su
primer archivo. No reservar ahora carpetas para policía, empleados o reputación.
No usar `Resources` ni Addressables sin una necesidad comprobada de carga.
M1–M6 incluyen `PrototypeRestaurant.unity`, tres materiales greybox, cuatro de
cajas físicas y tres de alimentos, todos URP/Lit simples. M3 crea tres
ScriptableObjects FoodDefinition. M4 añade un material de plancha y cuatro carnes.
M5 añade dos definiciones de reconocimiento, un material de bandeja, tres
estaciones y diez suministros. M6 añade dos materiales, configuración de servicio
y una raíz CustomerServiceZone. El polish conserva objetos/fileIDs/GUID, reubica
estaciones/obstáculos y añade un anchor al cliente. M8 añade tres prefabs de
ingredientes, tres productos configurables y estación greybox; reutiliza materiales.

## Assemblies y tests al implementar

M1 introduce assemblies Runtime, Editor y de tests por una necesidad real: las
pruebas y el generador deben referenciar el controlador sin depender de
Assembly-CSharp. Runtime referencia Unity.InputSystem; Editor referencia Runtime,
Input System y URP y solo compila para Editor. Los scripts del tutorial permanecen
en sus assemblies predefinidos. Los assemblies de tests son TestAssemblies y no
se incluyen en builds normales del jugador.

M3 introduce las primeras reglas de negocio:

- `ZeroStarRestaurant.Domain`: bajo Domain, `noEngineReferences: true`, sin
  referencias a Runtime o paquetes del Editor.
- Runtime referencia Domain para adaptar FoodState; Editor lo referencia para
  generar categorías/validar perfiles. No se mueven scripts M1/M2 ni cambian GUID.
- EditMode referencia Domain para pruebas deterministas, además de comprobar
  escena, configuración e input. PlayMode también lo referencia para comprobar
  identidad del estado durante interacción/física nativa y ciclo de activación.

Los assemblies de tests no se incluyen en builds de jugador. No añadir NUnit a
Assembly-CSharp, ni depender de Assembly-CSharp desde un assembly de tests:
primero separar el código bajo prueba. Mantener el asmdef de Editor limitado a la
plataforma Editor; no hacer que Runtime lo referencie.

En M1, `FirstPersonController` adapta input/mirada/cursor y llama a
`FirstPersonMotor.Step(movement, jumpPressed, deltaTime)`. Motor adapta intención
a CharacterController con gravedad y salto, sin conocer cámara, modelos o input.
Ambos son Runtime porque dependen de Unity; no se inventan clases Domain para
física del motor. Ver [ADR 0002](Decisions/0002-first-person-greybox.md).

Referencia técnica: [assemblies en Unity](https://docs.unity.com/en-us/engine/6000.0/manual/programming-environment/script-compilation/assembly-definition-files).

## Interacción física M2

```text
InteractionDetector → objetivo más cercano / oclusión / alcance
InteractionInput    → intención Interact / Drop / Throw
PlayerInteraction   → revalidación y llamada al contrato Interactable
Interactable        → CanInteract / TryInteract / nombre y acción
Pickup              → reclamación exclusiva → PhysicalCarry → Rigidbody
InteractionFeedback ← foco y agarre actuales / etiquetas del input
```

`PlayerInteraction` coordina referencias sin conocer tipos concretos. Un nuevo
interactuable deriva de `Interactable` e implementa disponibilidad y acción;
recibe un `InteractionContext` local con actor y portador opcional. No hay
registro global, inventario ni lista de tipos de objetos en el jugador.

`PhysicalCarry` conserva el Rigidbody dinámico, ajusta velocidad con fuerza
limitada según masa y mueve el destino mediante consultas de volumen. Nunca
parenta ni teletransporta el objeto. Guarda/restaura configuración física y pares
de colisión del jugador, y libera el agarre ante desactivación/destrucción o
espacio inseguro. `CarryPhysics` reúne cálculos deterministas de seguimiento y
salida; pertenece a Runtime por usar vectores/física Unity, sin crear un Domain
artificial. El comportamiento no consulta Renderer, mesh, material ni color.

Input y feedback dependen de `FirstPersonController.HasControl` para respetar
cursor/foco. La API de interacción puede probarse sin un dispositivo físico.
Cada adaptador posee su copia del asset; el de M2 solo habilita sus tres acciones.
Se reutilizan los assemblies existentes. Ver [ADR 0003](Decisions/0003-physical-interaction.md)
y [configuración, límites y validación de M2](M2.md).

## Alimentos M3

```text
FoodDefinition (asset de autoría) → FoodProfile (instantánea inmutable, Domain)
FoodItem (unidad Unity)           → FoodState (edad, exposición, temperatura, contaminación)
FoodSimulation (tiempo/ambiente)  → FoodState.Advance(segundos, ambiente, multiplicador)
Pickup + Rigidbody               → pose y agarre, sin referencias a Food
FoodInspectionFeedback           ← estado de la comida enfocada o sostenida
```

La definición incluye ID/nombre/categoría, coste en céntimos, vida de frescura,
respuesta térmica y umbrales. No incluye material, malla, collider ni estado
mutable. Dos FoodItem pueden compartir el asset; cada uno crea su propio estado
en Awake y conserva esa referencia al desactivar/reactivar y al recoger/lanzar.
Los ajustes iniciales de fixture son campos de la unidad, no del asset compartido.

La edad aumenta con tiempo explícito; la exposición equivalente aumenta con ese
tiempo por una tasa externa no negativa. Frescura 0–100 se deriva de exposición
limitada a su vida; la condición Fresh/Acceptable/Spoiled/Rotten se deriva de
umbrales, sin duplicar estado. Temperatura se aproxima analíticamente al ambiente.
Contaminación es una marca independiente. Domain no usa Time, GameObject ni Unity.

La API original M3 sin perfil de conservación usa tasa 1, independiente de temperatura.
M7 asigna explícitamente su perfil al reloj para conservación por temperatura real.
El método
Advance permite intervalos grandes, con resultados equivalentes a pasos pequeños
cuando entorno/tasa son constantes. Para cambios, segmentar los intervalos.
La fixture ofrece multiplicador y botón de desarrollo sin modificar el tiempo
global. El driver referencia las cuatro unidades explícitamente, sin descubrimiento
global, inventario ni persistencia.

El feedback de comida conoce ambos adaptadores para presentar datos. Los
componentes de interacción y el modelo de alimento no consultan su representación.
Ver [ADR 0004](Decisions/0004-food-state-and-time.md) y [M3](M3.md), incluyendo
responsabilidad única sobre tiempo y separación entre deterioro y futura cocción.

## Cocción M4

```text
GrillHeatSource → consulta física del volumen efectivo → ThermalEnvironment
FoodSimulation → resuelve entorno único → FoodState.Advance(tiempo, entorno)
FoodState      → temperatura + deterioro + CookingState (dosis independiente)
FoodDefinition → CookingProfile opcional, inmutable en la sesión
FoodInspectionFeedback ← frescura / temperatura / condición / etapa / progreso
```

La plancha no conoce IDs concretos ni avanza tiempo. El driver conserva referencias
explícitas y evita duplicar unidades en su lista; solo debe existir un propietario
temporal por unidad. Comida inactiva conserva el envejecimiento M3 y se aproxima
al ambiente, sin recibir calor de la plancha. No hay registro persistente de contactos.

La dosis integra una tasa térmica por encima del mínimo durante el calentamiento
exponencial, con resultados equivalentes al segmentar un entorno constante.
Retirar pausa dosis y enfría gradualmente; recolocar continúa desde el mismo estado.
No hay cocción residual en este prototipo. Cocinar no recupera frescura ni limpia
contaminación. M2 permanece genérico: no se modifican sus scripts ni input.
Ver [ADR 0005](Decisions/0005-thermal-cooking.md) y [M4](M4.md).

## Conservación y almacenamiento M7

```text
Fridge / Freezer (ColdStorage) → posición actual en interior → ThermalEnvironment
Plancha / almacenamiento / ambiente → FoodSimulation → un entorno por FoodItem
FoodPreservationSettings → FoodPreservationProfile inmutable (Domain)
FoodState.Advance(tiempo, entorno, preservation: perfil) → edad + exposición térmica + temperatura
Pickup / Rigidbody / estantes → colocar y recuperar la misma unidad
```

ColdStorage reutiliza HeatSource sin avances adicionales ni listas de entrada/salida.
El contrato permite un objetivo frío y desactiva cocción. Compara la posición de la
unidad con un interior orientado, también para ingredientes originales de Dish
cuyos colliders individuales están retirados. No consulta recetas, representación
ni identidad para decidir temperatura. Un gabinete desactivado o transferencia cero
deja de aportar entorno; el reloj vuelve al ambiente sin salto de temperatura.

La política usa temperatura real: >5 °C tasa 1; entre 0 y 5 °C tasa 0.1;
≤0 °C tasa 0.001. Integra analíticamente cruces durante la relajación exponencial,
sin bucles por segundo ni decisiones por pertenencia a nevera. La edad avanza
completa; frío no restaura frescura, contaminación o dosis de cocción. La política
también se aplica mientras una pieza retirada todavía permanece fría.

El asset compartido solo configura umbrales/tasas, validado y copiado una vez por
Advance. Sin perfil, la API conserva las reglas originales M3/M4. La escena asigna
el perfil explícitamente y conserva un solo propietario de tiempo. Se mantiene
selección M4 por mayor transferencia y empate por orden de referencias.
Los gabinetes son fixtures activos sin puertas, inventario, electricidad o compras.
Ver [M7](M7.md) y [ADR 0009](Decisions/0009-food-storage-refrigeration.md).

## Platos y montaje M5

```text
DishDefinition → DishProfile (secuencia de IDs, requisito de pila)
FoodState      → Guid de unidad y pertenencia exclusiva de montaje
DishState      → referencias ordenadas a FoodState + consultas agregadas vivas
AssemblySurface → consulta física + orden aproximado → composición de borrador
PlaceIngredient / clic → DishAssemblyInteraction → AssemblySurface → snap por bounds
F / E sobre bandeja → confirmar → DishItem con proxy físico + Pickup genérico
FoodSimulation → mismas unidades originales, único reloj antes/después de confirmar
```

DishState es independiente de FoodState; sus componentes son referencias reales,
sin snapshots desconectados. Reconocimiento usa secuencia exacta y alineación
aproximada, sin reglas comerciales. Confirmar asigna identidad pública al plato,
congela composición/orden y mantiene estados vivos. FoodItem originales siguen
existiendo como hijos del agregado con físicas individuales retiradas; el proxy
lleva el conjunto. No hay sustitución por prefab de hamburguesa, inventario o
save system. IDs de unidades/platos son distintos de IDs de definiciones.
Ver [ADR 0006](Decisions/0006-physical-dish-assembly.md) y [M5](M5.md).

El snap pertenece solo al adaptador AssemblySurface: exige una Food real sostenida
registrada en su reloj, hueco libre dentro del sensor y pertenencia válida. El
coordinador revalida el primer hit/alcance sin atravesar obstáculos. M2 libera el
agarre, se colocan los bounds sobre bandeja/pila y se anulan velocidades de salida;
la unidad sigue dinámica y extraíble con E. No cambia FoodState ni se crean unidades.
El proxy final abarca los colliders de todos los ingredientes; colliders/Pickup hijos
se retiran. PhysicalCarry acepta una elevación mínima configurable en Pickup (-90°
por defecto, 5° para platos), y una tolerancia de aproximación inicial decreciente
para levantar desde el suelo sin liberar prematuramente. No conoce tipos de comida.
Fuerza, velocidad, comprobaciones de espacio y tether normal M2 permanecen activos.

## Servicio M6

```text
CustomerServiceConfiguration → OrderOffer (DishProfile + precio en céntimos)
CustomerVisit → OrderState (IDs distintos y estado de cada visita/pedido)
CustomerMovement ← ruta explícita, sin reglas de alimentos
CustomerDishCarrier ← mismo Dish vendido → anchor local hasta salida
DeliveryZone → DishItem final, suelto, intacto, apoyado y suficientemente lento
CustomerServiceLoop → OrderDelivery → OrderEvaluation + PaymentLedger
OrderEvaluation → DishSnapshot → IngredientSnapshot de unidades originales
OrderFeedback ← pedido/visita/saldo y último resultado histórico
```

CustomerVisit valida las transiciones Enter → Order → Wait → Receive → Evaluate
→ Pay/Reject → Leave → Finished. Movement solo mueve el placeholder; el coordinador
crea un pedido por visita, consume tiempos de configuración y nunca prepara platos.
Receive/Evaluate son fases síncronas de la transacción; Pay/Reject dura 4 s por defecto.
Máximo un cliente/pedido activo; tras salir se reutiliza el placeholder con nuevas
identidades y un intervalo de 3 s. Menú alternado, con override de desarrollo una vez.

Corrección compara ID reconocido de DishDefinition; no mira nombre, malla, color,
frescura, contaminación ni CookingStage. Estos datos aparecen por separado en el
resultado. Pedido correcto paga el precio completo incluso Raw/Burnt/Rotten;
incorrecto o Custom Dish paga cero, termina visita y conserva plato vivo.

OrderDelivery captura evidencia primero y registra una única transacción síncrona:
solo tras validar IDs/duplicados/overflow modifica saldo, marca vendido y cierra
pedido. PaymentLedger usa long en céntimos, sin conocer alimentos/cocción. Un fallo
de transacción deja pedido y plato disponibles. No hay soporte multihilo/persistencia.

DishState mantiene referencias vivas M5 incluso vendido, hasta la salida. Las snapshots de M6 son
evidencia histórica inmutable del instante de entrega, con los mismos IDs, perfiles,
estado y coste, independiente del posterior deterioro o destrucción. Después de
cobrar, CustomerDishCarrier vincula el mismo agregado al anchor del cliente,
retira su Pickup y colisiones y lo mantiene visible/cinemático. FoodSimulation
sigue siendo el único reloj. Al completar salida, CustomerServiceLoop desregistra
las unidades y desactiva/destruye el plato. M10 retira también el NPC admitido y
libera su reserva para avanzar la cola; las fixtures M6 aún reutilizan un cliente. Cancelar
servicio también limpia la venta. Rechazos no se vinculan ni se destruyen.
No quedan referencias Unity en recibos.
Los IDs procesados se conservan en el ledger de la sesión para impedir doble pago.

DeliveryZone consulta volumen actual, ignora ingredientes/cajas/borradores/manos
ocupadas y exige apoyo en el pad. La entrega tolerante usa solapamiento del collider
real con el trigger y cobertura XZ mínima del 20% del área menor pad/plato, no el
centro del plato. Conserva tolerancia de altura y velocidades; visual y trigger
comparten huella. Ver [corrección y límites](DELIVERY-TOLERANCE-FIX.md).
Una colocación rechazada se registra hasta retirar
o recoger el plato: evita venderlo involuntariamente al siguiente cliente. No exige
una nueva tecla, input o dependencia. Ver [ADR 0007](Decisions/0007-customer-delivery-payment.md)
y [M6](M6.md).
Ver [ADR 0008](Decisions/0008-vertical-slice-polish.md) y
[polish y validación actual](VERTICAL-SLICE-POLISH.md) para estos cambios Runtime.

## Convenciones prácticas

M11 usa GameTime/RestaurantDay en Domain y RestaurantDayController en Runtime,
con referencias locales mutuas a la cola. Su Update corre antes del servicio y
convierte segundos de simulación en segundos del mundo con velocidad configurable.
La cola consulta Open para toda admisión y notifica su ocupación al completar
Exit. Closing sigue sirviendo a clientes admitidos; Closed permite iniciar el
siguiente día sin recrear ledger, FoodState o DishState. La pausa es únicamente
del reloj del mundo. FoodSimulation conserva su tiempo/driver independiente;
no hay salto alimentario nocturno. Ver [ADR 0013](Decisions/0013-restaurant-day-and-world-time.md).

M10 mantiene las reservas e identidades en `CustomerQueueState`/`QueuedCustomerState`
(Domain). `CustomerQueueController` adapta admisión, recorrido por puntos y retiro;
`QueuedCustomer` vincula cada estado a su Movement/Carrier. `CustomerServiceLoop`
es el único driver, con pasos fijos de 0.05 s y remanente. Crea el pedido solo al
llegar la cabeza a Service Position y conserva su ID en CustomerVisit. Los pedidos
tienen IDs propios. La paciencia restante llega a cero sin alterar el servicio;
el HUD muestra estado, plaza y espera individual. Ver [ADR 0012](Decisions/0012-customer-queue-and-patience.md).

- Código y nombres técnicos en inglés; documentación y explicaciones en español.
- Namespace raíz `ZeroStarRestaurant`; subnamespaces por responsabilidad y feature.
  Una clase pública principal por archivo, nombre igual al del archivo.
- PascalCase en tipos, métodos y propiedades; camelCase en parámetros/locales;
  `_camelCase` en privados. Campos privados serializados con `[SerializeField]`,
  sin exponer campos públicos mutables solo para usar el Inspector.
- Nombres claros de assets: `PrototypeRestaurant`, `Player`, `CookingStation`,
  `PlaceholderFood`. Sin prefijos de tipo obligatorios para todos los archivos.
- Dinero en céntimos enteros; 0 € = 0, 5 € = 500. Sin float para pagos.
- Usar unidades Unity como metros; tiempos en segundos. Definir y probar los
  umbrales de cocción al implementar M4.
- Gameplay inicial en una escena. Sin persistencia, scene streaming ni sistema
  de guardado hasta que exista un requisito jugable.
- Prefabs con colliders y componentes; geometría placeholder en un hijo Visual
  cuando facilite su sustitución. El núcleo no consulta la forma de ese hijo.
- Estado explícito para pedidos y entregas: rechazar una segunda entrega/pago,
  sin inferir el estado a partir de Destroy o de objetos ocultos.
- Validar input y referencias obligatorias con mensajes claros, sin fallos
  silenciosos. Comprobar OnEnable/OnDisable si se suscribe a callbacks de input.

## Interacción física y montaje sobre superficies

El polish físico previo a M12 separa hold/release de Pickup y acciones contextuales.
PhysicalDishAssembly encuentra contacto y apoyo fuera de estaciones, libera claims
de preview y confirma con DishItem y los FoodState originales. DishTraySupply solo
crea un prefab vacío cuando el hueco queda libre. Grill consulta geometría local
conservada por cada FoodItem tras retirar sus colliders, sin otro reloj ni calor
automático para todo el agregado. Ver [ADR 0014](Decisions/0014-physical-interaction-and-surface-assembly.md).

La colocación asistida solicitada posteriormente se intenta al soltar el ratón,
con mano tranquila y apoyo/pila cercana. InteractionInput referencia explícitamente
DishAssemblyInteraction, que reutiliza el snap de bandeja o bounds sobre un apoyo
normal. Valida primer hit, distancia, obstáculos y pertenencia antes de liberar.
G y gestos rápidos mantienen liberación libre; F sigue confirmando.

## Cuándo documentar una decisión

Añadir una nota corta en `Docs/Decisions/NNNN-nombre.md` para cambios de dependencias,
pipeline, persistencia, límites entre sistemas o representación del estado que
afecten al trabajo posterior. Incluir contexto, decisión y consecuencias. No hace
falta un ADR para cada campo, componente o cambio de color.

## Economía y adquisiciones M8

IngredientPurchaseButton reutiliza E/Interactable y referencia una estación local.
IngredientProduct configura precio entero y prefab raíz inactivo. La estación
comparte el PaymentLedger de CustomerServiceLoop con las ventas, comprueba fondos y
salida física y prepara una nueva FoodItem antes de activar. TryInitialize crea
FoodState una sola vez; FoodSimulation.Register deduplica el registro. TrySpend
valida IDs de compra/unidad y descuenta una vez; un fallo retira la unidad provisional.
No se escribe estado en assets ni se crea inventario o un driver alimentario adicional.

DevelopmentIngredientSupply retira las fixtures gratuitas de la simulación y las
desactiva antes de su Awake, salvo opt-in explícito de desarrollo. La escena configura
1000 céntimos iniciales de desarrollo; el valor por defecto del código continúa en 0.
La primera provisión con saldo 0 es deuda de diseño. Ver [M8](M8.md) y
[ADR 0010](Decisions/0010-economy-ingredient-procurement.md).
