# Trabajo diario y validación

## Git

`main` es la referencia estable. Empezar leyendo el estado y el historial:

```powershell
git status --short --branch
git log --oneline -5
git diff
```

Crear la rama de la tarea desde la base acordada. Si hay cambios locales, revisarlos
y conservarlos antes de cambiar de rama. No hacer stash, reset o limpieza automática
para conseguir un árbol vacío.

```powershell
git switch -c feature/player-controller feature/project-foundation
```

La foundation aprobada está en `feature/project-foundation`. M1 parte directamente
de ella, sin integrar en main. Para tareas posteriores, usar la rama o commit
aprobado que incluya sus dependencias; partir de main solo cuando ya las contenga.
No hacer merge o push salvo autorización de la tarea.
No hay remoto, CI ni protección de ramas del servidor configurados: la estabilidad
de main depende de seguir este flujo local hasta conectar un remoto.

Un commit debe contar un cambio concreto y comprobable. Ejemplos:
`feat: add first-person movement`, `fix: prevent duplicate order payment`,
`docs: record cooking state rules`. No mezclar controlador, cocina e IA de clientes.

```powershell
git add -- Assets/_Project/Scripts/Runtime/Player
git diff --cached --stat
git diff --cached
git diff --cached --check
git commit -m "feat: add first-person movement"
```

La ruta de Player del ejemplo se creará en M1; no existe aún. Añadir también los
metas de nuevas carpetas, assets y scripts. Los YAML generados por Unity pueden
incluir espacios tras campos vacíos; identificar ese origen antes de reformatear
un archivo entero. Revisar documentación/código propios sin esos espacios.

Para verificar exclusiones:

```powershell
git check-ignore -- Library/probe Temp/probe Logs/probe obj/probe
git ls-files Library Temp Logs obj UserSettings
```

El primer comando debe listar las cuatro rutas; el segundo no debe listar nada.
No usar exclusiones generales de `.meta`, `.asset`, `.dll` o modelos como sustituto
de excluir directorios generados: algunos de esos archivos pueden ser fuentes.
No se necesita Git LFS para este prototipo de primitivas.

## Comprobar la foundation en Unity

1. Usar Unity 6000.5.3f1. Si el Editor ya está abierto, esperar a que actualice el
   Project; usar **Assets > Refresh** si es necesario.
2. Buscar `_Project` y expandir Scripts/Domain, Scripts/Runtime, Scenes, Prefabs,
   ScriptableObjects, Materials, Tests/EditMode, Tests/PlayMode y Editor.
3. Abrir `Assets/Scenes/SampleScene.unity`: debe seguir teniendo Main Camera,
   Directional Light y Global Volume. Esta tarea no crea una escena de restaurante.
4. Revisar Console tras la importación; no debería haber errores introducidos por
   esta entrega, que solo añade documentación y metadatos.
5. En **Edit > Project Settings**, revisar Graphics y Quality: PC_RPAsset/URP y
   calidad PC. En Player, Active Input Handling debe ser Input System Package.
6. En Editor, Asset Serialization debe seguir en Force Text; en Version Control,
   Visible Meta Files. Estos ajustes ya estaban configurados y no se cambiaron.
7. Abrir **Window > General > Test Runner**: no esperar tests propios todavía.
   Las carpetas están reservadas; los tests reales se incorporan con sus assemblies.

Referencia de Unity para metas: [uso de control de versiones externo](https://docs.unity.cn/2022.1/Documentation/Manual/ExternalVersionControlSystemSupport.html).

## Validar los milestones de gameplay

Para M1, seguir [las instrucciones de jugador y greybox](M1.md). Hay pruebas
propias en ambos modos del Test Runner. El generador no se ejecuta automáticamente
al importar el proyecto y no cambia las escenas de Build Settings.

- Revisar los criterios de `ROADMAP.md` de la feature concreta y los errores de
  Console. Probar un caso válido y los rechazos que afecten al estado.
- Ejecutar los tests reales existentes en EditMode/PlayMode. Informar de los
  resultados y de las pruebas manuales realizadas por separado.
- Para automatización en batch, usar el Editor exacto, `-runTests`, una plataforma
  de tests apropiada y resultados/logs bajo `TestResults` o `Logs` (ignorados).
  No lanzar batch sobre esta carpeta mientras otro Editor la tenga abierta.
- Hacer una build solo cuando haya gameplay que validar y los módulos del destino
  estén disponibles. Documentar destino y errores; no asumir una build correcta
  a partir de la existencia de YAML o de los tests de reglas.
- Tras abrir Unity, revisar `git status` y explicar cualquier cambio en manifest,
  lock o settings. No incluir migraciones o actualizaciones incidentales en el
  commit de gameplay.

## Entrega de cada tarea

Indicar comportamiento final, archivos afectados, rama y commits, pruebas
ejecutadas, pasos de comprobación desde Unity y cualquier limitación pendiente.
Registrar decisiones duraderas en `Docs/Decisions`; mantener los milestones al día.
