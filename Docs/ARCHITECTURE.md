# Arquitectura y convenciones

Estado: M1 implementa jugador FPS; M2 interacción genérica y agarre físico;
M3 añade definiciones y estado de alimentos, deterioro y temperatura en Domain,
adaptadores Unity e inspección. Los sistemas de cocción/restaurante siguen pendientes.

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
Ejemplos futuros: progreso de cocción de una porción, contenido de un plato,
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
M1–M3 incluyen `PrototypeRestaurant.unity`, tres materiales greybox, cuatro de
cajas físicas y tres de alimentos, todos URP/Lit simples. M3 crea tres
ScriptableObjects FoodDefinition. Todavía no hay prefabs propios.

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
