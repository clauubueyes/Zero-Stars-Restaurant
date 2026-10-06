# Auditoría inicial

Inspección del 6 de octubre de 2026. Los datos proceden de los archivos del
proyecto; no se dedujeron de una plantilla o de la versión del Hub.

## Editor y configuración

| Elemento | Valor observado | Fuente |
| --- | --- | --- |
| Editor | 6000.5.3f1, revisión c2eb47b3a2a9 | `ProjectSettings/ProjectVersion.txt` |
| Pipeline | Universal Render Pipeline, 17.5.0 | manifest, lock y Graphics/Quality Settings |
| Pipeline global y calidad PC | `Assets/Settings/PC_RPAsset.asset` | GUID 4b83569d67af61e458304325a23e5dfd |
| Calidad activa | PC, índice 1 | `QualitySettings.asset` |
| Calidad Mobile | `Assets/Settings/Mobile_RPAsset.asset` | GUID 5e6cbd92db86f4b18aec3ed561671858 |
| Color | Linear, valor serializado 1 | `ProjectSettings.asset` |
| Entrada activa | Input System, `activeInputHandler: 1` | `ProjectSettings.asset` |
| Actions compartidas | `Assets/InputSystem_Actions.inputactions` | referencia en `EditorBuildSettings.asset` |
| Serialización | Force Text, `m_SerializationMode: 2` | `EditorSettings.asset` |
| Control de versiones | Visible Meta Files | `VersionControlSettings.asset` |
| Escena habilitada para build | `Assets/Scenes/SampleScene.unity` | `EditorBuildSettings.asset` |
| Product Name | `Zero Stars Restaurant` | `ProjectSettings.asset` |
| Company Name / versión producto | DefaultCompany / 0.1.0 | `ProjectSettings.asset` |
| Namespace raíz generado | Vacío | `EditorSettings.asset` |

El nombre conceptual es **Zero Star Restaurant**. El nombre de carpeta y Product
Name preexistentes usan **Zero Stars Restaurant**; se conservan para evitar una
modificación incidental de configuración. La convención del código nuevo será
`ZeroStarRestaurant`; no se ha cambiado el namespace del generador del IDE.

## Paquetes directos

Versiones verificadas en manifest y lock (profundidad 0):

| Paquete | Versión | Uso en esta fase |
| --- | --- | --- |
| `com.unity.render-pipelines.universal` | 17.5.0 | Pipeline existente, conservar. |
| `com.unity.inputsystem` | 1.19.0 | Entrada para el futuro jugador. |
| `com.unity.test-framework` | 1.7.0 | Pruebas al implementar reglas. |
| `com.unity.ai.navigation` | 2.0.13 | Instalado; el primer cliente será estático. |
| `com.unity.ugui` | 2.5.0 | Disponible para feedback mínimo. |
| `com.unity.collab-proxy` | 2.12.4 | Preexistente; no sustituye el flujo Git. |
| `com.unity.ide.rider` | 3.0.38 | Integración IDE preexistente. |
| `com.unity.ide.visualstudio` | 2.0.26 | Integración IDE preexistente. |
| `com.unity.multiplayer.center` | 1.0.1 | Preexistente; multijugador fuera de alcance. |
| `com.unity.timeline` | 1.8.12 | Preexistente; sin animaciones nuevas. |
| `com.unity.visualscripting` | 1.9.11 | Preexistente; gameplay previsto en C#. |

También hay módulos integrados de Unity a versión 1.0.0. Las dependencias
transitivas están registradas en `Packages/packages-lock.json`, que se versiona.
No se añadieron, retiraron ni actualizaron paquetes.

## Assets y código existentes

- `SampleScene`: Main Camera, Directional Light (intensidad 2), Global Volume y
  componentes URP. No hay restaurante, jugador ni entidades de gameplay.
- `Assets/Settings`: pipelines PC/Mobile, renderers, perfiles de Volume y ajustes
  globales URP. Sus referencias coinciden con los GUID en Graphics/Quality Settings.
- `InputSystem_Actions`: mapas Player y UI. Player incluye Move, Look, Interact y
  otras acciones de plantilla. **Interact usa Hold**; M2 deberá decidir y comprobar
  si cambia a pulsación simple en una tarea dedicada. Sin wrapper C# generado.
- Únicos scripts C# propios de Assets: `Readme.cs` y `ReadmeEditor.cs`, del tutorial
  URP. No hay lógica de negocio que migrar.
- `Assets/Readme.asset`, icono URP y layout de tutorial preexistentes conservados.
  No se ha añadido arte nuevo ni se ha reestructurado la plantilla.

## Git y estado local

No existía `.git` dentro del proyecto. Se inicializó un repositorio independiente,
con la identidad Git disponible, sin cambiar la identidad global ni añadir remoto.
El commit inicial en `main` conserva los fuentes existentes con `.gitignore` y
`.gitattributes`. La estructura y documentación se incorporan en
`feature/project-foundation`.

El sandbox creó `.git` con un propietario distinto al usuario de Windows. Se
añadió a la configuración global de Git del usuario una única excepción
`safe.directory` para `C:/Users/Usuario/Zero Stars Restaurant`, sin habilitar
confianza general para otras carpetas. Se comprobó que Git funciona desde ese
usuario sin pasar una excepción adicional en cada comando.

Se excluyen cachés Unity, ajustes locales, builds y archivos generados del IDE.
Se preserva `.vscode` local sin versionarlo. No se borraron cachés ni soluciones.
La normalización de finales de línea en Git no cambia la lógica o los assets.
Los espacios al final de campos YAML vacíos de Unity ya existían en la plantilla;
no se ha reformateado todo el proyecto para eliminarlos.

## Límites de la inspección

La versión exacta del Editor está instalada en este equipo. Se observaron procesos
Unity y `Temp/UnityLockfile`; el proyecto está abierto o bloqueado por un Editor.
No se lanzó una segunda instancia sobre la misma carpeta ni se cerró el Editor.
El Editor abierto importó `Assets/_Project` y generó sus `.meta`; se verificó la
importación en `Logs/Editor.log`, sin regenerar ni reemplazar ningún GUID existente.
No se añadieron scripts compilables; no se ejecutó una build, PlayMode ni Test
Runner en esta entrega. Ver `FOUNDATION.md` para las comprobaciones finales.
