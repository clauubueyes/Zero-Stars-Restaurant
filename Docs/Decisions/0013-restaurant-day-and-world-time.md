# ADR 0013: jornada de restaurante y tiempo del mundo

Estado: implementado en M11 desde M10 validado (`bd8d33c`).

## Contexto

M10 admite clientes de forma continua. El gameplay necesita apertura, cierre y
sucesión de días sin duplicar el reloj de alimentos ni recrear estados al cerrar.
La velocidad del mundo debe poder ajustarse sin cambiar el tiempo de cocción,
la física o el movimiento que ya validó el usuario.

## Decisión

`GameTime` y `RestaurantDay` contienen estado/reglas C# sin Unity: número de día,
hora/minuto, horario y fases Ready → BeforeOpen → Open → Closing → Closed.
Una jornada admite `0 <= inicio <= apertura < cierre < 24:00`; no hay horarios
nocturnos. Empezar la primera jornada usa Day 1. Iniciar otra requiere Closed y
ocupación cero, incrementa el número y sitúa el reloj en la hora inicial.

`RestaurantDayController` convierte segundos de simulación explícitos en
segundos del mundo mediante una velocidad configurable. Su Update observa
`Time.deltaTime` **solo para el reloj del mundo**, antes del servicio de clientes.
No usa Time.timeScale, ni llama a FoodSimulation, FoodState o CustomerServiceLoop.
Pausa/resume y velocidad cero detienen únicamente el reloj del mundo.

Relación deliberada en M11:

| Sistema | Tiempo que consume | Driver |
| --- | --- | --- |
| Horario/hora del mundo | segundos de simulación × velocidad del mundo | RestaurantDayController |
| Edad, conservación, temperatura y cocción | segundos de simulación × multiplicador alimentario de desarrollo M3 | FoodSimulation existente, una vez por unidad |
| Movimiento, fases y paciencia de clientes | segundos de simulación M6/M10 | CustomerServiceLoop existente |
| Jugador/física | tiempo Unity existente | controladores/física existentes |

No existe conversión inversa desde hora del mundo a alimentos. Llegar al día
siguiente tampoco simula una noche ni un salto alimentario: solo cambia la
etiqueta de jornada y su hora inicial. Mientras el juego sigue ejecutándose,
la comida continúa simulándose también en Closing/Closed o con reloj pausado.

La cola referencia el controlador del día explícitamente. Solo Open permite
admisión, tanto por temporizador como por TryAdmit. Llegada, espera, entrega,
evaluación, pago y salida de clientes ya admitidos siguen funcionando después
del cierre. La reserva se conserva hasta Exit M10; retirar la última notifica
la ocupación y termina Closing inmediatamente. No se expulsa a nadie.

El día siguiente reinicia el delay de admisión M6 para que una jornada corta
no herede un temporizador más largo que su horario. Conserva contador/IDs de
clientes, ledger, recibos, comida, platos y configuración. No recarga la escena,
reinicializa servicio ni repone bandejas. Las fixtures sin controlador de día
mantienen admisión M10 para regresiones/escenas aisladas; deshabilitar un
controlador asignado bloquea nuevas admisiones.

## Consecuencias y límites

El cierre con clientes esperando permanece Closing hasta resolver sus visitas.
La paciencia cero M10 sigue siendo debug, sin abandono. El reloj avanza durante
Closing y se congela en Closed; si la espera cruza medianoche se limita a
23:59:59 en la misma jornada hasta empezar la siguiente. Es una política mínima
para no introducir calendario, turnos nocturnos ni reinicios implícitos.

El prototipo empieza automáticamente abierto a las 09:00, cierra a las 17:00 y
avanza 60 segundos del mundo por segundo de simulación: ocho minutos de horario.
Las horas/automatismos se configuran antes de Play; la velocidad y pausa pueden
cambiarse durante Play. El siguiente día se inicia con un comando provisional
del Inspector. No hay facturas, alquiler, electricidad, sueño, eventos ni guardado.
