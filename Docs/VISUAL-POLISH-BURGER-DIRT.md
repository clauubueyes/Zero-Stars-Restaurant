# Visual polish: Hamburger y Dirt

Informe histórico del pass. La restricción de compactación solo después de F y
el apoyo en COLLECT HERE se corrigen en el [bugfix desde b023fe8](FOOD-VISUAL-PLACEMENT-REGRESSIONS.md),
sin alterar las proporciones aprobadas ni Dirt. Consultar ese informe para la versión actual.

Base: VP1B/C validado `e4fb2fd`. Rama `fix/visual-burger-dirt-polish`.
Implementación `f0bb39a`; pruebas, auditoría y harness de capturas `c4dae0e`.
Entrega real en **`C:/Users/Usuario/Zero Stars Restaurant M15`**; no había Editor
abierto al iniciar. Se conservan también los cambios locales de la carpeta
original `Zero Stars Restaurant`. No Rebuild, milestone nuevo, merge o publicación.

## Hamburger / Cheeseburger

`BurgerIngredientVisual`, añadido solo al shell de cada Bun/Patty/Cheese existente,
lee el mismo DishState confirmado y sus FoodState originales. No modifica reglas,
ingredientes, orden, poses físicas, colliders, Cooking, Delivery o M14–M18.
Top/bottom se distinguen por el orden que ya reconoce el juego.

| Pieza visual | Altura respecto al visual VP1B/C | Altura con el prefab actual |
| --- | --- | --- |
| Bottom bun | 20 %, mesh de corte plano y sin semillas | 4,4 cm |
| Top bun | 45 %, dome/semillas originales | 9,9 cm |
| Patty | 28 %, mismos Raw/Cooked/Burnt materiales | 3,36 cm |
| Cheese | 7 %, mismo material y mesh | aproximadamente 0,39 cm |

Diámetro al 95 % del anterior: Bun/Patty ~38 cm en este prototipo. Las capas se
colocan visualmente sobre la base física original, con ~1 mm de solape para evitar
huecos. Hamburger sin Cheese también se compacta. No se inventa un ingrediente,
se reemplaza FoodState o recrea DishState. FoodStageVisual sigue leyendo Cooking.

Los alimentos sueltos presentan menor grosor apoyándose en la cara inferior de
su collider. La compactación relativa del plato se aplica **al confirmar con F**;
antes, los apoyos/separación físicos históricos permanecen. Custom Dish conserva
su disposición. El proxy Dish sigue siendo conservador y alto, igual que antes:
este polish no cambia dimensiones lógicas o contacto para ajustarlos al arte.

## Dirt orgánico y progresión

Se reutilizan exactamente los **42 renderers/objetos de manchas anteriores**:
seis por cada una de las siete superficies M17. Cero GameObjects o colliders nuevos.
Cada renderer usa un mesh combinado de 26 marcas (104 vértices / 52 triángulos),
con grupos irregulares, escalas, giros y distribuciones distintas. No hay objetos
individuales por marca, material por instancia o nuevo estado de suciedad.

- Grill: grasa cálida, restos carbonizados, salpicaduras y marcas arrastradas.
- Prep/Assembly: restos pequeños, manchas/salpicaduras dispersas, menor brillo.
- Floor: marcas alargadas, suciedad gris/marrón y fragmentos dentro de su región.

El aspecto se selecciona con la fuente térmica y DirtKind de contacto M17
existentes. Semilla estable por superficie; variación entre seis meshes por acabado.
La vista solo lee **DirtState.Amount y AmountsByKind**: aparición escalonada suave,
opacidad y tamaño continuos; grasa añade brillo. No escribe DirtState o contaminación.

Clean (0–5 %) queda prácticamente limpio; Used activa pocas marcas débiles;
Dirty muestra varios grupos y residuos; Filthy incorpora seis grupos más densos.
No cubre toda la encimera de marrón. Limpiar reduce opacidad/tamaño y oculta todas
las manchas al llegar a cero. Los umbrales/rates de HygieneSettings no cambian.

Assets nuevos propios: **un atlas RGBA 512 × 512, tres materiales URP/Lit
transparentes y 19 meshes** (18 de Dirt y BottomBun). Alpha convencional sin
Preserve Specular evita reflejos rectangulares invisibles. Sin sombras decorativas,
shaders personalizados, assets externos, paquetes o cambios de luz/exposición.
Todos los materiales/texturas/meshes principales VP1B/C permanecen iguales.

## Preservación y regresión

`python Tools/audit_visual_polish.py` compara con la escena actual guardada y
`e4fb2fd`: **641 transforms originales de escena** y **11 transforms de los tres
prefabs** conservados íntegramente, incluidos todos los funcionales y visuales.
**111 componentes físicos de escena y seis de prefabs** idénticos. Se añaden solo
18 componentes de presentación en escena y uno por prefab. Los únicos campos
anteriores cambiados son referencias a esos componentes, los campos visuales de
DirtSurfaceView y meshes/materiales/sombras de sus 42 manchas.

**999 archivos protegidos** de Assets, Domain, gameplay Runtime, materiales/arte
anteriores, Packages y ProjectSettings mantienen contenido. GUID y fileIDs
originales estables, referencias locales resueltas y metas únicos/completos.
Resultado en `Build/VisualPolish/Audit.json`. El instalador incremental no reescribe
un polish instalado ni restaura posteriores ajustes manuales.

Pruebas finales ejecutadas en M15: **390/390 EditMode y 331/331 PlayMode**,
cero fallos u omitidas. XML en `TestResults/VisualPolishEditMode.xml` y
`TestResults/VisualPolishPlayMode.xml`; logs en `Logs/VisualPolishEditMode.log`
y `Logs/VisualPolishPlayMode.log`. Importación/compilación y capturas en el
proyecto real sin errores C# o excepciones. Reaplicar `VisualPolishBuilder.Install`
terminó con código 0 y SHA256 de escena idéntico; recibo en
`Build/VisualPolish/Idempotence.txt`, log `Logs/VisualPolishIdempotence.log`.
Cuatro casos nuevos verifican instalación/idempotencia con ajustes manuales,
Hamburger/Cheeseburger originales sin mutación de estados/proxy y Dirt progresivo
que desaparece al limpiar sin sanitizar. Los contratos M1/VP1B/C ahora permiten
solo estas representaciones visuales registradas; la física sigue siendo original.

## Capturas reales de primera persona

`VisualPolishValidation.Run` usa **PlayerCamera durante Play**, 1600 × 900,
ojo a 1,65 m, mismo FOV/pose antes y después. Compras, cocción FoodSimulation/Grill
y confirmación PhysicalDishAssembly reales. Pausa drivers y coloca unidades solo
para la toma estable; no guarda escenas/assets. No captura HUD OnGUI.
Las comparaciones restauran temporalmente los shells anteriores y los cubos M17,
manteniendo las mismas unidades y estados. Receipt conserva IDs/etapas/categorías.

Base: **`Build/VisualPolish/Captures/`** en M15. Galería `index.html` con 16 PNG:
capturadas en Direct3D12, NVIDIA GeForce RTX 5060 Ti, y revisadas visualmente.

| Vista | Después | Antes VP1B/C |
| --- | --- | --- |
| Hamburger | 01-Hamburger-Polish.png | 01-Hamburger-VP1BC-Before.png |
| Cheeseburger | 02-Cheeseburger-Polish.png | 02-Cheeseburger-VP1BC-Before.png |
| Grill Clean | 03-Grill-Clean-Polish.png | 03-Grill-Clean-VP1BC-Before.png |
| Grill Used | 04-Grill-Used-Polish.png | 04-Grill-Used-VP1BC-Before.png |
| Grill Dirty | 05-Grill-Dirty-Polish.png | 05-Grill-Dirty-VP1BC-Before.png |
| Grill Filthy | 06-Grill-Filthy-Polish.png | 06-Grill-Filthy-VP1BC-Before.png |
| Prep Dirty | 07-Prep-Dirty-Polish.png | 07-Prep-Dirty-VP1BC-Before.png |
| Floor Dirty | 08-Floor-Dirty-Polish.png | 08-Floor-Dirty-VP1BC-Before.png |

## Archivos y comprobación en Unity

Inventario exacto y `.meta` en [VISUAL-POLISH-FILES.txt](VISUAL-POLISH-FILES.txt).
Archivos visuales principales: BurgerIngredientVisual.cs, DirtSurfaceView.cs,
VisualPolishAssets.cs, VisualPolishBuilder.cs, Art/VisualPolish, tres prefabs Food y
PrototypeRestaurant. Harness, auditoría/galería, tests y documentación completan
la entrega. Build, Logs, TestResults y ajustes incidentales permanecen sin versionar.

Abrir M15 y PrototypeRestaurant; no ejecutar Rebuild. Encender corriente, comprar
y montar Hamburger con/sin Cheese; F confirma la silueta compacta. Cocinar, recoger,
entregar y conservar en frío con los controles existentes. Ensuciar Grill/Prep/suelo
o usar debug M17 para revisar las categorías; limpiar con la misma esponja/E.
La contaminación M18 debe permanecer después de limpiar. Revisar Game/Profiler:
capturas y suites automatizadas no sustituyen aceptación artística/de controles.
No se ha medido FPS ni generado build standalone. Ver [ADR 0027](Decisions/0027-burger-shell-compression-and-organic-dirt-overlays.md).
