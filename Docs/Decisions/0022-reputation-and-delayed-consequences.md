# ADR 0022: reputación y consecuencias diferidas

Fecha: 2026-10-07. Base: M15 validado `3799710`.

## Decisión

Separar completitud/pago M14, calidad/reacción M15 y consecuencias persistentes M16.
RestaurantReputationState recibe la misma CustomerConsequence y conserva sus IDs,
OrderResult, FoodQualityEvaluation y snapshots originales. No depende de PaymentLedger,
comida viva ni Unity. History y HealthRisks son vistas de solo lectura. Deduplicación
por CustomerId y OrderId al servir y por visita al finalizar. Los eventos conservan
valor anterior/posterior, causa, día/hora, avance de mundo y resultado de RNG/forzado.

ReputationPolicy es inmutable; ReputationSettings crea una copia al iniciar sesión.
Rango 0–100, inicial 50; Satisfied +1, Unhappy -1, Complaint -3, confirmado -8.
Las reacciones normales solo cambian al completar Exit. Health Incident registra
un pendiente al servir sin penalización inmediata. Tras Exit se fija el vencimiento.
Cancelar el servicio de una visita ya consumida también habilita ese riesgo: destruir
su representación no puede borrar la evidencia. Una cancelación no otorga mejoras
por visitas normales que no finalizaron. El dinero y las estadísticas M15 no cambian.

Por visita hay un caso, con todas las causas sanitarias reales del snapshot. La
probabilidad provisional es el máximo entre carne Raw/Undercooked (35%), Spoiled/
Rotten (55%) y Contaminated (75%). No se multiplican por cantidad de ingredientes
ni se acumula Burnt como riesgo sanitario. Son parámetros de gameplay, no un modelo
médico. Una tirada uniforme en [0,1), comparada con p mediante roll < p; los extremos
0/1 tienen comportamiento exacto. Domain recibe Func<double>; Runtime usa System.Random
con seed configurable. Se conserva la tirada y la evidencia incluso al descartar.
Una resolución no puede repetirse; forzar no consume RNG y queda identificado.

GameTime existente incorpora ElapsedWorldSeconds: acumula solo el cambio efectivo
de SecondsOfDay en Advance. StartDay cambia fecha/hora pero conserva este acumulado.
No cuenta una noche, la pausa, Closed ni el exceso descartado al cerrar o al límite
diario. El plazo usa este avance monotónico, de modo que un riesgo puede terminar
durante varias jornadas sin desaparecer o resolverse por un salto ficticio de noche.
No hay otro reloj ni Update de M16: RestaurantDayController llama ResolveDue después
de avanzar el mundo. FoodSimulation, física, clientes, Time.timeScale y electricidad
mantienen sus drivers. Closed sin avance deja pendientes para Next Day o debug.

RestaurantReputation se referencia explícitamente desde servicio y jornada y contiene
el estado de sesión. OnDisable/OnEnable no lo reinician. La escena actual recibe solo
un componente y referencias; ningún transform, material, prefab o objeto se regenera.
OrderFeedback añade texto al panel de servicio con scroll; DebugHudPresenter observa
las resoluciones y usa su región de mensajes existente, sin nuevos OnGUI ni regiones.

## Límites

Persistencia durante una sesión Play y entre días, como el ledger/M15; no guardado en
disco o restauración entre sesiones. Sin estrellas, inspecciones, multas, policía,
hospital, enfermedades concretas, reseñas, higiene, cambios de clientes/precios ni M17.
