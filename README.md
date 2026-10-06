# Zero Star Restaurant

Prototipo de simulador sandbox de restaurante 3D en primera persona, desarrollado
en Unity. El jugador comienza con **0 €**, un local casi vacío y **sin electricidad**.
El objetivo es conseguir que el negocio sobreviva mediante sistemas combinables.

M1 permite caminar, mirar y saltar en un restaurante greybox. M2 añade interacción
genérica y cuatro cajas físicas para coger, transportar, soltar y lanzar. M3 añade
alimentos con estado independiente, deterioro temporal y temperatura, inspeccionables
con feedback de desarrollo. M4 añade una plancha física con cocción térmica;
M5 añade montaje libre, reconocimiento de platos y transporte agregado.
Clientes, pedidos y pagos siguen pendientes.
La escena de plantilla
`SampleScene` permanece separada y conservada.

## Abrir el proyecto

1. En Unity Hub, añadir esta carpeta como proyecto existente.
2. Usar **Unity 6000.5.3f1**, la versión registrada en
   `ProjectSettings/ProjectVersion.txt`. No actualizar el Editor o los paquetes
   durante una tarea de gameplay.
3. Esperar a la importación y restauración de paquetes. Un clon limpio requiere
   acceso al registro de Unity o una caché válida de esos paquetes.
4. Abrir `Assets/_Project/Scenes/PrototypeRestaurant.unity` y revisar la Console.
5. Pulsar Play y enfocar Game: **WASD** mueve, **ratón** mira, **Espacio** salta,
   **Escape** libera el cursor y **clic izquierdo en Game** vuelve a capturarlo.
   Con el cursor libre se suspenden movimiento voluntario y mirada; la gravedad
   sigue activa. Detener Play también libera el cursor.
6. Mirar una caja a menos de 3 m: **E** la recoge, **G** la suelta y **botón derecho
   del ratón** la lanza. El texto provisional muestra objeto, acción y binding.
   Consultar [M2](Docs/M2.md) para probar masas, paredes y límites físicos.
7. Ir al banco de pruebas a la izquierda del spawn (`FoodTestZone`): dos porciones
   de carne, pan y queso usan las mismas acciones M2. Mirar o sostener comida
   muestra frescura, temperatura, condición, edad y contaminación. El Inspector
   de FoodTestZone permite avanzar tiempo solo de comida; ver [M3](Docs/M3.md).
8. Coger una carne de la mesa `CookingPrepBench` y soltarla sobre la superficie
   rojiza `GrillHotSurface`, a la izquierda del local. Temperatura y cocción
   avanzan por presencia física; retirarla pausa cocción y permite enfriamiento.
   Ver [M4](Docs/M4.md) para colocación, tiempos y pruebas de rechazo.
9. Las tres bandejas `AssemblyStation1/2/3` están en el banco del fondo izquierdo.
   Depositar ingredientes reales, reorganizarlos y retirarlos con M2; apuntar a
   bandeja/pestaña lateral y pulsar E confirma. El plato final se recoge con E.
   Ver [M5](Docs/M5.md) para orden de pila, reconocimiento y pruebas manuales.

Configuración comprobada: **URP 17.5.0**, calidad PC activa y color Linear;
**Input System 1.19.0** como sistema de entrada activo;
**Unity Test Framework 1.7.0** instalado. El inventario completo está en
[la auditoría inicial](Docs/PROJECT_AUDIT.md).

## Filosofía del prototipo

Primitivas, materiales simples y placeholders. Primero demostrar el ciclo jugable;
después mejorar la presentación. Sin modelos personalizados, Blender, nuevas
texturas, animaciones, shaders complejos, arte definitivo ni assets externos
innecesarios. Las reglas y el estado deben poder sobrevivir a un cambio de modelo,
material, prefab o interfaz.

## Primer vertical slice

**Player → Interaction → Food → Cooking → Dish Assembly → Customer Order → Delivery → Payment**

Una pequeña escena permitirá coger una porción, cocinarla con una fuente de calor
no eléctrica, montar un plato, entregarlo a un cliente placeholder y cobrar una
única vez. Las provisiones iniciales son una fixture limitada de desarrollo para
probar ese ciclo con saldo cero; su obtención será una tarea posterior.

## Documentación

- [Reglas para agentes](AGENTS.md).
- [Estado inicial y configuración comprobada](Docs/PROJECT_AUDIT.md).
- [Arquitectura, estructura y convenciones](Docs/ARCHITECTURE.md).
- [Slice, milestones y criterios verificables](Docs/ROADMAP.md).
- [Flujo Git y comprobaciones en Unity](Docs/WORKFLOW.md).
- [Decisión arquitectónica inicial](Docs/Decisions/0001-prototype-foundation.md).
- [Inventario de cambios y validación de esta entrega](Docs/FOUNDATION.md).
- [M1: pruebas, controles, configuración y reconstrucción del greybox](Docs/M1.md).
- [M2: interacción física, validación y pruebas manuales](Docs/M2.md).
- [Decisión sobre interacción y agarre físico](Docs/Decisions/0003-physical-interaction.md).
- [M3: alimentos, definiciones, deterioro, pruebas y límites](Docs/M3.md).
- [Decisión sobre estado de alimento y tiempo](Docs/Decisions/0004-food-state-and-time.md).
- [M4: plancha, cocción térmica, validación y límites](Docs/M4.md).
- [Decisión sobre fuente térmica y cocción](Docs/Decisions/0005-thermal-cooking.md).
- [M5: montaje, reconocimiento, transporte y validación](Docs/M5.md).
- [Decisión sobre composición física e identidad](Docs/Decisions/0006-physical-dish-assembly.md).

`main` conserva el proyecto original como baseline. La foundation está en
`feature/project-foundation`; M1 parte de ella en `feature/player-controller`.
M2 parte de M1 aprobado en `feature/world-interaction`.
M3 parte de M2 aprobado (`14e0575`) en `feature/food-state`.
M4 parte de M3 aprobado (`307174a`) en `feature/basic-cooking`.
M5 parte de M4 aprobado (`d7eb2bf`) en `feature/dish-assembly`.
No hay remoto configurado y no se ha integrado en main; los commits son locales.
