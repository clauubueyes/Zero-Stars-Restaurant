# ADR 0025: iluminación atmosférica sobre la escena actual y suministro M12

- Estado: implementado para VP1A; aceptación visual humana pendiente.
- Base: M18 validado `0cdaab3`.

## Contexto

El restaurante usa geometría manual actual, materiales provisionales y una luz
direccional plana. VP1A debe aportar iluminación funcional fría, rincones con
menos luz y un techo oscuro sin redistribuir el local, cambiar gameplay o anticipar
VP1B. M12 posee el suministro y tres aparatos con un contrato térmico explícito.

## Decisión

1. Añadir una única raíz visual, sin colliders. Siete luminarias de primitivas con
   tubos emisivos y spots coincidentes. Dos paneles de techo nuevos siguen los
   bounds actuales de las paredes; dos spots de luz exterior tenue siguen puertas.
   No modificar ningún transform existente ni sustituir materiales anteriores.
2. PoweredLightFixture es un adaptador de presentación con referencia explícita
   al mismo RestaurantElectricity. Lee IsOn; apaga Light/emission cuando no hay
   suministro o se deshabilita. No almacena otro estado eléctrico, crea switches,
   cobra ni avanza simulación. Cuatro de nueve luces usan sombras; todas realtime.
3. No forzar luminarias dentro de ElectricalAppliance: requiere HeatSource y
   cambiaría el modelo de consumo/facturación. Los tres medidores térmicos siguen
   intactos; metering de luz se difiere. Emisión sin baked GI para que un corte
   no deje iluminación eléctrica precalculada.
4. Ambiente Trilight tenue, preset de tarde/noche separado de GameTime. Volume
   local al proyecto con ACES, exposición/color moderados, bloom pequeño y
   vignette leve. Se aprovecha SSAO PC existente sin tocar el renderer global.
5. RestaurantAtmosphere permite comparación de desarrollo y conserva los campos
   originales de ambiente/cámara/luz direccional. OFF restaura el referente y
   oculta solo la geometría del pass; ON respeta el suministro actual. No hay HUD
   nuevo ni opción destinada al jugador. RenderSettings solo se escribe si la
   escena del componente es la activa, conservando escenas aditivas ajenas.
6. Instalación incremental e idempotente. Unity serializa una copia temporal y
   solo se combinan nuevos documentos y campos concretos de luz/postprocesado.
   Los bloques existentes de física/gameplay y transforms se conservan idénticos.
   Repetir no recalcula ni sobrescribe luminarias editadas por el usuario.

## Consecuencias y límites

El pass depende únicamente de paquetes URP/Core ya presentes. Domain y las reglas
de M1–M18 no cambian. No hay bake, nuevas texturas/modelos, niebla, flicker, ciclo
solar o probes realtime. Es un primer ajuste sobre primitives; el envejecimiento
material y la arquitectura final siguen en VP1B/VP1C.

Las regresiones completas y pruebas nuevas verifican suministro, emisión,
comparación y preservación. Capturas reales de URP sirven para revisar lectura,
pero requieren aceptación humana. No se infiere rendimiento en FPS de contar
luces: ver configuración, resultados e inventario en [VP1A](../VP1A.md).
