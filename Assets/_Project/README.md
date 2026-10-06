# Contenido de Zero Star Restaurant

Todo el contenido propio del juego se incorpora aquí. La plantilla existente en
`Assets/Scenes`, `Assets/Settings`, `Assets/TutorialInfo` y el asset de input se
conservan en sus ubicaciones actuales para mantener sus GUID y referencias.

| Carpeta | Responsabilidad |
| --- | --- |
| `Scripts/Domain` | Estado y reglas en C# sin dependencias de Unity. |
| `Scripts/Runtime` | Componentes de Unity, input, física, vistas y composición. |
| `Scenes` | Escenas propias, comenzando por el futuro `PrototypeRestaurant`. |
| `Prefabs` | Objetos reutilizables con primitivas y componentes. |
| `ScriptableObjects` | Datos de configuración compartidos, sin estado de partida. |
| `Materials` | Materiales simples de color compatibles con URP. |
| `Tests/EditMode` | Pruebas de reglas y validación de datos. |
| `Tests/PlayMode` | Pruebas de integración con escenas y componentes. |
| `Editor` | Herramientas que solo se ejecutan en el Editor. |

Las carpetas vacías contienen `.gitkeep` para que existan también tras clonar.
M1 incluye `Scenes/PrototypeRestaurant.unity`, tres materiales greybox, controlador
FPS en `Scripts/Runtime/Player`, generador de escena en Editor y tests EditMode y
PlayMode. Domain, Prefabs y ScriptableObjects siguen reservados para tareas futuras.
Los tests ya tienen sus assemblies; no añadir NUnit al código Runtime.

Consulta `README.md`, `AGENTS.md` y `Docs/ROADMAP.md` en la raíz antes de trabajar.
