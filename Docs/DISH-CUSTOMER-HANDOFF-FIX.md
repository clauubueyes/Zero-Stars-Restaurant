# Corrección: transferencia del Dish independiente del jugador

Rama `fix/dish-customer-handoff`, desde `f700ab8`. Unity 6000.5.3f1, URP 17.5.0,
Input System 1.19.0 y Test Framework 1.7.0 comprobados. Sin nuevo milestone.
Implementación y pruebas en `f91f676`.
Ver [ADR 0015](Decisions/0015-dish-customer-transfer.md).

## Flujo

`Dish finalizado se suelta sobre DeliveryZone → detección física → cliente activo
recibe/evalúa → aceptación y transferencia síncronas → Pay → Leave con el mismo
agregado → limpieza de Dish/ingredientes/cliente en Exit`.

DeliveryZone ya era independiente de cámara e input; conserva su consulta de
colliders y filtros de apoyo, velocidad, huella, integridad y objeto suelto.
La corrección refuerza el límite con el lifecycle y el portador: TryReceive
valida antes de pagar y une el mismo objeto antes de resolver la visita. No se
ignora un bool de transferencia después de vender. Todos los cuerpos originales
quedan cinemáticos y sin interpolación/colisiones; sus poses nativas se sincronizan
con el cliente en cada paso del único servicio. El Dish vendido no es interactuable
ni revendible y sus alimentos siguen registrados hasta Exit.

La batería aislada de posiciones y miradas también pasaba en el código anterior:
no reproduce por sí sola el fallo manual comunicado. Esta entrega añade garantías
explícitas de transferencia y seguimiento físico; la aceptación visual del caso
original necesita comprobarse en Game.

## Tests

M6ServiceTests añade cuatro escenarios con el jugador de frente, a izquierda,
a derecha y mirando en dirección opuesta al cliente. Recoge el Dish, comprueba
que sostenido no se entrega y lo libera con ReleaseFromMouse. A partir de ahí
DeliveryZone lo procesa mediante FixedUpdate, sin llamar manualmente a TryDeliver
para aceptar la entrega.
Durante Pay/Leave el jugador se mueve y gira continuamente. Se comprueban las
poses de Transform/Rigidbody y rotaciones de cada ingrediente después de pasos
de física/render, identidad de FoodState, pertenencia al cliente, pago único,
bloqueo de recogida/reventa y destrucción de originales en Exit. Dos casos más
comprueban proxy deshabilitado y pago imposible por overflow sin transferencia.

M6PrototypeTests conserva las entregas centrada y parcial; añade entrega desde
ambos lados con mirada desviada tras soltar. Todos recorren la ruta real con
pasos físicos entre avances y comprueban ingredientes originales y destrucción
del cliente FIFO al llegar a Exit.

## Validación

Validación del 2026-10-07 con Unity 6000.5.3f1, en la copia aislada
`Build/HandoffValidation`, conservando el Editor del usuario sobre el original:

- **57/57 PlayMode** de servicio, escena real y cola FIFO: `TestResults/HandoffFocused.xml`.
- **194/194 EditMode** completos: `TestResults/HandoffEditMode.xml`.
- **168/168 PlayMode** completos, incluidos ocho casos nuevos y las regresiones
  M1–M11: `TestResults/HandoffPlayMode.xml`.

Los tres logs correspondientes están en `Logs/HandoffFocused.log`,
`HandoffEditMode.log` y `HandoffPlayMode.log`; todas las pasadas terminan con código
0, sin errores C# ni excepciones inesperadas. El test de rollback M8 emite su
error de configuración esperado. Unity registra diagnóstico inicial de handshake
de licencia y después resuelve los permisos de ejecución; la pasada acotada
también registra un aviso nativo de vida de asignación, sin fallo de tests.
Resultados/logs/copia están ignorados por Git. No se ha ejecutado una build.

`git diff --check` correcto; hashes SHA256 de los cuatro fuentes coinciden con
los ejecutados en la copia. Se conservan metas, GUID, Force Text y Visible Meta
Files. Las referencias DeliveryZone → Service y Service → portador/template →
anchor se revisan y las pruebas cargan la escena real sin reinstalación.
La prueba manual con teclado/ratón y la reproducción visual del caso original
quedan pendientes; la batería inicial de posiciones/miradas ya pasaba antes de
los cambios y no demuestra por sí sola la causa del fallo comunicado.

## Comprobación en Unity

1. Esperar recompilación con Play detenido y abrir PrototypeRestaurant. Revisar
   Console. No hace falta regenerar ni modificar referencias de escena.
2. Preparar un Dish que coincida con el pedido y soltar clic sobre el verde.
   Repetir desde centro, lado izquierdo y lado derecho en distintas visitas.
3. Tras soltar, mirar hacia cocina/suelo, girar y alejarse. Comprobar un solo
   cobro y que el mismo Dish completo quede fijado al cliente durante resultado,
   giros y salida. No debe poder recogerse. Desaparece junto al cliente en Exit.
4. Entregar un Dish incorrecto: cero ingreso, plato disponible, cliente sale sin
   comida. Recoger/retirar antes de volver a ofrecerlo al siguiente.
5. Window → General → Test Runner: ejecutar EditMode y PlayMode. La validación
   automática no sustituye la aceptación manual del ratón y representación.

## Archivos

Modificados:

- `Assets/_Project/Scripts/Runtime/Customers/CustomerDishCarrier.cs`.
- `Assets/_Project/Scripts/Runtime/Customers/CustomerServiceLoop.cs`.
- `Assets/_Project/Tests/PlayMode/M6ServiceTests.cs`.
- `Assets/_Project/Tests/PlayMode/M6PrototypeTests.cs`.
- `README.md`, `Docs/ROADMAP.md`.

Creados: este documento y `Docs/Decisions/0015-dish-customer-transfer.md`.
Sin cambios de escenas, prefabs, dependencias, settings, Domain, metas ni controles.
Los tres materiales modificados y `_Recovery` existentes se conservan fuera de
los commits. Sin integración en main ni publicación.
