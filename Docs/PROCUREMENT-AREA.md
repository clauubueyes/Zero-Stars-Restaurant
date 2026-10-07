# Procurement en una zona independiente

Rama `fix/procurement-separate-area`, desde `6810059`; implementación `5736d62`. Cambio localizado de
greybox: estación M8 fuera de la cocina, con la misma funcionalidad y referencias.

## Ubicación y acceso

Pequeño anexo al oeste del local. Banco `FoodTestBench` en `(−9,7; 0,4; 1,25)`;
OUTPUT en `(−9,7; 1,12; 1,5)`. Se llega desde el pasillo de cocina por un hueco
en WallWest centrado en `(−7,25; 2,1)` en XZ. El anexo tiene suelo sólido continuo
con el existente y tres paredes de primitivas con materiales ya disponibles.

El acceso tiene 1,8 m de ancho y 2,7 m de altura libre. Desde el spawn, ir al
oeste por el espacio que ocupaba Procurement, atravesar el hueco y situarse
delante del banco, aproximadamente `(−9,7; 0; 2,65)`. Comprar con E, retirar de
OUTPUT manteniendo clic y regresar por el mismo acceso hacia Prep/Storage/Grill.
El recorrido con comida cruza hacia Prep por `z ≈ 1,6`, dejando libre el frente
de Freezer al norte del acceso.
El acceso no utiliza la cola ni las puertas de clientes.

Se conservan la estación, sus tres botones, etiquetas, OUTPUT, clearance y banco;
se trasladan los cuatro ingredientes de fixtures ligados al banco, aún desactivados
por defecto. No se recrean componentes ni cambian sus referencias, productos,
precios, prefabs, saldo o simulación. Storage, Prep, Grill, Assembly, Delivery,
spawn, cola, rutas de cliente y geometría restante mantienen sus posiciones.

`M9RestaurantLayoutBuilder.ConfigureProcurementArea` realiza solo esta adaptación.
El menú **Zero Star Restaurant → Prototype → Move Procurement Outside Kitchen**
la aplica a una escena guardada y cerrada; no hace falta usarlo sobre la escena
entregada. Rebuild y Apply M9 llaman al mismo procedimiento y no devuelven la
estación a la cocina. Aplicarlo otra vez no duplica objetos ni mueve otras zonas.

## Validación

Unity 6000.5.3f1, URP 17.5.0, Input System 1.19.0 y Test Framework 1.7.0
comprobados. Instalación nativa, **196/196 EditMode** y **168/168 PlayMode**
correctos en la copia aislada `Build/HandoffValidation` el 2026-10-07, incluidos
transporte físico y regresiones. Ambos runners terminan con código 0. Resultados
en `TestResults/ProcurementAreaEditModeFinal.xml` y
`TestResults/ProcurementAreaPlayMode.xml`; logs en
`Logs/ProcurementAreaEditModeFinal.log` y `Logs/ProcurementAreaPlayMode.log`.
No se lanza otro Editor sobre la carpeta abierta del usuario.

La auditoría local `Build/ProcurementSceneAudit.py` conserva los **640 documentos
originales/fileIDs**, limita las modificaciones a transforms autorizados y añade
**32 documentos**: un grupo y seis primitivas. Mantiene exactamente los documentos
de scripts/referencias y el texto/orden de todos los documentos no afectados;
descarta dos escrituras incidentales de defaults existentes que Unity añade al
guardar. Meta y GUID de escena conservados. El código Runtime/Domain no cambia.
Los 672 documentos finales resuelven todas sus referencias locales. Los siete
archivos de escena, implementación y tests comparados con SHA-256 contra la
copia validada tienen contenido idéntico. `git diff --check` correcto.

Los tests comprueban suelo continuo y acceso con cápsula de 70 cm, retorno al
pasillo, rutas de trabajo y cliente, referencias intactas, idempotencia,
compra/pickup/transporte de la misma unidad, OUTPUT ocupado y saldo insuficiente.
Las pruebas históricas de fixtures se adaptan a la posición del banco trasladado.

## Comprobación desde Unity

1. Detener Play, guardar cualquier edición propia aparte y recargar
   `Assets/_Project/Scenes/PrototypeRestaurant.unity` desde disco. No sobrescribir
   la escena nueva con una copia antigua aún abierta. Revisar Console.
2. Play: caminar desde cocina al anexo oeste sin saltar ni cruzar la cola.
   Comprobar que frío, Prep, Grill y Assembly siguen en sus sitios.
3. Comprar Bun (0,35 €), Raw Beef Patty (0,80 €) y Cheese (0,25 €), retirando cada
   unidad. OUTPUT ocupado debe rechazar sin cobrar. Llevar comida de vuelta por
   el hueco y continuar el loop habitual. Precios y saldo funcionan como M8.
4. Revisar la comodidad del paso con comida sostenida, señal y botones. La
   aceptación visual/teclado/ratón queda pendiente; no se ejecuta una build.

## Archivos

Modificados:

- `Assets/_Project/Scenes/PrototypeRestaurant.unity`.
- `Assets/_Project/Editor/M9RestaurantLayoutBuilder.cs`.
- `Assets/_Project/Tests/EditMode/M9RestaurantLayoutTests.cs`.
- `Assets/_Project/Tests/PlayMode/M5AssemblyFocusTests.cs`, `M6PrototypeTests.cs`,
  `M7StoragePrototypeTests.cs`, `M8ProcurementTests.cs`.
- `README.md`, `Docs/M8.md`, `Docs/M9.md`, `Docs/ROADMAP.md`,
  `Docs/Decisions/0011-restaurant-layout-service-flow.md`.

Creado: este documento. Assets nuevos solo como objetos de escena; ninguna meta
nueva. Los tres materiales modificados y `_Recovery` previos se conservan fuera
de commits. Sin merge a main ni publicación, dependencias nuevas o tienda.
