# ADR 0001: base mínima y separación de reglas/presentación

Fecha: 2026-10-06. Estado: aceptada para preparar el prototipo.

## Contexto

El proyecto es la plantilla URP Empty de Unity 6000.5.3f1, sin gameplay propio.
El concepto prevé muchos sistemas, pero primero necesita demostrar una venta
completa. Habrá que sustituir primitivas sin reescribir cocina, platos o economía.

## Decisión

1. Conservar Editor, URP, Input System, paquetes y assets de plantilla. Añadir el
   contenido propio bajo `Assets/_Project`, sin migrar GUID o referencias existentes.
2. Separar reglas/estado en C# independiente de Unity y adaptadores/vistas en
   Runtime. Componer por referencias explícitas. No añadir un framework ni
   interfaces, servicios o assemblies vacíos por anticipación.
3. Usar ScriptableObjects como configuración; crear instancias para estado mutable.
   Introducir assemblies de Domain/Runtime al necesitarlos para las primeras
   reglas y pruebas, con Domain sin referencias al motor.
4. Priorizar el slice hasta el cobro y después ampliar los sistemas. Para probarlo
   con 0 € y sin electricidad, incluir provisiones limitadas de desarrollo y una
   estación de calor no eléctrica. Obtener equipo/provisiones y gestionar
   combustible/electricidad de forma jugable se abordará tras el slice.
5. Registrar el proyecto original en main y hacer la preparación en una feature
   con commits separados. Sin remoto, merge o dependencias externas nuevas.

## Consecuencias

Se mantiene el proyecto reconocible y se evita trabajo de migración sin valor
jugable. Cambiar una vista no debe cambiar el estado de comida o la validación
del pedido. La separación por carpetas es inicialmente una convención y deberá
forzarse con assemblies cuando haya código y tests reales.

La fixture de calor/provisiones valida una venta, no resuelve todavía el equilibrio
económico ni la adquisición inicial. Esa deuda de diseño queda explícita en el
roadmap. Habrá assets y paquetes de plantilla sin uso en gameplay; retirarlos será
una limpieza independiente y verificada cuando aporte valor.
