# Integración de M19 y estabilización en main

Entrega histórica en M15. La carpeta oficial posterior es
**C:/Users/Usuario/Zero Stars Restaurant**, en main; ver
[cambio de carpeta y validación](OFFICIAL-MAIN-PROJECT.md).

Autorizada por el usuario el 2026-10-10: «Primero quiero que me subas todo a Main».

Se integra la entrega `ec90c9d` con el main local/remoto `0cceab8`. Incluye M19,
VP1A, VP1B/C, polish de comida/suciedad, corrección de apoyo visual y estabilización
posterior a M19. Se conservan ambas historias y los merges anteriores de main,
incluida la recuperación histórica de Unity. No se reescribe historial.

## Contenido y conflictos

Main solo tenía seis documentos exclusivos respecto al ancestro común `0cdaab3`.
Los conflictos del merge quedan limitados a README y AGENTS; se combinan los
datos actuales de M19 con la documentación histórica de integración M16/M18.
Docs/M16.md, Docs/M18.md, MAIN-M16-INTEGRATION.md y MAIN-M18-INTEGRATION.md se
conservan. README y AGENTS identifican main como rama actual en la carpeta M15.

Assets, Packages, ProjectSettings, Tools y .gitignore quedan idénticos a `ec90c9d`.
No se modifica, reconstruye ni reserializa la escena validada. No cambia código,
assets, GUID, layout, colliders, triggers, rutas ni reglas M1–M19. No M20.

## Proyecto entregado y cambios locales

Abrir **C:/Users/Usuario/Zero Stars Restaurant M15**, rama **main**, con M19 y
estabilización. El sufijo M15 es histórico. Unity estaba cerrado al integrar;
los archivos versionados de esta carpeta estaban limpios. El archivo incidental
ProjectSettings/SceneTemplateSettings.json se conserva sin modificar/versionar.

La carpeta original sin sufijo mantiene sus cambios manuales de escena/materiales
y Recovery. Delivery, M12 y VP1BC mantienen sus cambios locales pendientes; Fixes
y M14 conservan sus settings locales. Ninguna carpeta antigua se elimina o se
sustituye. Sus commits de trabajo ya están en la historia de la entrega validada;
los cambios locales sin commit permanecen en sus propias carpetas.

## Validación y publicación

Se comprueban los XML de las suites completas ejecutadas en esta misma carpeta:
**398/398 EditMode y 350/350 PlayMode**, sin fallos ni omitidos. Evidencia:
TestResults/PostM19EditModeDelivery.xml y TestResults/PostM19PlayModeDelivery.xml.
La integración modifica documentación e historial; se verifica la igualdad de
todo el árbol ejecutable respecto a la entrega probada, sin repetir esas suites.

La auditoría de preservación y la repetición nativa del instalador permanecen
válidas: 256 transforms funcionales, 111 documentos de física, 645 archivos
protegidos; escena sin cambios al repetir. Ver POST-M19-STABILIZATION.md y
Build/PostM19/index.html para informe, inventario y capturas FPS antes/después.
Build/MainM19/Audit.json documenta la igualdad del proyecto y los resultados XML.

La publicación autorizada utiliza push normal a `origin/main` en
https://github.com/clauubueyes/Zero-Stars-Restaurant. El merge contiene el main
remoto anterior como ancestro; no requiere force push ni borrado de ramas.
La carpeta M15 queda en main y se comprueba la coincidencia con origin/main.

Pendientes de aceptación: recorrido humano con input real, HUD con scroll y
build standalone. Las capturas FPS no incluyen el overlay IMGUI; no se presentan
esas comprobaciones humanas como realizadas.
