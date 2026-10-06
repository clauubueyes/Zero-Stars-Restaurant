# Zero Star Restaurant

Prototipo de simulador sandbox de restaurante 3D en primera persona, desarrollado
en Unity. El jugador comienza con **0 €**, un local casi vacío y **sin electricidad**.
El objetivo es conseguir que el negocio sobreviva mediante sistemas combinables.

M1 permite caminar, mirar y saltar en un restaurante greybox. No hay todavía
interacción con objetos ni sistemas de restaurante. La escena de plantilla
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

`main` conserva el proyecto original como baseline. La foundation está en
`feature/project-foundation`; M1 parte de ella en `feature/player-controller`.
No hay remoto configurado y no se ha integrado en main; los commits son locales.
