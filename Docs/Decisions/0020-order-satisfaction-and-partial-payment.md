# ADR 0020: completitud del pedido y pago parcial

Fecha: 2026-10-07. Base validada: `cf12f9d` (M1–M13 y correcciones aprobadas).

## Decisión

M14 sustituye la aceptación binaria por ID de receta de M6. `DeliveryContents`
contiene referencias a las unidades realmente entregadas: Dish final o alimentos
sueltos. Evaluar alimentos no crea ni confirma un Dish. `OrderSatisfaction`
compara multiconjuntos por ID estable, conservando multiplicidades: dos Bun
requieren dos unidades distintas. Registra Expected, Received, Missing y Extra.
Reconocimiento y corrección exacta se conservan como hechos separados.

`OrderOffer` copia pesos positivos para cada posición de receta. Las posiciones
con el mismo ID tienen el mismo peso. Configuración vacía usa pesos iguales.
La escena configura Hamburger `[3,4,3]` y Cheeseburger `[3,4,3,3]`:
cada Bun aporta 150 céntimos, Patty 200 y Cheese 150 al precio de 500/650.

`pago = floor(precio máximo × peso esperado recibido / peso esperado total)`.

Se multiplica antes de dividir; el redondeo ocurre una sola vez. Extras no
aportan peso ni aumentan el máximo. Una entrega con al menos un ingrediente
esperado se acepta, aunque su receta sea otra o no esté reconocida. Sin ninguno
se rechaza y se conservan los objetos disponibles. No hay bonificación por receta
ni penalización por extras en esta política provisional. Un precio muy pequeño
puede redondear una entrega relevante a cero; aceptación y pago son distintos.

`DishSnapshot`/`IngredientSnapshot` siguen conservando evidencia inmutable de
calidad y seguridad: identidad, temperatura, edad, frescura, deterioro, contaminación
y cocción/dosis. Esos hechos no reducen el pago M14 y no forman un qualityScore.

El único `PaymentLedger` recibe el importe real y todos los IDs alimentarios.
Valida pedido, entrega, unidades, máximo, duplicados y overflow antes de mutar.
Marca vendidos los estados originales solo tras registrar la operación. El
resumen diario M13 suma ingresos reales. Rechazar cierra el pedido una vez sin
crear venta ni vender las unidades; el siguiente pedido puede reevaluarlas.

PASS/DELIVERY conserva su soporte como única referencia espacial. No se consultan
posición, mirada ni manos del jugador para decidir sobre comida ya depositada.
Se exige soltar, baja velocidad, solapamiento razonable y apoyo. El servicio
reserva la transferencia antes de pagar. El carrier conserva todos los objetos
originales hasta Exit; allí desregistra los FoodItem y los destruye, también al
cancelar servicio. No crea alimentos, no añade reloj ni cambia FoodSimulation.

## Consecuencias y límites

Componer en Prep antes de colocar en PASS: una entrega relevante resuelve el
pedido una vez, sin rondas posteriores para completar faltantes. Plate sigue
siendo utensilio opcional, fuera de la receta y sin precio de venta adicional.
Una Hamburguesa para Cheeseburger ya no sirve como fixture de rechazo; los tests
de rechazo/cola usan ingredientes realmente irrelevantes y siguen verificando
reintento por otro cliente. No hay reputación, intoxicación, inspección, M15 ni
guardado. La política es provisional y configurable antes de iniciar sesión.
