# Proyecto oficial en main

El usuario establece el 2026-10-10 **C:/Users/Usuario/Zero Stars Restaurant** como
carpeta oficial, en **main**. M15 y los restantes worktrees quedan para desarrollo
y pruebas. El nombre de una carpeta antigua no identifica la última entrega.

## Migración y preservación

Base oficial: main `c8a9e6c`, con M19, todos los visual passes y estabilización
posterior `ec90c9d`. Antes de actualizar, Unity estaba cerrado y la carpeta original
estaba en docs/unity-latest-version `20614d6`, con una escena/materiales locales.

Se comparan los documentos de escena con el commit antiguo y con el main validado.
Las modificaciones funcionales de ExitWallEnd (posición/rotación/escala), Shelf,
DevelopmentSun, el componente URP añadido y los parámetros de interacción ya
estaban incorporados en main. Los tres materiales Greybox también coinciden.
Las otras diferencias locales eran orden/espacios de serialización. Se conserva
todo el tuning manual y se incorporan los hijos/componentes posteriores validados.
No se reconstruye ni se regenera la escena; no cambian layout, física o reglas.

Respaldo externo verificado por SHA-256:
**C:/Users/Usuario/Zero Stars Restaurant Backups/before-official-main-20261010-205946**.
Incluye escena, metas, materiales, Recovery, settings locales, patch y Manifest.json.
Respaldo Git de los cuatro archivos modificados: rama
`backup/user-unity-before-official-main-20261010`, commit `a327816`.
Recovery y SceneTemplateSettings.json se mantienen en su ubicación original sin
versionarlos ni publicar copias de recuperación. Los otros proyectos no se borran.

M15 libera main y vuelve a `fix/post-m19-visual-stats-regressions`, commit `ec90c9d`.
La carpeta original ocupa main. El código/assets/configuración ejecutables son
idénticos a la base `c8a9e6c`; esta promoción solo añade documentación de la carpeta
oficial. No se actualiza Unity, paquetes ni pipeline.

## Verificación en la carpeta oficial

Unity 6000.5.3f1, URP 17.5.0, Input System 1.19.0 y Test Framework 1.7.0.
Se ejecutan importación y suites completas en la carpeta original. Los resultados
se registran al terminar en TestResults/OfficialMainEditMode.xml y
TestResults/OfficialMainPlayMode.xml, con logs equivalentes en Logs.
Build/OfficialMain/Audit.json registra preservación, igualdad de árbol y hashes.
EditMode: **398/398** y PlayMode: **350/350**, cero fallos y cero omitidos,
ejecutados después de importar la versión actual en la carpeta oficial.
La auditoría confirma 2 transforms manuales, 4 documentos de componentes originales
y 11 archivos locales idénticos byte por byte, incluidos Recovery y settings.
La auditoría post-M19 confirma además 256 transforms funcionales, 111 documentos
de física y 645 archivos protegidos. La escena conserva su SHA-256 validado:
`4c21e34ddeba75f5f5765b101d2429401b22abc08219cd0f583c2a5a8e2d623a`.
Las capturas FPS de 16 zonas Power ON/OFF se generan en esta carpeta; la galería
Build/PostM19/index.html conserva las imágenes Before de la auditoría f6fb68c y
los frames After actuales. Excluyen el overlay IMGUI del HUD.

## Uso y entregas futuras

- Unity Hub: abrir la entrada existente que apunta a
  **C:/Users/Usuario/Zero Stars Restaurant**. No añadir otra carpeta para la versión
  oficial. Tras actualizar la lista debe indicar main.
- Trabajo nuevo: rama y worktree de desarrollo/pruebas. Conservar todos los cambios
  manuales actuales; no mover ni reconstruir arquitectura para sincronizar.
- Promoción autorizada: completar las comprobaciones necesarias y actualizar main
  en la carpeta oficial. Comprobar Editor, Play, cambios sin guardar, importación,
  referencias y errores antes de dar la entrega por actualizada.
- Main contiene la versión estable validada; aceptación humana de controles/HUD
  y build standalone son comprobaciones independientes que deben declararse.

La promoción a main y publicación mantienen el historial mediante avance normal;
no force push ni limpieza de carpetas antiguas.
