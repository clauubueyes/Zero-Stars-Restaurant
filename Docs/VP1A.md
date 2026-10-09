# VP1A: Lighting & Atmosphere

Base de gameplay: M18 validado `0cdaab3`. Rama `visual/lighting-atmosphere-pass-1`.
Proyecto entregado: **`C:/Users/Usuario/Zero Stars Restaurant M15`**, la carpeta
confirmada en la entrega M18. El nombre de carpeta se conserva. La carpeta original
`Zero Stars Restaurant` sigue con sus cambios locales anteriores; no se sustituye.
Unity estaba cerrado al empezar. No merge a main ni publicación.

Es un visual pass sobre la escena actual. No implementa M19, VP1B, nuevos sistemas
de gameplay, materiales envejecidos completos ni arte definitivo. Ver
[ADR 0025](Decisions/0025-lighting-atmosphere-and-existing-power.md).

## Luces y geometría añadida

Una raíz nueva `AtmospherePass`, con `AtmosphereVisuals`, contiene siete luminarias
de dos tubos. Cada una usa una carcasa rectangular gris, dos tapas de plástico
apagado y dos cilindros Unity. Materiales nuevos propios, sin texturas. La fuente
spot está debajo de los tubos para no quedar tapada por la carcasa. Las posiciones
se derivaron de los soportes actuales; una repetición del instalador conserva las
posiciones e intensidades que edite el usuario.

| Luminaria | Zona | Intensidad Unity | Alcance | Sombras |
| --- | --- | ---: | ---: | --- |
| GrillFluorescent | Grill actual, ligeramente hacia el frente | 6 | 6,5 m | Soft |
| AssemblyFluorescent | AssemblyWorkbench / estación central actual | 6 | 6,5 m | Soft |
| ColdStorageFluorescent | Frente a los gabinetes actuales | 10 | 6 m | No |
| PrepFluorescent | CookingPrepBench actual | 16 | 6 m | Soft |
| PassFluorescent | DeliveryPad actual | 14 | 6 m | Soft |
| CustomerFluorescent | Entre atención y pared sur | 10 | 7 m | No |
| ProcurementFluorescent | Encima del OUTPUT actual del anexo | 10 | 5,5 m | No |

Las siete luces son realtime, spot de 140°, cono interior de 100°, RGB
`(0,96; 1; 0,97)`. Montaje a 3,33 m con las paredes actuales de 3,5 m; fuente a
3,21 m. No se colocan tubos repetidos arbitrariamente por todo el techo.
Emission del material de tubo: `(3,1; 3,2; 3,08)`, aplicada por instancia; OFF la
pone a cero sin editar el material compartido.

Dos spots exteriores representan luz fría residual de tarde/noche entrando por
las aperturas Entrance/Exit: intensidad 1,5, alcance 5 m, cono de 90°, RGB
`(0,74; 0,8; 0,88)`, sin sombras y sin dependencia eléctrica. No son lámparas
interiores que permanezcan encendidas durante un corte.

El greybox carecía de techo. Dos paneles nuevos, finos, oscuros y **sin collider**
cubren visualmente local y anexo usando los bounds de las paredes actuales. Son
apoyo provisional para las luminarias, no una redistribución arquitectónica.
Comparación OFF oculta estos paneles junto con las luminarias. Se mantienen todos
los objetos anteriores, sus materiales, jerarquías, colliders y transforms.

La Directional Light anterior se desactiva durante el pass. Conserva su transform,
color, intensidad y configuración originales para la comparación de desarrollo.

## Ambiente y postprocesado

`RestaurantAtmosphere.asset` configura un preset único de tarde/noche:

- Ambient Mode: Trilight; Sky `(0,5; 0,52; 0,515)`, Equator `(0,43; 0,455; 0,44)`
  y Ground `(0,29; 0,305; 0,295)`; intensidad 1. Valores revisados en capturas para
  conservar lectura en superficies de baja reflectancia sin corriente.
- Reflection Intensity: 0,25. Se conserva el entorno existente, sin probes realtime
  ni cubemaps nuevos.
- Fondo de cámara: `(0,035; 0,045; 0,06)`; fog desactivado.
- No reloj nuevo, ciclo solar ni suscripción por minuto a GameTime. El preset es
  configuración separada y puede sustituirse explícitamente en una fase futura.

Volume global propio, prioridad 10, perfil `RestaurantAtmosphereVolume.asset`:

| Efecto | Configuración |
| --- | --- |
| Tonemapping | ACES |
| Color Adjustments | Post Exposure +0,25 EV; Contrast +8; Saturation −8; filtro blanco |
| White Balance | Temperature −3; Tint −1 |
| Bloom | Intensity 0,12; Threshold 1,6; Scatter 0,35; Clamp 4; HQ filtering OFF |
| Vignette | Intensity 0,08; Smoothness 0,45 |

Se activa postprocesado en los datos URP de PlayerCamera, conservando FOV,
transforms, input y resto de configuración. Sin chromatic aberration, film grain,
motion blur, lens dirt ni filtros verdes fuertes. La exposición es fija.

La configuración **PC existente** ya usa renderer Deferred y SSAO: intensidad 0,4,
radio 0,3, Direct Lighting Strength 0,25. Se conserva íntegra; no se añade otro
SSAO ni se modifica el pipeline Mobile, GraphicsSettings o QualitySettings.

## M12 y comparación

Cada `PoweredLightFixture` tiene referencias explícitas al **mismo**
RestaurantElectricity, su Light y sus dos Renderer. Consulta `Supply.IsOn` y
actualiza luz/emission en LateUpdate cuando cambia el resultado. Si la fuente o
la luminaria se deshabilita, deja de emitir. No avanza tiempo, comida o energía.
No modifica RestaurantElectricity, HeatSource, ElectricalAppliance ni sus reglas.

La escena sigue empezando **sin corriente**. E en el interruptor general existente
enciende las siete luminarias; un corte apaga luz y emisión en el siguiente frame.
El ambiente residual y las aperturas exteriores conservan orientación básica.
OFF individual de Grill/Fridge/Freezer sigue controlando únicamente ese aparato.

No se añade consumo de iluminación: el listado/meter M12 sigue con sus tres
aparatos térmicos, 2350 W a plena carga. Su contrato exige HeatSource y ampliar
la facturación no sería un cambio de presentación. El consumo de lámparas queda
pendiente, sin inventar otro sistema eléctrico ni una factura específica.

En Play, seleccionar `AtmospherePass` y cambiar **Atmosphere Enabled**, o usar los
menús contextuales **Development: Atmosphere ON / OFF**. OFF oculta exclusivamente
la raíz visual nueva, desactiva el Volume y restaura los campos originales de
ambiente, cámara y Directional Light. ON reaplica el pass y consulta el suministro
actual: cortar corriente durante la comparación no enciende los fluorescentes
al volver. Tampoco cambia selecciones individuales, medidores, saldo, comida,
suciedad, contaminación, cola ni jornada. No hay tecla nueva ni opción de jugador.
El referente greybox tenía iluminación independiente de electricidad; recuperarla
en la comparación no equivale a encender M12.

## Rendimiento

Nueve spots realtime añadidos; cuatro con sombras Soft de tier Medium, **512** en
el PC_RPAsset actual. Atlas adicional existente: 2048; Shadow Distance existente:
50 m. Bias local 0,03, Normal Bias 0,15, Near Plane 0,1. Las otras cinco luces no
generan mapas de sombras. La luz direccional anterior queda desactivada durante
el pass. Cero luces baked/mixed, cero bake, cero realtime GI y cero probes nuevos.

37 MeshRenderers nuevos (35 piezas de luminarias y dos techos), primitivas y
materiales compartidos. No se añaden colliders, rigidbodies ni consultas de física
por frame. Siete comprobaciones de presentación por frame y escrituras de
MaterialPropertyBlock solo al cambiar ON/OFF; sin instanciar materiales.
Bloom y postprocesado moderados añaden trabajo de GPU; SSAO ya existía.

La carga es acotada para este local pequeño. **No se ha medido un coste en ms o
una cifra de FPS**; los recuentos anteriores no son un benchmark. Perfilado y
aceptación en la máquina del jugador siguen siendo necesarios para fijar un
presupuesto final de rendimiento.

## Instalación y preservación

La escena se entrega instalada; no ejecutar Rebuild. El menú
`Zero Star Restaurant > Visual > Install VP1A Lighting and Atmosphere (incremental)`
solo instala si falta el pass. Requiere guardar, salir de Play y cerrar
PrototypeRestaurant, para evitar sobreescribir una escena abierta.

El instalador abre la escena actual, añade componentes y serializa una copia
temporal propia. Conserva los bloques originales de la escena y solo combina los
campos previstos: RenderSettings de ambiente, enabled de la luz direccional,
background de la cámara, postprocesado y la raíz nueva. La copia temporal propia
se elimina al terminar. No usa una escena histórica como fuente.

Auditoría `Build/VP1A/Audit.json`: **912 documentos originales conservados,
211 añadidos; 200 transforms originales idénticos**. Solo cinco documentos
originales cambian, incluyendo SceneRoots. Colliders/Rigidbodies y referencias de
gameplay originales son idénticos; nuevas piezas sin física. Referencias locales
resueltas. Materiales existentes, prefabs, input, Packages, ProjectSettings,
pipeline y GUID originales se conservan. Únicamente se crean cuatro materiales
del pass; no se modifica ninguno existente.

## Archivos exactos

Todos los archivos nuevos de Assets incluyen `.meta`; la carpeta nueva de
materiales también tiene su `.meta`.

Creado bajo `Assets/_Project`:

- `Scripts/Runtime/Presentation/PoweredLightFixture.cs`.
- `Scripts/Runtime/Presentation/RestaurantAtmosphere.cs`.
- `Scripts/Runtime/Presentation/RestaurantAtmosphereSettings.cs`.
- `Editor/VP1ALightingBuilder.cs` y `Editor/VP1AVisualValidation.cs`.
- `Materials/Atmosphere/AtmosphereCeiling.mat`, `FluorescentHousing.mat`,
  `FluorescentEndCaps.mat`, `FluorescentTube.mat`.
- `ScriptableObjects/RestaurantAtmosphere.asset` y `RestaurantAtmosphereVolume.asset`.
- `Tests/EditMode/VP1ALightingSceneTests.cs` y `Tests/PlayMode/VP1ALightingTests.cs`.

Modificado bajo `Assets/_Project`:

- `Scenes/PrototypeRestaurant.unity`.
- `Scripts/Runtime/ZeroStarRestaurant.Runtime.asmdef` y
  `Editor/ZeroStarRestaurant.Editor.asmdef`: referencias a assemblies URP/Core ya instalados.
- `Tests/EditMode/ZeroStarRestaurant.Tests.EditMode.asmdef` y
  `Tests/PlayMode/ZeroStarRestaurant.Tests.PlayMode.asmdef`: mismas referencias para pruebas.
- `Tests/EditMode/M1SceneTests.cs`: mantiene la exigencia de primitivas/materiales
  URP sin texturas; permite cilindros exclusivamente en Tube de una PoweredLightFixture.

Documentación: creados `Docs/VP1A.md` y ADR 0025; modificados `README.md`,
`AGENTS.md`, `Docs/ROADMAP.md` y `Docs/ARCHITECTURE.md`.
Resultados, capturas, logs, auditoría y backups quedan ignorados bajo
`TestResults`, `Logs` y `Build`. SceneTemplateSettings incidental no se versiona.

## Validación ejecutada

En la **carpeta entregada M15**, Unity 6000.5.3f1, paquetes y pipeline existentes:

- **387/387 EditMode**, cero fallos/omitidos, salida 0; XML
  `TestResults/VP1AFinalEditMode.xml`, log `Logs/VP1AFinalEditMode.log`.
- **327/327 PlayMode**, cero fallos/omitidos, salida 0; XML
  `TestResults/VP1AFinalPlayMode.xml`, log `Logs/VP1AFinalPlayMode.log`.
- Siete casos nuevos, junto con todas las regresiones M1–M18: suministro/emisión
  durante cortes repetidos, disable/enable, comparación sin alterar medidores,
  selección, FoodState, ledger, GameTime, Dirt o contaminación; instalación
  incremental/idempotente que conserva cambios manuales, referencias y física.
- Regresiones existentes de interacción/Pickup, Food/cocción, Grill, frío,
  montaje/entrega, clientes, jornada, electricidad/costes y limpieza/contaminación.
  Incluye la cadena física real M18 y sus consecuencias M14–M16.
- Repetición del instalador desde batch, salida 0: SHA-256 de la escena idéntico
  antes/después. Auditoría de 200 transforms y de todos los documentos existentes;
  GUID únicos, metas completas y referencias de assets/escena resueltas.
- Hashes conservados de 91 archivos protegidos existentes: materiales, prefabs,
  input, pipeline, Packages y ProjectSettings. Sin cambios incidentales versionados.
- Importación/compilación y capturas URP con **Direct3D 12 / NVIDIA RTX 5060 Ti**.
  Sin errores C# ni excepciones inesperadas en las pasadas finales. El rollback
  M8 conserva su error intencionado y esperado por la prueba.

La calibración visual subió ambiente residual y amplió los haces respecto al
primer intento oscuro; redujo la intensidad cerca de la pared norte para evitar
clipping. Los filtros siguen moderados. Capturas a 1280×720 en
`Build/VP1A/Captures`: Kitchen Atmosphere PowerOn/PowerOff y Reference;
Customers Atmosphere PowerOn y Reference; Procurement Atmosphere PowerOn;
CookingReadability PowerOn; Customers Service PowerOn. El harness usa las compras
y cola reales, con drivers pausados y colocación temporal para una captura estable;
no guarda escenas ni cambia assets. `VP1AVisualValidation.Run` está reservado a
batch con gráficos y se ejecuta sin `-quit`, pues termina su propio Editor después
de salir de Play. No iniciarlo sobre una carpeta abierta en otro Editor.

La pasada EditMode preliminar detectó la restricción histórica de cubos y una
aserción Count sobre un array. La primera ahora admite solo los tubos autorizados;
la segunda consulta Count de IReadOnlyList directamente. Las suites finales
anteriores pasan completas. La aceptación artística/de controles y la build
standalone siguen pendientes; ninguna ejecución automática se presenta como
validación humana.

Commits de implementación:

- `6076d1f`: luminarias/M12, ambiente/Volume, geometría visual nueva, instalación
  incremental, capturas y ajuste acotado del contrato de primitivas M1.
- `695c8a2`: siete casos de iluminación/preservación y referencias de tests a URP/Core.
- El commit documental posterior registra este inventario y la validación final.

Al entregar Unity está cerrado y la carpeta M15 contiene la implementación,
escena y configuración comprobadas, en la rama solicitada. No queda pendiente
sincronización a esa carpeta; abrirla para aceptación. Main y los cambios locales
del proyecto original se conservan.

## Validación visual manual

1. Abrir la carpeta **Zero Stars Restaurant M15**, rama del visual pass, con
   Unity 6000.5.3f1; abrir PrototypeRestaurant y revisar Console.
2. Play: comienza OFF como M12. Observar techo, tubos apagados, luz ambiental y
   caída hacia rincones. Ir al interruptor general norte y pulsar E.
3. Recorrer Grill, Assembly, CookingPrepBench, Fridge/Freezer, PASS, clientes y
   Procurement. Los tubos encendidos deben corresponder a zonas iluminadas;
   confirmar ingredientes/utensilios legibles sin linterna.
4. Cortar/restaurar suministro. Deben apagarse/encenderse los siete spots y su
   emisión, conservando luz exterior/ambiental tenue y las selecciones térmicas.
5. Comprar, recoger, cocinar, montar con F y servir un pedido normalmente; comprobar
   pago, salida y avance de la cola. Guardar/retirar comida en Fridge/Freezer.
6. Ensuciar y limpiar una superficie; probar la cadena sanitaria M18 habitual.
   Comprobar que luz y filtros no ocultan el ingrediente o el feedback de limpieza.
7. Alternar Atmosphere ON/OFF desde el Inspector de AtmospherePass. Cortar corriente
   con OFF y regresar a ON: las luces eléctricas deben seguir apagadas.
8. Verificar sombras de apoyo y que ninguna estación importante resulte oscura o
   deslumbrante. Confirmar la dirección artística humana y rendimiento con Profiler.

Las capturas automatizadas permiten revisar composición/luz, pero no sustituyen
esta prueba con teclado/ratón ni la aceptación artística del usuario. Build
standalone pendiente. No materiales/texturas finales ni luz horaria dinámica.
