# ADR 0018: contabilidad y liquidación diaria M13

Fecha: 2026-10-07. Estado: implementado desde M12 `6258bad` en
`feature/bills-operating-costs`; aceptación visual pendiente.

## Contexto

PaymentLedger ya cobra ventas y compras con protección por IDs. RestaurantDay
decide apertura, Closing y Closed; la cola notifica la salida del último cliente.
M12 registra kWh reales por aparato y total, sin cobrar. El negocio necesita
ingresos, gastos y un resultado diario sin crear otro saldo ni reiniciar objetos.

## Decisión

- Extender PaymentLedger con LedgerTransaction inmutable: día, ID de transacción,
  ID de objeto, categoría e importe entero. TrySpend/TryRecord escriben evidencia
  al realizar la operación original; rechazos, compras fallidas y duplicados no
  generan ingresos/gastos. No se vuelve a cobrar al resumir ni se consultan objetos.
- Un OperatingCostSettings compartido crea OperatingCostPolicy inmutable para
  cada sesión. Tarifa entera en céntimos/kWh y lista de costes fijos con ID,
  etiqueta e importe; solo Rent está configurado. Los costes futuros no requieren
  ramas hardcodeadas por nombre. No se escribe estado mutable en el asset.
- El cargo eléctrico usa el total **diario** del mismo ElectricitySupplyState
  de M12. Convertir ese double a decimal, multiplicar por la tarifa y redondear
  **una sola vez** el cargo final al céntimo más próximo, con mitades hacia arriba
  (MidpointRounding.AwayFromZero). No sumar redondeos por aparato/frame ni usar
  los kWh redondeados del HUD. 2,35 kWh × 30 céntimos/kWh = **71 céntimos**.
- PaymentLedger.TrySettleDay prepara todos los cargos y el DailySummary antes de
  mutar. Comprueba overflow y conciliación; después publica juntos los cargos,
  saldo y recibo. Permite saldo negativo por facturas; TrySpend conserva el
  requisito M8 de fondos suficientes. El primer resumen del día es definitivo;
  repetir liquidación devuelve la misma instancia y no añade cargos.
- RestaurantOperatingCosts referencia explícitamente el día, servicio/ledger,
  suministro y settings. No tiene Update. RestaurantDayController lo invoca al
  llegar a Closed desde el reloj o RefreshOccupancy. Closing no liquida y admite
  las compras/ventas pendientes hasta Exit. Next Day exige la liquidación previa;
  sus reintentos tampoco pueden cobrar otra vez. Escenas M13 marcan el requisito
  para que una referencia ausente no permita saltarse el pago. Fixtures anteriores
  sin el componente mantienen el comportamiento M11.
- El Update eléctrico pasa a orden −250, antes del día −200 y servicio 0: el
  resumen incluye el consumo de la actualización que cierra el restaurante.
  Los segundos siguen siendo de simulación, independientes de velocidad/pausa
  del mundo y FoodSimulation. No se añade otro reloj térmico/alimentario.
- Los medidores M12 mantienen los acumulados históricos y añaden un acumulado
  diario. CompleteDay congela los diarios al liquidar; mientras se consulta el
  resumen, los aparatos pueden seguir funcionando y el histórico sigue contando.
  Ese intervalo entre jornadas no se factura ni simula una noche. BeginDay resetea
  solo los diarios del suministro y **todos** sus aparatos, incluidos los OFF.
- El mismo ledger comienza el siguiente período con el saldo anterior y otro
  índice de transacciones; conserva IDs procesados, transacciones y resúmenes
  históricos. No se reemplazan comida, platos, equipos, estados ni ledger.
  Procurement rechaza antes de instanciar mientras el día está liquidado, para
  mantener inmutable la conciliación del resumen. Se habilita de nuevo en Next Day.
- El HUD provisional presenta el recibo hasta Next Day. Usa el comando existente
  de Inspector para iniciar el siguiente día; no cambia el input/captura FPS.
  Los saldos negativos se representan correctamente incluso por debajo de 1 €.

## Consecuencias y límites

Valores de desarrollo: alquiler 300 céntimos/día, electricidad 30 céntimos/kWh,
saldo inicial de escena 1000 céntimos. Se configuran antes de Play y se copian
al iniciar; cambiar el asset durante una sesión no cambia su política contable.
El saldo de código sigue empezando en cero. No hay préstamos, intereses,
consecuencias de deuda, impuestos, personal, otros servicios, mantenimiento,
averías, noche, guardado ni M14. Ver recorrido, tests e inventario en [M13](../M13.md).
