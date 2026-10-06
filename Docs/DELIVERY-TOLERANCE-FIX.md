# Corrección: entrega tolerante por solapamiento

Rama `fix/tolerant-dish-delivery`, desde el último estado aprobado `17bacd9`.
Árbol limpio al comenzar. Unity 6000.5.3f1, URP 17.5.0, Input System 1.19.0 y
Test Framework 1.7.0 comprobados. Sin cambios Domain, dependencias ni M7.
Commit de implementación y pruebas: `922d0c4` (`fix: accept partially placed dishes
on delivery pad`); documentación en un commit separado.

## Causa y comportamiento

DeliveryZone ya consultaba el volumen físico con OverlapBox, pero después exigía
que el centro de los bounds del plato estuviera dentro del trigger. Eso descartaba
platos claramente apoyados sobre el verde, especialmente en bordes.

Ahora el collider real del Dish debe solapar el trigger y su huella XZ debe cubrir
al menos **20% del área menor entre pad y plato**. El umbral se configura en
**Minimum Footprint Overlap** del Inspector. Se usan los bounds del proxy físico,
sin reglas especiales para recetas/tamaños ni depender del Renderer/nombre.
El denominador permite platos pequeños y platos mayores que el pad; un simple
roce de esquina o borde queda excluido. El centro puede quedar fuera del verde.

Se conservan todos los filtros: Dish finalizado/intacto/activo/no vendido,
Pickup disponible y suelto, Rigidbody dinámico, base a **±6 cm** del pad,
velocidad lineal **≤0.5 m/s** y angular **≤2 rad/s**. Un plato en vuelo, fuera o
pasando rápido no se entrega. La transacción sigue siendo única; el rechazado
permanece recuperable y requiere retirar/recoger antes de otra visita.
El cliente sigue llevando la misma venta hasta salida, con recibo inmutable.

El pad verde pasa de **1.25 × 0.9 m** a **1.8 × 1.0 m**, con la misma altura
**1.13 m** y centro `(0, 1.115, 0.5)`. Visual y soporte físico son la misma
primitiva; el trigger tiene exactamente su misma huella XZ. El generador deriva
ambas medidas de deliverySize. La escena entregada conserva todos los fileIDs/GUID
y los demás objetos.
No hay ampliación invisible del área de entrega ni cambios del mostrador.

## Validación

Se usó una copia aislada `Build/DeliveryValidation`, sin cerrar ni duplicar el
Editor sobre el proyecto original abierto. Los resultados/logs se ignoran en Git.

- **EditMode: 108/108 Passed**, cero fallos/omitidos, salida 0.
  `Build/DeliveryEditFinalResults.xml` y `Build/DeliveryEditFinal.log`.
- **PlayMode: 104/104 Passed**, cero fallos/omitidos, salida 0. Incluye **28 casos
  nuevos** respecto al estado aprobado y las regresiones completas M1–M6.
  `Build/DeliveryPlayResults.xml` y `Build/DeliveryPlay.log`.
- Generación nativa de greybox completada con salida 0; la escena entregada
  coincide semánticamente con el generador. **409 fileIDs originales** conservados,
  **147 GUID únicos**, metas/referencias válidas. Solo cambian tres documentos
  serializados: tamaño del pad, tamaño del trigger y umbral de entrega.
  Evidencia local: `Build/DeliveryRegenerate.log` y `Build/DeliveryAudit.py`.
- `git diff --check` correcto; fuentes entregadas idénticas a las probadas.
  Domain, Input System, dependencias, ProjectSettings, materiales y main intactos.

La primera ejecución EditMode detectó redondeo al comparar exactamente los bounds
del Renderer y collider. La aserción usa ahora tolerancia de 1 mm por eje y se
repitió la suite completa. Los resultados anteriores corresponden al estado final.
La comprobación visual y sensación con teclado/ratón quedan para la prueba manual;
la automatización se ejecutó en el Editor batchmode con físicas nativas.

Casos nuevos en M6ServiceTests: centro; cuatro bordes; centro fuera y solapamiento
parcial; esquina con cobertura suficiente; roce mínimo y fuera; rotación; tamaños
0.5/1/2 de Hamburger, Cheeseburger y Custom; rechazo recogible; velocidad/altura y
doble procesamiento. M6PrototypeTests repite el flujo completo con G y físicas
nativas tanto centrado como con el centro del agregado fuera del pad. EditMode
comprueba la coincidencia visual/soporte/trigger.

## Comprobación manual

1. Detener Play, guardar cambios propios aparte y reabrir desde disco
   `Assets/_Project/Scenes/PrototypeRestaurant.unity`. No guardar una escena
   antigua encima. Esperar importación y comprobar Console; no hace falta regenerar.
2. Seleccionar CustomerServiceZone, Force Next Offer Index **0**, Play.
   Montar Hamburger con Bun → Patty → Bun,
   clic para colocar, F para finalizar y E para coger; esperar al cliente en Wait.
3. Desde el lado de cocina, soltar con **G** sobre el pad verde ampliado. Probar en
   sesiones nuevas centro y ambos extremos laterales: basta que una parte visible
   razonable del conjunto quede sobre el verde. Dejarlo reposar brevemente.
   Una prueba reproducible de borde: jugador aproximadamente `(1.04, 0, 2.25)`,
   mirando hacia el mostrador. El centro del plato puede quedar fuera del pad.
4. Comprobar pago €5 una sola vez y salida visible llevando el plato. Mantenerlo
   sostenido sobre el verde no vende; fuera del verde o lanzado de paso tampoco.
5. Sesión nueva, forzar **1** y entregar Cheeseburger; también probar Hamburger
   incorrecta/Custom parcialmente colocados: Reject, saldo sin ingreso, E recupera.
6. Window > General > Test Runner: Run All EditMode y PlayMode fuera de las
   pruebas manuales. Para el recorrido de cocina, consultar VERTICAL-SLICE-POLISH.md.

## Límites

Huella por AABB del collider agregado en XZ y tolerancia de apoyo por altura,
apropiadas para el pad horizontal y primitivas del prototipo. La rotación usa una
aproximación conservadora; no es cálculo exacto de área de contacto ni detector de
reposo prolongado. Un roce pequeño no basta, y un plato todavía moviéndose debe
frenar. No se introduce snap/teleporte de entrega ni una nueva tecla.

## Archivos

Nuevo: `Docs/DELIVERY-TOLERANCE-FIX.md`.

Modificados:

```text
Assets/_Project/Scripts/Runtime/Orders/DeliveryZone.cs
Assets/_Project/Editor/M1GreyboxBuilder.cs
Assets/_Project/Scenes/PrototypeRestaurant.unity
Assets/_Project/Tests/EditMode/M6ServiceSceneTests.cs
Assets/_Project/Tests/PlayMode/M6ServiceTests.cs
Assets/_Project/Tests/PlayMode/M6PrototypeTests.cs
Docs/ARCHITECTURE.md
Docs/ROADMAP.md
Docs/M6.md
Docs/VERTICAL-SLICE-POLISH.md
README.md
```
