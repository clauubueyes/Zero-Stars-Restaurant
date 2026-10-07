# ADR 0011: distribución greybox y recorridos de servicio

Fecha: 2026-10-06. Estado: implementado en M9 desde M8 validado `c12d408`;
aceptación manual pendiente.

## Contexto

El loop M1–M8 funciona, pero las mesas, obstáculos de prueba y puntos de cliente
siguen organizados como una sala de tests. M9 autoriza level design y preparación
espacial de una cola futura, manteniendo exactamente un cliente activo.

## Decisión

- El norte (`z > 1`) es cocina. Un mostrador sólido de pared a pared separa el
  área pública al sur. El jugador empieza detrás del mostrador; el cliente espera
  delante. Delivery conserva soporte, dimensiones, sensor y referencias M6.
- Fridge/Freezer abren hacia el este desde la pared oeste. Procurement y Prep
  forman la fila baja de trabajo; Grill y Assembly forman la fila alta. Un pasillo
  intermedio conecta ambas filas y el pase hacia Delivery.
- La pared sur tiene dos huecos de 2 m y un umbral libre de 2,7 m de altura:
  entrada oeste y salida este. El suelo se prolonga fuera de esas puertas para
  que aparición/desaparición ocurran fuera del restaurante.
- CustomerMovement recibe `_departurePath` opcional, con salida independiente.
  No cambia el reloj de visitas ni la cardinalidad del servicio. Sin ruta de
  salida explícita conserva el regreso por el camino M6. No hay navegación,
  evitación dinámica ni lógica de cola.
- `QueuePoints` contiene cuatro transforms ordenados desde el mostrador hacia
  atrás. El primero es el mismo CustomerWaitingPoint ya referenciado por M6;
  los demás solo reservan espacio. Sus marcas no tienen colliders ni scripts.
- Las cajas M2 y obstáculos de movimiento permanecen inactivos y recuperables
  desde Inspector. AssemblySupplyBench queda apartada al este e inactiva;
  DevelopmentIngredientSupply activa ese soporte junto con sus ingredientes al
  optar explícitamente por las fixtures. Las estaciones normales siguen activas.
- El generador M9 adapta los objetos existentes sin recrearlos. Rebuild Greybox
  aplica M8 y M9, y la aplicación repetida de M9 no duplica geometría ni referencias.
  Rebuild sigue siendo una reconstrucción destructiva explícita de la escena;
  Apply M9 exige cerrarla y conserva los objetos M1–M8.

## Consecuencias

Ajuste posterior solicitado: Procurement se traslada con su banco, botones,
OUTPUT y fixtures de desarrollo a un pequeño anexo al oeste del perímetro de
cocina. Un hueco local en WallWest mantiene acceso directo sin cruzar el servicio.
El resto de zonas y recorridos conserva su posición. La adaptación específica y
Rebuild reutilizan el mismo procedimiento idempotente; referencias, precios,
prefabs, estado y código Runtime permanecen intactos. Ver [zona de Procurement](../PROCUREMENT-AREA.md).

Se conservan Domain, saldo, precios, IDs de comida/platos, único FoodSimulation,
input, assets y GUID. Los tests de interacción física dependientes de posiciones
antiguas se adaptan al acceso actual. Se verifican las rutas contra colliders
sólidos con anchura de cliente y cápsula de jugador, además de las regresiones.
La comodidad del transporte, cámara y puesta sobre mesas requiere también
aceptación manual en Game. Los recorridos son explícitos para un solo cliente;
un jugador/objeto que invada la zona pública no recibe evitación automática.
