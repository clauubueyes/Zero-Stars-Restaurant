# ADR 0021: calidad alimentaria y consecuencias por visita

Fecha: 2026-10-07. Base validada: M14 `8751d34`.

## Decisión

M14 sigue calculando aceptación y céntimos por completitud. M15 lee exactamente
el `DishSnapshot`/`IngredientSnapshot` inmutable capturado al entregar, con los
IDs y estados reales de todas las unidades recibidas, también extras. No introduce
qualityScore, reloj, descuento, restitución de frescura ni cambios a la transacción.

`FoodQualityEvaluation` conserva el snapshot y una causa por problema y unidad.
`Issues` resume problemas distintos sin perder causas repetidas de varios alimentos.
Good significa ausencia de problemas; Cooked y los alimentos no cocinables frescos
sin contaminación son buenos. Temperatura, edad, frescura y dosis permanecen como
hechos inspeccionables; no se inventa una penalización térmica o microbiología nueva.

Política provisional determinista:

| Estado real | Causa | Reacción mínima |
| --- | --- | --- |
| Fresh, Cooked / no cocinable, sin contaminación | Good | Satisfied |
| FoodCondition.Acceptable | Stale | Unhappy |
| Overcooked | Overcooked | Unhappy |
| Burnt | Burnt | Complaint |
| Spoiled / Rotten | Spoiled / Rotten | Health Incident |
| Contaminación, cualquiera que sea la cocción | Contaminated | Health Incident |
| Raw / Undercooked en alimento cocinable de categoría Meat | Raw / Undercooked | Health Incident |
| Raw / Undercooked cocinable de otra categoría | Raw / Undercooked | Unhappy |

Los umbrales de deterioro/cocción provienen de cada perfil existente. Una pieza
no cocinable no se considera cruda peligrosa. Se conserva cada problema simultáneo;
la reacción es única y prevalece Health Incident > Complaint > Unhappy > Satisfied.
Los contadores son exclusivos: un incidente sanitario con Burnt no incrementa
además Complaints. La causa Burnt sí permanece en el registro del incidente.

`CustomerVisit.Resolve` produce una sola `CustomerConsequence` si M14 aceptó y
completó la transacción. Es la representación provisional de aceptar/comer, durante
Pay, después de fijar físicamente la entrega y antes de salir; sin animación ni
etapa temporal nueva. Un reintento de Resolve falla. El rechazo no genera reacción.
La consecuencia conserva CustomerId, OrderId, recibo M14, calidad y reacción.

El único `CustomerServiceLoop` registra esa consecuencia en estadísticas de sesión.
Se deduplican tanto CustomerId como OrderId. CustomersServed incluye aceptaciones
parciales y relevantes redondeadas a cero; excluye rechazo/fallo de transferencia.
History y HealthIncidentHistory conservan evidencia después de Exit, cancelación
y Next Day. Disable/enable no reinicia contadores ni ledger. No hay estado global,
driver adicional ni persistencia entre sesiones.

PASS y CustomerDishCarrier permanecen sin cambios: únicamente objetos originales,
independientemente del jugador, vivos hasta Exit. La evidencia se congela al
entregar; cambios posteriores de los alimentos transportados no alteran la reacción.
OrderFeedback añade causas, reacción y estadísticas al HUD provisional existente.

## Límites

Health Incident es un registro inmediato, no una intoxicación simulada. No hay
reputación, estrellas, enfermedad diferida, hospital, inspección, policía, multa,
devolución, reducción del pago ni M16. Las reglas están centralizadas en Domain;
no añaden configuración de escena ni dependencias. Se podrán ampliar cuando lo
autorice un milestone futuro.
