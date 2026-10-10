# Integración de M14–M16 en main

Fecha: 2026-10-08. Solicitud: «sube todo a main».

## Estado integrado

Main incorpora M14, M15 y M16 desde `1609a29`. El merge `47799ca` conserva como
padres esa entrega y el antiguo main remoto `1c9186f`, sin reescribir historial.
El árbol del merge es exactamente igual al árbol validado de `1609a29`.
Los commits siguientes solo actualizan documentación de entrega/publicación.

Al consultar origin/main se detectaron los commits `0317f55` y `1c9186f`, que habían
retirado 517 archivos Unity e introducido Godot. El usuario confirmó explícitamente:
«lo de godot fue un error mío. Este repo solo debe tener cosas de Zero Star Restaurant».
Se restaura Unity/M16 como contenido de main; los archivos Godot accidentales no
permanecen en el árbol actual. Se conservan ambos commits en el historial del merge
y una referencia local `backup/main-before-m16-1c9186f`. No force push ni borrado de
historia. La estrategia de merge conserva el árbol Unity validado deliberadamente.

## Carpeta real para Unity

Abrir **`C:/Users/Usuario/Zero Stars Restaurant M15`**, rama **main**, con M16.
Es la última carpeta observada en el Editor del usuario; el sufijo M15 es histórico.
Al sincronizar, Unity ya estaba cerrado y el árbol de trabajo no tenía cambios
versionados ni escena sin guardar en un Editor activo. SceneTemplateSettings
incidental se conserva sin añadirlo a Git. No se cierra ningún proceso del usuario.

La actualización de escena desde `3799710` añade solo RestaurantReputation y sus
referencias. Los 149 transforms y demás documentos originales se conservan;
materiales, prefabs, GUID y dependencias existentes no se regeneran. No Rebuild.
La carpeta original sin sufijo conserva sus cambios manuales de escena/materiales,
Recovery y settings, con su rama previa. Los demás worktrees no se sustituyen.

## Validación

La entrega M16 original pasó 333/333 EditMode y 290/290 PlayMode completos.
El merge conserva exactamente ese código, assets y configuración; no añade gameplay.
En la carpeta M15 actual se ejecutan, con salida 0, cero fallos y cero omisiones:

- **333/333 EditMode completos**, XML `TestResults/M16MainEditMode.xml`, log
  `Logs/M16MainEditMode.log`.
- **10/10 PlayMode de jornada/M16**, con filtro M11RestaurantDayTests: seis casos
  M16 y cuatro regresiones de jornada. XML `TestResults/M16MainPlayMode.xml`, log
  `Logs/M16MainPlayMode.log`. No se cuenta este pase enfocado como suite completa.
- Importación/compilación correctas; sin errores C# ni scripts perdidos en esos logs.
- `Build/M16MainAudit.json`: código/assets/Packages/ProjectSettings idénticos a
  `1609a29`; 149 transforms intactos, 697 documentos anteriores conservados y
  un único componente añadido. Godot fuera del árbol actual y conservado en historial.

Publicación autorizada en `origin/main`, repositorio
`https://github.com/clauubueyes/Zero-Stars-Restaurant.git`, mediante push normal.
La integración contiene el main remoto anterior como ancestro y permite avance
sin force push. Main queda abierto en el worktree M15 para evitar otra confusión
de carpeta/versión; el worktree Fixes conserva su estado anterior en HEAD separado.

Pendiente: aceptación visual/manual al reabrir esta carpeta. No build standalone.
La prueba manual de incidente y las reglas/configuración están en [M16](M16.md).
