# Entrega de la base inicial

Preparación del 6 de octubre de 2026. Rama: `feature/project-foundation`.

## Resultado

- Proyecto inspeccionado: Unity 6000.5.3f1, URP 17.5.0, Input System 1.19.0 y
  Test Framework 1.7.0; configuración inicial documentada.
- Git inicializado; `main` conserva la plantilla original, con exclusiones de
  generados y normalización de texto. Estructura y plan en una feature.
- Carpetas propias bajo `Assets/_Project`, con metadatos generados por el Editor
  abierto y `.gitkeep` para las carpetas reservadas.
- Arquitectura mínima, convenciones, reglas para agentes, ADR y milestones M0–M8.
- No se implementó gameplay ni se añadieron código C#, assemblies, escenas,
  prefabs, materiales, tests o dependencias. Esos archivos se crearán en sus
  milestones, con criterios de aceptación definidos.

## Inventario exacto de archivos nuevos

En la raíz:

```text
.gitignore
.gitattributes
README.md
AGENTS.md
```

Documentación fuera de Assets (no requiere `.meta`):

```text
Docs/PROJECT_AUDIT.md
Docs/ARCHITECTURE.md
Docs/ROADMAP.md
Docs/WORKFLOW.md
Docs/FOUNDATION.md
Docs/Decisions/0001-prototype-foundation.md
```

Assets propios, incluidos todos los metadatos y marcadores:

```text
Assets/_Project.meta
Assets/_Project/README.md
Assets/_Project/README.md.meta
Assets/_Project/Editor.meta
Assets/_Project/Editor/.gitkeep
Assets/_Project/Materials.meta
Assets/_Project/Materials/.gitkeep
Assets/_Project/Prefabs.meta
Assets/_Project/Prefabs/.gitkeep
Assets/_Project/Scenes.meta
Assets/_Project/Scenes/.gitkeep
Assets/_Project/ScriptableObjects.meta
Assets/_Project/ScriptableObjects/.gitkeep
Assets/_Project/Scripts.meta
Assets/_Project/Scripts/Domain.meta
Assets/_Project/Scripts/Domain/.gitkeep
Assets/_Project/Scripts/Runtime.meta
Assets/_Project/Scripts/Runtime/.gitkeep
Assets/_Project/Tests.meta
Assets/_Project/Tests/EditMode.meta
Assets/_Project/Tests/EditMode/.gitkeep
Assets/_Project/Tests/PlayMode.meta
Assets/_Project/Tests/PlayMode/.gitkeep
```

Total: **33 archivos nuevos de proyecto**: 2 reglas Git, 2 documentos raíz,
6 documentos en Docs y 23 archivos de estructura/metadata de Assets. De estos,
31 están en la feature; los 2 archivos de reglas Git están en el commit inicial.
Los 61 archivos fuente preexistentes se incorporaron al commit inicial sin
reescribir su contenido, salvo la normalización de finales de línea en el índice.
No se modificaron ni borraron archivos preexistentes del proyecto.

También se creó el directorio interno `.git` y una entrada global de Git
`safe.directory` limitada al proyecto, necesaria por el propietario del sandbox.
No se cambió la identidad de autor, ni se configuró un remoto o un servicio externo.

## Commits

- `chore: capture existing Unity project with Git hygiene` — baseline en main.
- `chore: prepare project asset folders with Unity metadata` — estructura en feature.
- `docs: define prototype architecture and vertical slice milestones` — plan y
  reglas de trabajo en feature.

Los hashes se consultan con `git log --oneline --all`. No se ha hecho merge ni push.

## Comprobaciones

- Reglas de exclusión verificadas con `git check-ignore` para Library, Temp, Logs,
  obj, UserSettings y archivos generados del IDE; ninguno está en el índice.
- GUID de todos los `.meta` de Assets válidos, únicos y con su asset/carpeta;
  todas las entradas visibles de Assets tienen `.meta`. Los `.gitkeep` son
  marcadores ocultos para Git y Unity no los importa.
- Versiones directas de manifest y lock coincidentes; ningún cambio en Packages,
  ProjectSettings ni assets preexistentes frente a main.
- Enlaces Markdown locales revisados y diff de documentación sin errores de
  espacios. Los `.meta` mantienen el formato generado por Unity, con espacios
  en campos YAML vacíos; se excluyen del chequeo genérico de trailing whitespace.
- `Logs/Editor.log` confirma importación de las carpetas y README nuevos; no se
  encontraron `error CS`, `Compilation failed` ni `Scripts have compiler errors`
  en el log inspeccionado. Esto no equivale a una build ni a probar gameplay.

La revisión visual de la Console, abrir la escena, PlayMode, Test Runner y una
build no se ejecutaron desde esta sesión. No hay tests propios todavía. Se
conservó el Editor abierto por el usuario y no se inició otra instancia sobre el
proyecto bloqueado. La importación se observó en su log y archivos generados.

## Comprobar desde Unity

1. Abrir o refrescar el proyecto con Unity **6000.5.3f1**.
2. Expandir `Assets/_Project` y comprobar sus carpetas reservadas.
3. Abrir `Assets/Scenes/SampleScene.unity`: permanece la cámara, luz y Volume de
   la plantilla. No hay gameplay nuevo que ejecutar en esta entrega.
4. Revisar Console y los ajustes existentes de URP, Input System, Force Text y
   Visible Meta Files. [Pasos detallados](WORKFLOW.md).
5. Leer [el roadmap](ROADMAP.md). La siguiente tarea jugable es M1, controlador
   de primera persona y escena de primitivas, cuando se solicite su implementación.
