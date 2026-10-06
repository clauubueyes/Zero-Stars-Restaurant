# Arquitectura y convenciones

Estado: M1 implementa jugador FPS; M2 interacción genérica y agarre físico;
M3 añade definiciones y estado de alimentos, deterioro y temperatura en Domain,
adaptadores Unity e inspección. M4 añade fuente física y cocción térmica.
M5 añade montaje libre y transporte de platos; M5 y su feedback están aprobados.
M6 añade un cliente activo, pedidos, entrega física, evaluación y pago. M7 pendiente.

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
y una raíz CustomerServiceZone; conserva toda la geometría M1–M5. Sin prefabs propios.

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

M3 no relaciona automáticamente temperatura y tasa de deterioro ni modela cocina.
El driver usa tasa 1; un adaptador posterior puede suministrar otra. El método
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

## Platos y montaje M5

```text
DishDefinition → DishProfile (secuencia de IDs, requisito de pila)
FoodState      → Guid de unidad y pertenencia exclusiva de montaje
DishState      → referencias ordenadas a FoodState + consultas agregadas vivas
AssemblySurface → consulta física + orden aproximado → composición de borrador
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

## Servicio M6

```text
CustomerServiceConfiguration → OrderOffer (DishProfile + precio en céntimos)
CustomerVisit → OrderState (IDs distintos y estado de cada visita/pedido)
CustomerMovement ← ruta explícita, sin reglas de alimentos
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

DishState mantiene referencias vivas M5 hasta la venta. Las snapshots de M6 son
evidencia histórica inmutable del instante de entrega, con los mismos IDs, perfiles,
estado y coste, independiente del posterior deterioro o destrucción. Después de
cobrar, Runtime desregistra los FoodItems del único FoodSimulation, desactiva y
destruye el agregado. No quedan objetos vendidos ni referencias Unity en recibos.
Los IDs procesados se conservan en el ledger de la sesión para impedir doble pago.

DeliveryZone consulta volumen actual, ignora ingredientes/cajas/borradores/manos
ocupadas y exige apoyo en el pad. Una colocación rechazada se registra hasta retirar
o recoger el plato: evita venderlo involuntariamente al siguiente cliente. No exige
una nueva tecla, input o dependencia. Ver [ADR 0007](Decisions/0007-customer-delivery-payment.md)
y [M6](M6.md).

## Convenciones prácticas

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

## Cuándo documentar una decisión

Añadir una nota corta en `Docs/Decisions/NNNN-nombre.md` para cambios de dependencias,
pipeline, persistencia, límites entre sistemas o representación del estado que
afecten al trabajo posterior. Incluir contexto, decisión y consecuencias. No hace
falta un ADR para cada campo, componente o cambio de color.
