# Contenido de Zero Star Restaurant

Todo el contenido propio del juego se incorpora aquí. La plantilla existente en
`Assets/Scenes`, `Assets/Settings`, `Assets/TutorialInfo` y el asset de input se
conservan en sus ubicaciones actuales para mantener sus GUID y referencias.

| Carpeta | Responsabilidad |
| --- | --- |
| `Scripts/Domain` | Estado y reglas en C# sin dependencias de Unity. |
| `Scripts/Runtime` | Componentes de Unity, input, física, vistas y composición. |
| `Scenes` | Escenas propias: `PrototypeRestaurant` para M1–M3. |
| `Prefabs` | Objetos reutilizables con primitivas y componentes. |
| `ScriptableObjects` | Datos de configuración compartidos, sin estado de partida. |
| `Materials` | Materiales simples de color compatibles con URP. |
| `Tests/EditMode` | Pruebas de reglas y validación de datos. |
| `Tests/PlayMode` | Pruebas de integración con escenas y componentes. |
| `Editor` | Herramientas que solo se ejecutan en el Editor. |

Las carpetas vacías contienen `.gitkeep` para que existan también tras clonar.
M1 incluye `Scenes/PrototypeRestaurant.unity`, tres materiales greybox, controlador
FPS en `Scripts/Runtime/Player`, generador de escena en Editor y tests EditMode y
PlayMode. Prefabs sigue reservado para tareas futuras.
M2 añade `Scripts/Runtime/Interaction`, cuatro materiales de cajas y extiende el
generador/escena de M1. Usa el asset de input existente con Interact, Drop y Throw.
Ver `Docs/M2.md` en la raíz.
M3 incorpora reglas en `Scripts/Domain/Food`, adaptadores en `Scripts/Runtime/Food`,
tres definiciones en `ScriptableObjects/Food`, tres materiales y una zona con cuatro
alimentos. Estado, Pickup y representación siguen separados; no hay cocina.
Ver `Docs/M3.md` y ADR 0004 en la raíz.
Los tests ya tienen sus assemblies; no añadir NUnit al código Runtime.

Consulta `README.md`, `AGENTS.md` y `Docs/ROADMAP.md` en la raíz antes de trabajar.
