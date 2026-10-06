# ADR 0003: interacción genérica y agarre físico M2

Fecha: 2026-10-06. Estado: aceptada para implementación M2; aceptación manual pendiente.

## Contexto

M1 está aprobado. M2 necesita interacción reutilizable y manipular cajas físicas
sin introducir comida, inventario ni otros sistemas. Se conserva el jugador
CharacterController y el Input System existente.

## Decisión

- Usar `Interactable` como contrato abstracto MonoBehaviour: nombre, acción,
  `CanInteract` y `TryInteract`, con un contexto local de actor y portador opcional.
  Es una dependencia real de detección/orquestación, sin registro de tipos futuros.
- Separar detector, adaptador de input, orquestador, feedback, Pickup y portador.
  El detector elige el collider sólido más próximo; una pared también bloquea.
  La solicitud vuelve a detectar y comprobar disponibilidad antes de ejecutarse.
- `Pickup` reclama un portador de forma exclusiva. `PhysicalCarry` posee un solo
  Rigidbody; destrucción/desactivación de cualquiera de los componentes libera
  esa relación. No se crea inventario ni se clona el objeto al recogerlo.
- Mantener Rigidbody dinámico con CCD e interpolación durante el agarre. Aplicar
  seguimiento en FixedUpdate mediante velocidad deseada y cambio de velocidad
  limitado por fuerza/masa. Congelar rotación, suspender gravedad y excluir solo
  colisiones con el jugador; restaurar ajustes y exclusiones previas al soltar.
  No parentar, teletransportar ni convertir a kinematic.
- Usar una esfera conservadora que engloba los bounds de colliders del mismo
  Rigidbody. Overlap en el origen y spherecast hacia delante acortan el destino
  ante obstáculos; no colocar su volumen dentro de la cápsula. Si no cabe o se
  separa demasiado del destino, soltar en su posición física actual. CCD y
  colisiones del cuerpo resuelven su recorrido hacia ese destino.
- Limitar seguimiento a 6 m/s, salida al soltar a 2 m/s y al lanzar a 12 m/s.
  Lanzar aplica el equivalente de un impulso de 6 N·s dividido por masa, más
  velocidad heredada limitada. Los parámetros son configurables en Inspector.
- Reutilizar el asset existente: Interact pasa de Hold a pulsación; añadir Drop
  (G) y Throw (botón derecho). Mantener GUID y bindings preexistentes. Cada
  adaptador habilita solo sus acciones en una copia runtime Keyboard&Mouse.
  El feedback obtiene las etiquetas de esos bindings. Escape/clic izquierdo
  siguen siendo controles de cursor M1; input de mundo requiere control capturado.
- Mantener todo en Runtime: las consultas, vectores y velocidades son física de
  Unity, no reglas de restaurante. Los cálculos deterministas tienen tests, sin
  inventar estado Domain ni nuevos assemblies/dependencias.
- Ampliar el generador existente y conservar su archivo/clase/GUID por referencias
  de M1. El menú pasa a Prototype y genera cuatro cajas de masa/tamaño/color
  distintos. Preservar geometría, materiales y escena de plantilla de M1.

## Consecuencias

Un futuro interactuable podrá añadir disponibilidad/acción sin cambiar el
detector ni conocer sus detalles desde el jugador. Un futuro ingrediente podrá
usar Pickup en su representación física conservando por separado identidad y
estado Domain; el agarre no crea, copia ni decide esos datos.

Las masas altas aceleran y se lanzan menos. El agarre sigue siendo asistido y
su orientación permanece fija; no simula una mano o articulación física. La
envolvente es deliberadamente conservadora: las cajas grandes pueden rechazarse
o soltarse en espacios estrechos y al mirar hacia los pies. No garantiza contacto
perfecto ante giros bruscos, colisiones complejas o cambios de collider/escala en
pleno agarre. Revisar estos límites cuando aparezcan necesidades jugables reales.

El contrato puede consultar el portador si lo necesita; otros interactuables
pueden ignorarlo. La sesión permite una acción genérica mientras se sostiene un
objeto, aunque el feedback M2 prioriza Drop/Throw y no muestra un segundo prompt.
La interpretación de colocar/consumir objetos se decide en milestones posteriores.

Se verifica Unity en una copia aislada ignorada; nunca se inicia un segundo
Editor sobre el proyecto abierto del usuario. No se integran cambios en main.
