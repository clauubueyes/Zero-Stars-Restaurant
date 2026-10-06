# ADR 0007: cliente, entrega física y transacción de pago M6

Fecha: 2026-10-06. Estado: implementada; aceptación manual M6 pendiente.

## Contexto

M5 y feedback aprobados en `6a3ca72` conservan ingredientes concretos y reconocen
Hamburger/Cheeseburger. La petición de M6 integra ahora pedido, entrega, evaluación
y pago con un cliente activo. Sustituye la división M6/M7 anterior del roadmap y
su rechazo por calidad: pedido correcto paga incluso Raw/Burnt/Rotten/contaminado.
M7 no está autorizado. No introducir consecuencias sanitarias/económicas futuras.

## Decisión

- `CustomerVisit` valida el ciclo y referencia `OrderState`, que tiene su propio
  Guid y términos inmutables `OrderOffer`: DishProfile y precio configurado.
  Cliente y ledger no conocen FoodState ni cómo se cocina.
- Un placeholder reutilizado, ruta explícita sin NavMesh, un pedido por visita,
  intervalos de configuración y rotación de menú determinista. Override de próximo
  pedido una vez. Sin cola, paciencia, mesas, animación o inventario.
- `DeliveryZone` consulta presencia física actual. Solo procesa DishItem final,
  intacto, disponible, suelto y apoyado con baja velocidad sobre un pad horizontal.
  Retiene la colocación rechazada hasta recoger/retirar, evitando reentrega al
  cliente siguiente sin intención. Ingredientes, cajas y borradores se ignoran.
- `OrderEvaluation` compara IDs estables de definición, nunca nombres/visuales.
  `DishSnapshot` y `IngredientSnapshot` capturan el estado real de entrega, IDs
  originales, composición, frescura mínima/media, Spoiled y Rotten separados,
  contaminación, temperatura, edad, etapa/dosis de cocción y coste total.
  Corrección y calidad se consultan por separado; sin qualityScore.
- Las snapshots son evidencia histórica inmutable, **no** la composición viva M5
  ni nuevas unidades. El plato vivo mantiene los FoodState originales hasta vender.
  Los recibos no retienen FoodItems, GameObjects o estados mutables destruidos.
- `OrderDelivery` ejecuta en el hilo de simulación una transacción síncrona sin
  callbacks: captura evaluación/recibo, valida IDs/duplicados/overflow, registra
  céntimos, marca vendido si acepta y cierra pedido. `PaymentLedger` empieza en 0,
  usa long en céntimos y registra IDs de pedidos procesados/platos vendidos.
  Segunda operación no muta saldo. Fallo de validación deja pedido/plato disponibles.
- Correcto paga precio configurado completo. Runtime desregistra unidades del único
  FoodSimulation, desactiva y destruye el agregado tras conservar el recibo.
  Incorrecto termina pedido/visita sin pago ni destrucción; el plato sigue vivo.
- Receive y Evaluate son fases síncronas sin animación. Pay/Reject se muestra 4 s,
  luego Leave y 3 s tras llegar a salida antes del siguiente cliente (valores de
  desarrollo configurables). Último recibo visible mientras llega el siguiente.

## Consecuencias

No se generan platos durante gameplay, no se reemplazan ingredientes, no cambia
el reloj de alimentos ni el input M1–M5. Precios y tiempos se autoran en
CustomerService.asset; las condiciones de un pedido se copian al inicio de sesión.
No hay guardado, compras, stock, propinas, gasto de ingredientes ni rentabilidad:
el coste en el recibo es informativo y el ingreso bruto aumenta el saldo.

Ruta cinemática no evita obstáculos dinámicos; puntos de entrada/salida son
marcadores interiores de aparición/desaparición en el greybox cerrado. No se
abren puertas ni modifican paredes. Entrega aproxima apoyo mediante bounds Y,
centro dentro del pad y velocidad; no soporta mostradores inclinados ni contacto
exacto. El portador conserva la esfera conservadora M2, con límites de colocación.

M7 deberá acordar su alcance. Políticas futuras podrán consultar la evidencia sin
confundir coincidencia, seguridad y satisfacción. Si se necesitan concurrencia,
persistencia o historial completo de ventas, habrá que revisar la transacción y
retención: hoy se conserva el último recibo y conjuntos de IDs durante la sesión.
