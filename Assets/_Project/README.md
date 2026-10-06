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
No hay escenas, prefabs, materiales, código de gameplay ni tests nuevos todavía.
Las carpetas de tests necesitan un assembly de tests antes de añadir C# con NUnit;
las instrucciones están en `Docs/ARCHITECTURE.md` en la raíz del repositorio.

Consulta `README.md`, `AGENTS.md` y `Docs/ROADMAP.md` en la raíz antes de trabajar.
