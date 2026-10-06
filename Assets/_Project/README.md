# Contenido de Zero Star Restaurant

Todo el contenido propio del juego se incorpora aquí. La plantilla existente en
`Assets/Scenes`, `Assets/Settings`, `Assets/TutorialInfo` y el asset de input se
conservan en sus ubicaciones actuales para mantener sus GUID y referencias.

| Carpeta | Responsabilidad |
| --- | --- |
| `Scripts/Domain` | Estado y reglas en C# sin dependencias de Unity. |
| `Scripts/Runtime` | Componentes de Unity, input, física, vistas y composición. |
| `Scenes` | Escenas propias: `PrototypeRestaurant` para M1–M7. |
| `Prefabs` | Objetos reutilizables con primitivas y componentes. |
| `ScriptableObjects` | Datos de configuración compartidos, sin estado de partida. |
| `Materials` | Materiales simples de color compatibles con URP. |
| `Tests/EditMode` | Pruebas de reglas y validación de datos. |
| `Tests/PlayMode` | Pruebas de integración con escenas y componentes. |
| `Editor` | Herramientas que solo se ejecutan en el Editor. |

Las carpetas vacías contienen `.gitkeep` para que existan también tras clonar.
M1 incluye `Scenes/PrototypeRestaurant.unity`, tres materiales greybox, controlador
FPS en `Scripts/Runtime/Player`, generador de escena en Editor y tests EditMode y
PlayMode. Prefabs sigue reservado para tareas futuras.
M2 añade `Scripts/Runtime/Interaction`, cuatro materiales de cajas y extiende el
generador/escena de M1. Usa el asset de input existente con Interact, Drop y Throw.
Ver `Docs/M2.md` en la raíz.
M3 incorpora reglas en `Scripts/Domain/Food`, adaptadores en `Scripts/Runtime/Food`,
tres definiciones en `ScriptableObjects/Food`, tres materiales y una zona con cuatro
alimentos. Estado, Pickup y representación siguen separados.
Ver `Docs/M3.md` y ADR 0004 en la raíz.
M4 añade `Scripts/Domain/Cooking`, `Scripts/Runtime/Cooking`, plancha de primitivas,
cuatro carnes adicionales y un material de superficie. FoodSimulation es el único
driver temporal; fuentes térmicas solo describen el entorno por contacto físico.
Ver `Docs/M4.md` y ADR 0005 en la raíz.
M5 añade `Scripts/Domain/Dishes`, `Scripts/Runtime/Dishes`, dos definiciones de
reconocimiento en `ScriptableObjects/Dishes`, tres bandejas, diez provisiones y
un material de color. El plato final mantiene FoodItems/estados originales con
un proxy físico único. Ver `Docs/M5.md` y ADR 0006 en la raíz.
M6 añade `Scripts/Domain/Customers`, `Orders`, `Economy` y adaptadores en
`Scripts/Runtime/Customers` y `Orders`. `ScriptableObjects/Customers/CustomerService`
configura menú, precios y tiempos; dos materiales simples y `CustomerServiceZone`
añaden cliente de primitivas, ruta y pad de entrega. Sin generación automática de
comida ni otro reloj. Ver `Docs/M6.md` y ADR 0007.
Polish del slice desde M6 aprobado `9335899`: colocación rápida en AssemblySurface
con clic izquierdo, agarre del Dish desde cualquier parte y CustomerDishCarrier
transportando la misma venta hasta la salida. Greybox compacto Prep → Grill →
Assembly → Delivery, sin cambios Domain. Ver `Docs/VERTICAL-SLICE-POLISH.md` y
ADR 0008. Los tests ya tienen sus assemblies; no añadir NUnit al código Runtime.
M7 añade ColdStorage para Fridge/Freezer abiertos, FoodPreservationProfile en
Domain y FoodPreservationSettings en `ScriptableObjects/Food`. El único reloj
integra conservación por temperatura real durante enfriamiento/calentamiento;
Pickup e input se conservan. Ver `Docs/M7.md` y ADR 0009. Sin electricidad ni M8.

Consulta `README.md`, `AGENTS.md` y `Docs/ROADMAP.md` en la raíz antes de trabajar.
