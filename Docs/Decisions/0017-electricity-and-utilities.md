# ADR 0017: suministro eléctrico y consumo M12

Fecha: 2026-10-07. Estado: implementado desde `ec2ba3e` en
`feature/electricity-utilities`; aceptación manual pendiente.

## Contexto

Grill y ColdStorage describen entornos térmicos; FoodSimulation es el único
driver de edad, temperatura, conservación y cocción. M12 introduce electricidad
real y evidencia de consumo, sin facturas ni modificaciones de FoodState.

## Decisión

- `ElectricitySupplyState` y `ElectricityMeter` son Domain sin Unity. Cada sesión
  comienza con suministro OFF y energía cero. ON/OFF conserva los acumulados.
  Cada medidor tiene potencia nominal inmutable en vatios y energía en kWh.
  `kWh = W × segundos / 3 600 000`; total y medidores se validan antes de mutar.
  Se rechazan valores negativos/no finitos, listas inválidas y overflow; referencias
  repetidas cuentan una vez. No hay precios, cargos, redondeo monetario ni ledger nuevo.
- `RestaurantElectricity` referencia explícitamente sus `ElectricalAppliance`.
  Cada aparato referencia el suministro y su HeatSource; esta referencia el mismo
  aparato. No hay descubrimiento global, eventos generales ni singleton.
- Corrección solicitada sobre M12: `ElectricalApplianceState` separa la selección
  ON/OFF individual del suministro y del medidor. `HasPower` exige suministro ON
  y selección ON; cortar/restaurar nunca escribe en esa selección. Cada instancia
  copia Initially On (por defecto true) al iniciar, conservándolo en disable/enable
  y Next Day. El suministro general sigue inicialmente OFF. Ningún estado se guarda
  en assets; el medidor existente conserva su acumulado al conmutar.
- `ApplianceSwitch` reutiliza E/Interactable en la raíz de cada aparato: los
  colliders sólidos de sus hijos resuelven esa acción, sin geometría/teclas nuevas.
  Se puede seleccionar ON/OFF con manos libres durante un corte. Un Food/Pickup
  enfocado sigue resolviendo su propia acción. PromptLabel conserva el formato
  genérico previo y permite **[E] Turn Grill On/Off** sin duplicar nombres.
  El HUD distingue selección ON/OFF y operación Running/Stopped; el instalador
  actualiza escenas M12 generales sin cambiar ajustes y conserva idempotencia.
- Las fuentes de la escena requieren electricidad explícitamente. Sin corriente,
  `IsOperational`/`TryGetEnvironment` dejan de ofrecer entorno y FoodSimulation
  selecciona ambiente con sus reglas existentes. Restaurar corriente solo cambia
  disponibilidad: no reinicia temperatura, edad, frescura, contaminación ni dosis.
  Grill caliente sin corriente enfría sin cocción residual, respetando M4.
- HeatSource sin dependencia y sin requisito eléctrico conserva las fixtures
  históricas aisladas de tests. Una fuente con requisito eléctrico y referencia
  ausente/destruida permanece apagada; quitar el componente no la vuelve gratuita.
- Los medidores registran carga nominal continua mientras aparato, fuente y volumen
  térmico están activos con transferencia positiva, incluso vacíos. Multiplicador
  térmico y vatios son unidades independientes. Cero vatios permite simular una
  carga ideal sin quitar el requisito eléctrico; cero transferencia apaga el aparato.
- El único Update eléctrico usa segundos de simulación Unity, como los clientes.
  No recibe velocidad/pausa de RestaurantDay, multiplicador alimentario de debug,
  botones Advance de comida ni saltos Next Day. Tampoco avanza FoodSimulation ni
  FoodState. Deshabilitar/reactivar suministro corta/restaura sin perder energía.
- La potencia se configura antes de Play por componente (no hay configuración
  compartida que requiera un ScriptableObject); el medidor copia el valor al iniciar
  la instancia. El estado queda en memoria de sesión y sobrevive a Next Day.
- Un interruptor de primitivas reutiliza E/Interactable con manos libres. Etiqueta
  y HUD muestran ON/OFF, W activos y kWh totales/por aparato. Menús de Inspector
  permiten cortes/restauraciones de desarrollo. Rebuild e instalador cerrado
  incluyen M12; repetir instalación valida sin recrear componentes ni cambiar ajustes.

## Consecuencias y límites

Los acumulados y referencias por aparato permiten calcular costes más adelante,
pero no se implementan facturas, impagos, generadores, averías ni M13. No hay
persistencia, tarifas, termostatos, ciclos de compresor, calor residual del aparato,
baterías ni noche simulada. ON es una interacción gratuita del prototipo; el
desbloqueo comercial del suministro sigue fuera de alcance. La iluminación ambiental
de desarrollo sigue permitiendo ver el greybox, sin convertirse en otro consumidor.

Cambios de suministro o posición segmentan el tiempo: Advance explícito cuenta el
intervalo con el estado actual. No reproduce cortes o movimientos dentro de un salto.
Los medidores conservan precisión `double`; el texto HUD redondea solo presentación.
