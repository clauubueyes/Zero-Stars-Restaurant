# ADR 0026: arte modular separado de la geometría funcional

- Estado: implementado para VP1B/C; aceptación artística humana pendiente.
- Base: VP1A `db39056`, sobre gameplay M1–M18.
- Entrega: `C:/Users/Usuario/Zero Stars Restaurant M15`.

## Contexto

El usuario autoriza un visual/art pass de realismo indie atmosférico sobre su
distribución actual. Los assets existentes son primitivas y materiales planos.
El layout y las superficies térmicas, sanitarias y físicas tienen prioridad;
reconstruir el greybox o sustituir colliders rompería ese contrato.

## Decisión

1. Añadir hijos `Visual_VP1BC` y ocultar sus renderers anteriores, manteniendo
   objetos, transforms, física y scripts originales. Las superficies visuales
   de Food/Grill/Prep coinciden con los apoyos existentes; ninguna pieza nueva
   lleva collider, trigger, Rigidbody o Light.
2. Generar localmente materiales URP/Lit con albedo, normal y máscara de metal/
   smoothness. Variación moderada y texturas repetibles; unidades UV métricas
   para evitar el estiramiento de los cubos. Reutilizar materiales/meshes y
   habilitar instancing. No descargar assets ni añadir dependencias o Blender.
3. Mantener gabinetes abiertos: puerta decorativa fija junto al lateral,
   compresor/tirador y acabados distintos. El acceso, Shelf, ThermalInterior
   y ColdStorage no cambian. No anticipar puertas funcionales.
4. Adaptar formas Food/Plate dentro de los bounds originales. FoodStageVisual
   consulta el mismo CookingState y cambia material compartido al alcanzar
   Cooked/Burnt. No recrear estados, drivers, identidades ni recetas. Raw y
   Undercooked comparten visual; Cooked y Overcooked comparten el suyo.
5. La suciedad estática es textura ambiental moderada. DirtSurfaceView, sus
   manchas y DirtState M17 permanecen intactos, por encima del apoyo original.
   Cooking, contaminación y limpieza conservan sus reglas.
6. Mantener fuentes VP1A, suministro y parámetros. Un Volume propio sobreescribe
   solo exposición (+0,85 EV frente a +0,25 EV); desactivar sombras de la campana
   decorativa evita tapar la fuente anterior. Sin nuevas luces, consumo o bake.
7. El instalador preserva documentos/fileIDs originales y orden de hijos,
   añadiendo solo referencias nuevas y enabled de renderers. Conservar los GUID
   de prefabs. Una repetición retorna sin modificar el arte instalado, incluidas
   ediciones manuales posteriores. No ejecutar Rebuild.

## Consecuencias

La física conserva las cajas originales, incluso donde la apariencia es redonda
o el mueble tiene patas: límite explícito de este pass. Las encimeras usan cuerpos
cerrados para mantener legible el volumen bloqueante. El arte es provisional y
requiere aceptación humana; recuentos de piezas no demuestran FPS.

La comparación Art ON/OFF es de desarrollo, independiente de la comparación
Atmosphere de VP1A. Los prefabs comprados conservan sus nuevas formas; la
comparación de arquitectura no pretende reescribir unidades vivas. Ningún cambio
en M14–M18, nuevo milestone, tienda, decoración comprable, personajes o UI final.

Validación e inventario completos en [VP1BC](../VP1BC.md).
