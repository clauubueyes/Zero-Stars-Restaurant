# ADR 0002: jugador FPS y greybox M1

Fecha: 2026-10-06. Estado: aceptada para M1.

## Contexto

M1 necesita navegación FPS con colisiones verificables y una escena reproducible,
conservando el Input System y los assets de la foundation. No requiere interacción
ni simulación de restaurante. El Editor del usuario está abierto.

## Decisión

- Usar CharacterController para locomoción, sin Rigidbody del jugador ni empuje
  físico de objetos. Gravedad explícita y salto sencillo configurable; altura
  cero desactiva el salto. La cápsula mide 1.8 m y la cámara está a 1.65 m.
- Separar intención/input/mirada/cursor (`FirstPersonController`) de locomoción
  física (`FirstPersonMotor`). No añadir interfaces, sistema de servicios ni Domain.
- Instanciar el asset de input existente por jugador; habilitar solo Move, Look y
  Jump, con máscara Keyboard&Mouse. No modificar bindings de plantilla, habilitar
  Interact/Attack ni usar PlayerInput para todo el mapa. Escape/clic son acciones
  locales de cursor del adaptador, no acciones de interacción con el mundo.
- Leer mouse delta como desplazamiento, sin multiplicarlo por deltaTime. Limitar
  pitch y velocidad diagonal; mantener gravedad cuando se libera el cursor.
- Introducir assemblies Runtime, Editor y tests en M1, antes de lo previsto en la
  foundation: el generador y las pruebas ya necesitan referenciar código real.
  Domain se mantiene vacío hasta que haya reglas de negocio.
- Generar una escena aditiva con cubos y tres materiales URP/Lit sin texturas. El
  menú requiere confirmación antes de reemplazar la escena guardada y exige que
  PrototypeRestaurant esté cerrada. Preserva otras escenas y materiales existentes.
  No hay generación automática al importar ni cambios de Build Settings.
- Validar en una copia aislada e ignorada bajo Build; no iniciar una segunda
  instancia de Unity sobre la carpeta que ya tiene abierta el usuario.

## Consecuencias

El jugador puede moverse y probar colisiones sin conocer la forma o materiales del
restaurante. Las pruebas de locomoción suministran intención/tiempo explícitos y
ejecutan CharacterController real, sin simular input humano. Las pruebas del input
verifican su activación/desactivación y que no se modifica el asset compartido.

El generador reproduce objetos, dimensiones, jerarquía y referencias. Unity puede
asignar otros fileID locales al reconstruir; no se promete YAML idéntico byte a
byte. Los GUID de assets existentes se conservan. La luz de desarrollo no implica
electricidad del restaurante; no hay ningún sistema económico o eléctrico en M1.

Al recompilar, Unity serializó `overrideShaderVariantLimit: 0` en los ajustes de
Shader Graph. Ese valor por defecto ausente en la baseline se registra en un
commit de mantenimiento independiente; no cambia paquetes ni el pipeline.
