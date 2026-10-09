# VP1B/C: Realistic Restaurant Materials, Architecture & Equipment

Base: VP1A validado `db39056`. Rama **`visual/realistic-restaurant-art-pass-1`**.
Proyecto entregado: **`C:/Users/Usuario/Zero Stars Restaurant M15`**, la carpeta
que tenía abierta el usuario al empezar. Guardó y cerró Unity antes de sincronizar.
La carpeta original `Zero Stars Restaurant` conserva sus cambios locales; el
worktree `Zero Stars Restaurant VP1BC` se usó para preparar el arte. La entrega
real y las suites finales se ejecutan en M15. No merge a main ni publicación.

Commits: `4e025f9` (arte incremental y materiales), `571dac8` (regresión,
preservación y capturas) y `d000f3b` (comparaciones de todas las tomas).
La documentación e inventario se entregan en el commit posterior de esta rama.

Visual/art pass exclusivamente, sobre el layout guardado actual. Dirección:
restaurante barato y envejecido, realismo indie atmosférico. Geometría modular
provisional y materiales URP propios, sin modelos/paquetes descargados, Blender,
shaders personalizados o nuevos sistemas. Ver [ADR 0026](Decisions/0026-restaurant-art-shells-and-functional-geometry.md).

## Objetos y arquitectura

**74 raíces `Visual_VP1BC` nuevas en escena**, como hijos de objetos existentes,
incluidas las fixtures alimentarias que ya estaban inactivas. Solo se ocultan
renderers anteriores; materiales originales y objetos funcionales se conservan.

| Objeto original | Representación añadida |
| --- | --- |
| Floor, ProcurementFloor | Baldosa gris de 25 cm, juntas y variación suave; acabado mate |
| WallNorth/South/East/West, extremos, linteles, paredes del anexo, KitchenDivider | Pintura crema envejecida, zócalos, remates y revestimiento inferior; azulejo de 20 cm en las paredes de cocina |
| MainVisualCeiling, ProcurementVisualCeiling VP1A | Panel barato de 50 cm, acabado industrial mate; mismas posiciones |
| GrillBase / GrillHotSurface | Cuerpo metálico, fascia y cuatro mandos, cajón de grasa, trasera, plancha oscura, campana/filtros/conducto |
| AssemblyWorkbench, CookingPrepBench, WorktopVolume, TableVolume | Encimera fina de acero, cuerpo cerrado/retranqueado, patas, juntas y tiradores; fixtures inactivas siguen inactivas |
| PrepSupport1–3 / TrayVisual | Acabado de acero en los apoyos fijos originales |
| Fridge / Freezer y Base/Top/Shelf/BackWall/LeftWall/RightWall | Esmalte comercial distinto, estantes metálicos, puerta decorativa fija junto al lateral, tirador, junta, rejilla de compresor y placa discreta |
| ServiceCounterVolume | Laminado marrón, encimera esmaltada, remate de acero, paneles, zócalo y placa ORDER PICKUP |
| DeliveryPad | Superficie metálica y borde fino; mismo soporte/DeliveryZone |
| Procurement BuyBun/BuyRawBeefPatty/BuyCheese/BuyPlate y OutputMarker | Botones esmaltados en su posición original, placas de producto/precio orientadas al acceso y texto sobre la salida metálica; conserva anexo fuera de cocina |
| ElectricitySwitch | Carcasa esmaltada y palanca roja decorativa; mismo interruptor |
| CleaningTool | Aspecto de esponja vieja, conservando su Pickup, forma física y controles |
| Food en escena y prefabs Bun/RawBeefPatty/Cheese; prefab Plate | Pan abombado con corte y semillas combinadas; Patty redondeada, queso fino y plato con borde |

Las etiquetas flotantes/markers de desarrollo quedan ocultos con Art ON; se
conservan sus textos, transforms y renderers para comparar. El HUD y los prompts
de interacción no cambian. No hay señalización nueva que cree acciones.

Props moderados: campana, conducto y filtros, un estante con cuatro recipientes,
soportes, conducción visible, enchufe, menú de pared barato, aviso ligeramente
torcido y rejilla de ventilación. Se montan en paredes o encima de la zona de
trabajo, dejando libres los apoyos físicos. Ninguno añade collider.

## Materiales y procedencia

**23 materiales URP/Lit, 53 texturas PNG de 512 × 512 y 125 meshes modestos**, más
un VolumeProfile de exposición. Assets bajo `Assets/_Project/Art/Restaurant`.
Texturas y geometría creadas localmente por `VP1BCArtAssets`, sin assets externos,
costes de proveedor, licencias de terceros o nuevas dependencias. El generador
no reemplaza assets ya existentes ni reedita el arte instalado.

| Familia | Materiales |
| --- | --- |
| Metal | BrushedSteel, DullGalvanized, SeasonedGriddle |
| Arquitectura | FloorTile, KitchenTile, OldCreamPaint, CeilingPanel |
| Laminado/madera | BrownLaminate, FadedWood |
| Plástico/esmalte | BlackRubber, CreamEnamel, FreezerEnamel, FadedRed |
| Props | Paper, Cardboard, OldSponge |
| Food / Plate | BunCrust, BunCrumb, RawPatty, CookedPatty, BurntPatty, CheeseSlice, PlateCeramic |

Albedo, normal y máscara de metallic/smoothness por material; mapas sRGB solo
para albedo, máscaras lineales y normales importadas como Normal Map. Smoothness
base entre 0,05 y 0,40, multiplicada por variación espacial 0,7–0,9. Metal de acero
0,60, galvanizado 0,60 y plancha 0,35; resto según su acabado. UV por metro en
revestimientos/cajas; mapas repetibles con ruido periódico suave, evitando bandas
diagonales regulares. Instancing habilitado y materiales/meshes compartidos.

El desgaste ambiental es moderado: tonos desparejos, juntas, grano, laminado,
esmalte, plancha curada y papel envejecido. **No consulta ni escribe DirtState**.
Las seis manchas por superficie de DirtSurfaceView M17 conservan material,
referencias y altura sobre el collider original. La captura 10 muestra su lectura
con el estado dinámico al 100 %.

Raw/Undercooked usan RawPatty; Cooked/Overcooked usan CookedPatty; Burnt usa
BurntPatty. FoodStageVisual solo lee la etapa y asigna un material compartido.
No cambia FoodState, CookingState, frescura, temperatura, contaminación o IDs.
La unidad sigue siendo la original al confirmar Dish, transportar y vender.
No es el Food Art Pass completo ni hay visuales de deterioro nuevos.

## Iluminación respecto a VP1A

- Se conservan las siete luminarias eléctricas, dos spots exteriores, transforms,
  intensidades, alcances, colores, bias, cuatro fuentes con sombras y consumo M12.
  **Cero luces nuevas**, bake, probes, realtime GI o modificación del pipeline.
- Un Volume global propio, prioridad 11, sobreescribe únicamente Post Exposure
  a **+0,85 EV**, frente a +0,25 de VP1A. Art OFF lo desactiva y recupera VP1A.
  Los perfiles/ajustes existentes quedan intactos.
- ACES, contraste, saturación, white balance, bloom (0,12), vignette y ambiente
  residual permanecen como VP1A. Acero menos reflectante y roughness moderada
  reducen brillos; no se suben las intensidades para compensar metales.
- Campana, labio, filtros y conducto decorativos no proyectan sombra sobre la
  fuente existente; el estante tampoco tapa el área de preparación. Los cuerpos,
  ingredientes y otros apoyos mantienen sombras; SSAO PC existente se conserva.
- Power OFF sigue apagando Light/emission mediante PoweredLightFixture y el
  mismo suministro. La exposición no enciende lámparas ni añade consumo.

## Preservación e instalación

Auditoría `Build/VP1BC/Audit.json`, con `python Tools/audit_vp1bc.py`:

- **255 transforms originales de escena preservados**: posición, rotación,
  escala, padre y orden previo de hijos. Se conservan también los **4 transforms
  raíz originales de prefabs**. Total: 259 transforms originales entre escena y
  los cuatro prefabs modificados.
- **111 documentos físicos originales de escena idénticos** y 8 de prefabs;
  colliders, triggers, Rigidbody, CharacterController, referencias y gameplay
  no se reserializan. Todas las APIs de M1–M18 y Domain quedan sin modificar.
- 1123 documentos originales de escena conservados; 1402 añadidos. Los únicos
  cambios previstos en documentos anteriores son hijos/SceneRoots y enabled de
  92 renderers sustituidos. Se conservan sus materiales, meshes y configuración.
- GUID y fileIDs originales de escena/prefabs estables; referencias locales
  resueltas, GUID de Assets únicos y `.meta` completos.
- 87 archivos protegidos de materiales originales, Packages, ProjectSettings y
  Assets/Settings conservan contenido frente a VP1A (normalizando CRLF de Git).
  No se sincronizan configuraciones incidentales o escenas de otros worktrees.

La escena ya está instalada: **no ejecutar Rebuild**. El menú
`Zero Star Restaurant > Visual > Install VP1B-C Restaurant Art (incremental)`
requiere guardar/salir de Play/cerrar PrototypeRestaurant y aplica únicamente
añadidos a la escena actual. Conserva documentos antiguos y añade referencias;
no copia una escena histórica sobre la del usuario. Una repetición conserva
el pass instalado y sus ajustes manuales.

`RestaurantArtPass` ofrece **Art Enabled** y menús contextuales ON/OFF en Play.
Restaura solo sus renderers y oculta los hijos nuevos; Volume propio desactivado
en OFF. No cambia iluminación eléctrica, estados, saldo, pedido, jornada, suciedad
o contaminación. Las compras nuevas mantienen la nueva forma de sus prefabs.

## Regresión y capturas

Ejecutado en la carpeta entregada **M15**, Unity **6000.5.3f1**, URP **17.5.0**,
Input System **1.19.0** y Test Framework **1.7.0**, sin actualizaciones:

- **389/389 EditMode**, cero fallos/omitidos, salida 0:
  `TestResults/VP1BCFinalEditMode.xml`, `Logs/VP1BCFinalEditMode.log`.
- **328/328 PlayMode**, cero fallos/omitidos, salida 0:
  `TestResults/VP1BCFinalPlayMode.xml`, `Logs/VP1BCFinalPlayMode.log`.
- Suites completas de movimiento, interacción/Pickup, Food, Cooking, frío,
  montaje, PASS/entrega, clientes, electricidad, higiene/limpieza, contaminación,
  pagos/calidad/reputación M14–M18. Tres casos nuevos cubren shells sin física,
  instalación/comparación/idempotencia con cambios manuales y visuales de Patty
  que conservan el estado original. El test M1 admite los meshes solo dentro del
  nuevo visual shell y mantiene su contrato para geometría funcional original.
- Auditoría de preservación, diff, metas y referencias. El error de rollback de
  compra M8 registrado por su prueba es intencionado y esperado.
- Instalación repetida en M15, salida 0 y hash de escena idéntico:
  `Build/VP1BC/Idempotence.txt`, `Logs/VP1BCIdempotence.log`.
- Capturas finales con Direct3D12 / NVIDIA GeForce RTX 5060 Ti, salida 0;
  `Logs/VP1BCFinalCaptures.log`, sin excepciones ni errores de compilación.

Capturas de **PlayerCamera durante Play**, con URP y gráficos reales, 1600 × 900,
mismo FOV y pose en cada pareja. Harness batch `VP1BCVisualValidation.Run`, sin
`-quit` porque termina su Editor al salir de Play. No ejecutarlo sobre una carpeta
abierta en otro Editor. No se usa Scene View, una cámara promocional o render offline.
El harness pausa drivers/input y coloca temporalmente cuerpos para tomas estables;
no guarda escenas/assets. Las tres Patty se compran y cocinan mediante los sistemas
reales, conservando IDs; ver `Receipt.txt`. No captura el HUD OnGUI.
Las referencias de Food/Plate ocultan temporalmente sus shells y muestran los
renderers originales del mismo prefab, sin recrear ni modificar las unidades.
Son **20 capturas: diez parejas antes/después**, incluida la suciedad M17.

Ruta base: **`Build/VP1BC/Captures/`** en M15. Galería local:
**`Build/VP1BC/Captures/index.html`**, generada con
`python Tools/create_vp1bc_gallery.py`.

| Toma | Arte | Referencia VP1A |
| --- | --- | --- |
| Cocina → Grill/Prep | 01-Kitchen-Grill-Prep-Art.png | 01-Kitchen-Grill-Prep-VP1A-Before.png |
| Cocina → PASS | 02-Kitchen-PASS-Art.png | 02-Kitchen-PASS-VP1A-Before.png |
| Clientes → Counter | 03-Customers-Counter-Art.png | 03-Customers-Counter-VP1A-Before.png |
| Almacenamiento | 04-ColdStorage-Art.png | 04-ColdStorage-VP1A-Before.png |
| Power OFF | 05-Power-OFF-Art.png | 05-Power-OFF-VP1A-Before.png |
| Grill de cerca | 06-Grill-Close-Art.png | 06-Grill-Close-VP1A-Before.png |
| Raw / Cooked / Burnt | 07-Raw-Cooked-Burnt-Patty-Art.png | 07-Raw-Cooked-Burnt-Patty-VP1A-Before.png |
| Procurement | 08-Procurement-Art.png | 08-Procurement-VP1A-Before.png |
| Primer Food/Plate pass | 09-Food-First-Pass-Art.png | 09-Food-First-Pass-VP1A-Before.png |
| DirtState M17 al 100 % | 10-M17-Dynamic-Dirt-Art.png | 10-M17-Dynamic-Dirt-VP1A-Before.png |

## Inventario y límites

Inventario exacto, incluyendo cada material/mesh/textura y `.meta`, en
[VP1BC-FILES.txt](VP1BC-FILES.txt). Creado: Art/Restaurant, RestaurantArtPass,
FoodStageVisual, VP1BCArtAssets, VP1BCArtBuilder, VP1BCVisualValidation, dos archivos
de tests, auditoría/galería y documentación. Modificado: PrototypeRestaurant,
cuatro prefabs Food/Plate, M1SceneTests, README, AGENTS, ROADMAP y ARCHITECTURE.
Capturas, backups, auditorías, logs y resultados permanecen ignorados en Build,
Logs y TestResults. SceneTemplateSettings incidental del usuario sigue sin versionar.

386 transforms y 311 renderers nuevos en escena, incluidas fixtures inactivas;
material sharing y semillas combinadas en un solo mesh. Ningún componente nuevo
de física, consumo, input o simulación. **Rendimiento en ms/FPS no medido**:
los recuentos no son un benchmark; Profiler y aceptación artística/de controles
humanas pendientes. No build standalone. Las formas redondeadas mantienen los
colliders de caja anteriores; no hay puertas móviles ni superficies comprables.
Los personajes y HUD provisionales se conservan. M19 queda pendiente.

## Comprobación desde Unity

1. Abrir **Zero Stars Restaurant M15** en la rama indicada, PrototypeRestaurant;
   esperar importación y revisar Console. No Rebuild.
2. Play empieza OFF. Encender el interruptor general existente con E. Recorrer
   Grill, Prep, PASS, counter, almacenamiento y anexo de Procurement.
3. Comprar Patty/Bun/Cheese/Plate, coger/soltar con los controles existentes,
   cocinar y comprobar rosa → marrón → negro; montar con F y entregar normalmente.
   Verificar pago, cliente con Dish original y salida. Guardar/recuperar en frío.
4. Ensuciar y limpiar con la misma esponja/controles M17; comprobar manchas y
   progreso. Repetir Raw Patty → Prep → Bun → Dish → Customer según M18.
5. Cortar/restaurar suministro; lámparas y emisión obedecen M12. Comparar Art
   ON/OFF desde RestaurantArtPass y Atmosphere por separado. No se reinicia sesión.
6. Revisar iluminación, lectura de alimentos, desgaste y rendimiento en Game/
   Profiler. Las pruebas automatizadas no sustituyen esta aceptación humana.
