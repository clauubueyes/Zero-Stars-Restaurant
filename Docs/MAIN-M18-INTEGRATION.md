# Integración M17–M18 en main

Autorizada por el usuario el 2026-10-09. Se integra M18 `0cdaab3`, incluidos los
tres commits M17 desde `1609a29`, con el main local/remoto `5b2890c`.

El merge conserva ambas historias y la recuperación previa del proyecto Unity.
Los conflictos están limitados a README y AGENTS: se conserva la descripción M18,
se identifica main como rama de entrega y se mantiene la documentación histórica
de la integración M16. Docs/M16.md y MAIN-M16-INTEGRATION.md se conservan.
No se retiran commits anteriores ni se utiliza force push.

## Proyecto entregado

Abrir **`C:/Users/Usuario/Zero Stars Restaurant M15`**, rama **main**, con **M18**.
Unity estaba cerrado al integrar. Los archivos versionados del proyecto entregado
no tenían cambios locales; SceneTemplateSettings.json incidental permanece fuera
del índice y sin modificar. La carpeta original sin sufijo y sus cambios manuales
de escena/materiales/Recovery se conservan.

## Verificación

Assets, Packages y ProjectSettings del merge son idénticos a M18 `0cdaab3`,
incluidas escena, materiales, GUID, scripts, tests y configuración. No se genera
ni reserializa la escena. La integración cambia documentación e historial.

Se verifica la evidencia de las suites completas ejecutadas en esta misma carpeta:
**384/384 EditMode y 323/323 PlayMode**, sin fallos ni omisiones, XML
`TestResults/M18FinalEditMode.xml` y `TestResults/M18FinalPlayMode.xml`.
No se repiten las suites al integrar porque su código y configuración no cambian.
La validación, reglas, inventario y prueba manual están en [M18](M18.md).

La publicación autorizada utiliza un push normal a `origin/main` en
`https://github.com/clauubueyes/Zero-Stars-Restaurant.git`; el merge contiene el
main remoto anterior como ancestro y permite avanzar sin reescribir historial.
La carpeta M15 queda en main con el mismo código M18 validado.

Pendientes: aceptación visual/de controles humana y build standalone. No M19.
