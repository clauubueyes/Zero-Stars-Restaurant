# ADR 0030: superficies visuales únicas y feedback de outcomes exclusivos

Fecha: 2026-10-10. Base validada: `f6fb68c`.

## Decisión

Reparar únicamente hijos Visual_VP1BC de la escena actual, conservando todos los
documentos funcionales. Suelos coplanares se particionan y comparten origen UV;
capas de pared tienen espesores reales y separación de 16 mm; las superficies
redundantes de linteles, bandejas fijas y encimera se retiran de la representación.
No se usan offsets mínimos de profundidad, movimientos de colliders ni rebuild.

El instalador aplica la migración una vez; el mesh propio de suelo del anexo
identifica su aplicación completa. Repetir no reconstruye ni altera ajustes
visuales posteriores del usuario. No se añaden componentes Runtime ni shells.
Se conserva cada documento salvo los campos visuales expresamente aprobados.

Las etiquetas físicas necesitan depth testing: el shader original de la fuente
las dibujaba sobre props situados delante. RestaurantArtPass adapta sus materiales
solo en Play mediante copias por fuente, UI/Default y LessEqual. Conserva el atlas
dinámico y los materiales compartidos; Art OFF restaura originales y OnDisable
libera las copias y su suscripción al evento de atlas. El shader built-in ya está
incluido en el proyecto; no se añaden shaders, componentes ni dependencias.

M15 ya produce una reacción principal exclusiva y registra una vez al consumir
en Pay. Exit no vuelve a sumar; el cierre M19 espera todas las salidas. M16 conserva
otra evidencia de confirmación/descarte sin modificar esa reacción. M19 ya captura
un EndOfDaySummary inmutable y resetea diarios solo en StartNextDay.

Se corrige la presentación: los cuatro outcomes aparecen juntos en diarios,
sesión y cierre, con anchos acotados y scroll vertical. No se cambia clasificación,
pago, contabilidad, riesgo sanitario, calendario ni conservación de estado.

## Evidencia

Ver POST-M19-STABILIZATION.md: auditoría serializada frente a la base, pruebas
deterministas A/B/C con flujo físico completo, doce comidas peligrosas con causas
múltiples, snapshots/reset/históricos y capturas FPS ON/OFF antes/después.
