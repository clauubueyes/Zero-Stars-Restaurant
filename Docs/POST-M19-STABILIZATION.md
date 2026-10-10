# Estabilización posterior a M19

Base `f6fb68c`; rama `fix/post-m19-visual-stats-regressions`.
La entrega descrita aquí fue en M15. La carpeta oficial posterior es
**C:/Users/Usuario/Zero Stars Restaurant**, en main; ver
[OFFICIAL-MAIN-PROJECT.md](OFFICIAL-MAIN-PROJECT.md).

Proyecto entregado: **C:/Users/Usuario/Zero Stars Restaurant M15**. Unity estaba
cerrado al iniciar; se trabaja y valida en esta misma carpeta. La carpeta original
Zero Stars Restaurant y sus cambios manuales permanecen intactos. Sin M20,
merge a main, publicación, paquetes nuevos ni cambios de reglas M1–M19.

## Causas visuales y reparación

| Defecto | Causa comprobada | Corrección exclusivamente visual |
| --- | --- | --- |
| Suelo del acceso a Procurement | Floor y ProcurementFloor tenían caras superiores a y=0 con 3,5404 m² de solapamiento. Las cuadrículas UV tenían orígenes distintos. | Recortar solo el shell del suelo del anexo hasta el borde occidental del shell principal; UV en metros alineadas con el suelo principal. Una única superficie por punto, sin offset vertical. |
| Líneas dentadas/parpadeo en bases y remates de paredes | Wainscot, Skirting y WainscotTrim eran cajas de 4 mm con caras exteriores coincidentes. | Revestimiento de 14 mm y remates de 30 mm: separación exterior real de 16 mm. Conservan alturas, materiales y referencias. |
| Banda negra sobre puertas y unión con techo | Wall() aplicaba zócalo/revestimiento inferior también a los tres linteles elevados; sus capas se solapaban. | Desactivar los renderers redundantes de esos linteles y conservar la viga pintada existente. No cambia el hueco físico, la altura ni el techo. |
| Material alternante en mostrador/PASS | CounterTop y Cladding tenían la misma cara superior en los 14 m² de encimera y también caras laterales superpuestas. | Terminar el cuerpo visual bajo CounterTop. Encimera esmaltada única a la altura física original. |
| Doble superficie en Prep/Assembly | TrayVisual y PrepSupport repetían 0,49 m² por apoyo con caras coincidentes. | Ocultar únicamente Cladding de los tres TrayVisual fijos. El apoyo, los colliders y las APIs de bandejas permanecen. |
| Etiquetas visibles a través de otros props | El shader de la fuente de TextMesh ignoraba el depth buffer; en Procurement Cheese se dibujaba sobre Plate aunque estuviese detrás. | RestaurantArtPass utiliza copias de material de fuente con UI/Default y ZTest LessEqual durante Play. Conserva atlas, texto, color y pose; restaura originales en Art OFF y libera las copias al deshabilitarse. |

Zonas reparadas: suelo del anexo/acceso; paredes norte, sur, este, oeste,
KitchenDivider, extremos y paredes de Procurement; uniones pared/suelo; linteles
de entrada, salida y acceso al anexo; mostrador junto a PASS; tres apoyos Prep.
Las etiquetas físicas de los shells, incluidos Procurement y cartel del menú,
respetan ahora la oclusión de la geometría. El shader built-in ya figuraba en
Always Included Shaders; no se modifica GraphicsSettings ni se añade un shader.
Grill, cámaras frías, techo, luminarias y props se auditan desde la cámara FPS.
No se detectan normales invertidas, materiales faltantes ni un fallo del clipping
que justifique cambiar cámara, pipeline, luces o estilo. Las intersecciones internas
de cuerpos de pared en esquinas quedan ocultas por sus caras interiores; no se
modifica la arquitectura física para resolverlas.

PostM19VisualFixBuilder modifica componentes existentes bajo Visual_VP1BC, sin
crear objetos/shells ni destruir assets. Su instalador mezcla únicamente campos
localPosition, mesh o enabled de los componentes aprobados, conservando los demás
documentos serializados. La segunda aplicación es vacía. No ejecutar Rebuild.
El menú incremental exige escena cerrada y fuera de Play; la escena entregada ya
contiene la reparación. La repetición no reconstruye el pass ni repone geometría
anterior sobre ajustes manuales.

## Reacciones, Exit y estadísticas

Se inspeccionó CustomerVisit.Resolve → TryAcceptDelivery → Statistics.TryRecord
→ Leave/Exit → RestaurantDayController.EndCurrentDay → EndOfDaySummary →
StartNextDay → OperatingCostsFeedback.

**No se encontró un bug de contabilidad ni de snapshot/reset.** Doce clientes servidos
pueden tener Satisfied=0, Unhappy=0, Complaints=0, HealthIncidents=12 si las doce
entregas contenían peligro sanitario. La captura no permite determinar la calidad
de cada comida; el test reproduce exactamente esos valores con comida peligrosa.

M15 conserva su reacción exclusiva Health Incident > Complaint > Unhappy >
Satisfied y todas las causas originales. TryRecord deduplica cliente y pedido.
El registro sigue ocurriendo al aceptar/consumir en Pay, como M15: Exit no añade
otro registro. El resumen solo admite ocupación cero después de todas las salidas.
Rechazos no consumidos no cuentan. No se trasladan ni recalculan las reglas.

El HUD de servicio diario, sesión y cierre agrupa ahora los cuatro outcomes bajo
Outcomes. Health incidents (food hazards) del cierre sigue siendo M15; confirmed,
dismissed y pending de M16 se muestran por separado. El panel fija el ancho del
contenido antes de calcular su altura y deja scroll vertical para datos largos;
no permite que recibos o IDs ensanchen el contenido fuera de la región. Las regiones
existentes se conservan, sin rediseño.

M19 ya tenía EndOfDaySummary inmutable: copia todos los contadores diarios y la
evidencia M16 en End Day, antes de cambiar a EndOfDay. El resumen no lee los
contadores vivos del día siguiente. Solo StartNextDay resetea diarios; el historial,
IDs, contadores de sesión y riesgos persisten. El resumen anterior permanece en
Summaries. Confirmar/descartar M16 no cambia retroactivamente reacción ni snapshot.
El HUD contable puede reflejar el pago explícito posterior de la factura M13 del
mismo día, como antes; ello no modifica el snapshot ni los outcomes capturados.

| Jornada determinista con flujo físico | Served | Satisfied | Unhappy | Complaints | Health incidents |
| --- | ---: | ---: | ---: | ---: | ---: |
| A | 4 | 1 | 1 | 1 | 1 |
| B | 3 | 3 | 0 | 0 | 0 |
| C | 3 | 0 | 0 | 0 | 3 |
| Sesión tras A+B+C | 10 | 4 | 1 | 1 | 4 |

Cada entrega pasa por PASS real, M14 (€2 por Patty parcial), CustomerVisit y Exit.
Se repiten Poll, Finish, TryRecord, CompleteVisit, End Day y Start Next Day para
comprobar deduplicación. Se fuerza una confirmación tras A y tres descartes tras C;
el texto de cierre y sus contadores se conservan. El caso Domain de doce peligros
conserva múltiples causas y verifica también snapshot/reset/histórico.

## Preservación y validación

`python Tools/audit_post_m19.py` compara con f6fb68c:

- 2544 documentos de escena, sin añadir/eliminar ninguno.
- 256 transforms funcionales idénticos, incluidos estaciones, PASS, Procurement,
  QueuePoints, rutas, arquitectura y objetos manuales actuales.
- 111 documentos de física idénticos, incluidos colliders/triggers y cuerpos.
- 645 archivos protegidos idénticos: Domain, prefabs, materiales originales,
  ScriptableObjects, Packages, ProjectSettings, ajustes URP y todos los assets
  anteriores de Art/Restaurant (incluidos meshes, texturas y sus metas).
- 157 componentes exclusivamente visuales modificados; GUID y referencias
  locales comprobados. Materiales/texturas y meshes anteriores no se sobrescriben.
- ProjectSettings/SceneTemplateSettings.json previo permanece sin versionar.

El instalador se repitió además en un Editor batch independiente y devolvió
«already installed; nothing changed». SHA-256 de la escena antes/después:
`4c21e34ddeba75f5f5765b101d2429401b22abc08219cd0f583c2a5a8e2d623a`.
Evidencia: Logs/PostM19InstallerRepeat.log y
Build/PostM19/DeliveryVerification.json; auditoría: Build/PostM19/Audit.json.

Unity 6000.5.3f1, URP 17.5.0, Input System 1.19.0, Test Framework 1.7.0.
EditMode final: 398/398, sin fallos ni omitidos, tras incluir la reparación de
etiquetas. XML: TestResults/PostM19EditModeDelivery.xml; log:
Logs/PostM19EditModeDelivery.log. PlayMode final: 350/350, sin fallos ni omitidos;
TestResults/PostM19PlayModeDelivery.xml y Logs/PostM19PlayModeDelivery.log.
Suites completas M1–M19 y visual passes, ejecutadas en el proyecto entregado.
Sin errores de compilación ni excepciones inesperadas. Las advertencias de APIs
FindObjectsByType obsoletas ya existían; el error de rollback M8 forma parte de
su test de rechazo y está verificado mediante LogAssert.

La nueva prueba PlayMode usa las etiquetas de la escena real: profundidad,
atlas de fuente, materiales compartidos intactos, reutilización de copias en
Art OFF/ON, conservación de poses/color/texto y liberación al deshabilitarse.
Las pruebas de layout existentes verifican regiones de HUD acotadas y sin
solapamientos a varias resoluciones; los nuevos casos comprueban el texto completo
de cierre y las cuatro categorías.

Commits de implementación:

- `5011b30` — superficies de arquitectura y reparación incremental.
- `8a00157` — cuatro outcomes, ancho/scroll HUD y regresiones A/B/C.
- `cdbd629` — profundidad de etiquetas físicas y ciclo de vida de materiales.

## Capturas y comprobación en Unity

Galería local: `Build/PostM19/index.html`. Antes/después con idéntica cámara,
FOV y resolución 1600×900, Power ON/OFF: 16 zonas × 2 estados × 2 versiones.
PlayerCamera real en Play, URP/D3D12; no Scene View ni cámara promocional.
Las poses se fijan temporalmente para comparación; no se guarda la escena.
Receipt.txt identifica GPU y método, Renderers.tsv registra bounds/materiales.
Las capturas corresponden al framebuffer de la cámara y excluyen el overlay
IMGUI del HUD. El contenido y layout se verifican en tests; queda pendiente la
aceptación visual humana del HUD con scroll dentro de Game View.

Incluye cocina, Prep/Assembly, Grill, almacenamiento, PASS, clientes/mostrador,
Procurement, suelo de acceso, lintel del anexo, entrada, salida, esquinas
nordeste/sudoeste, techo, KitchenDivider y unión de techos del anexo.

Repetir auditoría/galería: `python Tools/audit_post_m19.py` y
`python Tools/create_post_m19_gallery.py`. Capturar la versión actual con Unity
cerrado: Editor batch **con gráficos**, `-executeMethod
ZeroStarRestaurant.Editor.PostM19VisualValidation.Run -postM19Phase After`.
La herramienta entra en Play y termina su propio Editor; no necesita `-quit`.
El parámetro Phase solo nombra la carpeta, no restaura una escena anterior.
Las capturas Before corresponden a f6fb68c; repetirlas exige un checkout aislado
de esa base con la herramienta de captura, conservando intacta la escena actual.

1. Abrir **Zero Stars Restaurant M15**, rama de corrección, PrototypeRestaurant;
   esperar importación y revisar Console. No Rebuild ni instaladores anteriores.
2. Play; E en Power. Recorrer las zonas de la galería y mirar suelo, remates,
   dinteles y encimera desde varios ángulos. Repetir con corriente cortada.
3. Enter para abrir; servir Cooked, Overcooked, Burnt y peligros sanitarios.
   Comprobar cuatro outcomes y la suma con served; entregar/repetir Poll no duplica.
4. Force Close desde Inspector, terminar visitas hasta Exit, Enter para End Day.
   Resumen conserva Day N y las cuatro cifras. Enter para siguiente día: diarios
   cero, históricos conservados, sin recrear comida ni cargar una escena.

La inspección visual automatizada complementa la aceptación humana caminando con
input real. No se ejecuta build standalone ni se mide rendimiento/FPS.
Inventario exacto: [POST-M19-FILES.txt](POST-M19-FILES.txt), 93 archivos frente a
f6fb68c: 83 creados (incluidos 34 meshes y sus metas) y 10 modificados. Capturas,
logs, XML y auditoría son artefactos locales excluidos de Git, regenerables con
las herramientas documentadas. La carpeta M15 contiene la implementación final;
el Editor del usuario no estaba abierto y debe abrir esa carpeta para probarla.
