# Arquitectura y convenciones

Estado: M1 implementa adaptadores Unity de jugador, escena greybox y pruebas.
Domain no tiene clases todavía; las separaciones de negocio descritas aquí se
aplicarán cuando aparezcan sus reglas reales.

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
M1 incluye `PrototypeRestaurant.unity` y tres materiales simples URP/Lit. Todavía
no hay prefabs ni ScriptableObjects propios.

## Assemblies y tests al implementar

M1 introduce assemblies Runtime, Editor y de tests por una necesidad real: las
pruebas y el generador deben referenciar el controlador sin depender de
Assembly-CSharp. Runtime referencia Unity.InputSystem; Editor referencia Runtime,
Input System y URP y solo compila para Editor. Los scripts del tutorial permanecen
en sus assemblies predefinidos. Los assemblies de tests son TestAssemblies y no
se incluyen en builds normales del jugador.

Al introducir las primeras reglas de negocio (previsto M3):

- `ZeroStarRestaurant.Domain`: bajo Domain, `noEngineReferences: true`, sin
  referencias a Runtime o paquetes del Editor.
- Añadir a `ZeroStarRestaurant.Runtime` una referencia a Domain cuando use sus
  reglas. No mover los scripts M1 ni regenerar sus GUID.
- Ampliar `ZeroStarRestaurant.Tests.EditMode` con una referencia a Domain para
  sus pruebas. Ya prueba referencias, entrada y geometría de la escena M1.
- Mantener `ZeroStarRestaurant.Tests.PlayMode` para pruebas de integración. M1
  comprueba física nativa y el ciclo de activación del input.

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
