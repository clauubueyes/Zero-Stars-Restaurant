# M5: corrección de reconocimiento y confirmación durante montaje

Rama `fix/m5-assembly-feedback`, desde `1071d68`. Sin merge/push ni M6.
El working tree estaba limpio al comenzar. Unity 6000.5.3f1, URP 17.5.0,
Input System 1.19.0 y Test Framework 1.7.0; sin actualizar dependencias/settings.

## Causa y comportamiento corregido

El collider sólido del pan superior recibe el primer raycast, ocultando la
bandeja. Ese comportamiento es necesario para retirar ingredientes con M2.
El feedback anterior solo consultaba platos al detectar directamente
AssemblySurface, y el reconocimiento estaba dentro de un panel con scroll.
La pestaña lateral pequeña era difícil de descubrir; el prompt de confirmación
decía Finalize dish Assembly 1, sin nombrar la composición.

Ahora `DishAssemblyInteraction` consulta el primer hit M2 y resuelve su montaje
por referencia al FoodState original, entre superficies explícitas del Inspector.
No hace raycasts que atraviesen ingredientes/paredes ni búsquedas globales.
Si el primer hit es una pared u objeto ajeno, está fuera del alcance, o la unidad
no pertenece al borrador, no ofrece ese montaje. Desactivar la superficie también
lo excluye. Reconocimiento/composición siguen siendo las reglas Domain M5.

- Mirar un componente del borrador muestra **Recognized: Hamburger** y
  **[F] Finalize Hamburger** encima de la mira, fuera del scroll.
- **E** mantiene el pickup individual antes de confirmar, para retirar/reorganizar.
  **F** confirma con manos libres mirando cualquier componente de ese montaje.
  Si las manos están ocupadas se explica que primero hay que colocar/soltar.
  La bandeja vacía pide ingredientes y no puede confirmarse.
- E sobre bandeja/pestaña libre también confirma y ahora muestra
  **[E] Finalize Hamburger**, Cheeseburger o Custom Dish según el borrador.
- Después de confirmar, la misma dirección de raycast detecta el Pickup raíz:
  **[E] Pick up Hamburger**. Pickups/colliders individuales están deshabilitados,
  conservando todas las unidades/estados originales. Soltarlo conserva el nombre.

Player/FinalizeDish en el InputActionAsset existente usa F, configurable allí.
El adaptador clona estado de acciones y lo libera al desactivarse/destruirse;
respeta control/cursor M1 y prioridad M2 ante pulsaciones simultáneas E/G/Throw.
M2 permanece genérico: solo DisplayName pasa a virtual para el nombre dinámico
de AssemblySurface. No se cambia detector, portador, motor, comida, cocción ni
reglas de dominio. Véase [ADR 0006](Decisions/0006-physical-dish-assembly.md).

## Comprobación manual

1. Detener Play, guardar trabajo propio y esperar compilación. Reabrir la escena
   `Assets/_Project/Scenes/PrototypeRestaurant.unity` actualizada desde disco;
   no guardar encima una versión antigua aún abierta. No requiere reconstrucción.
2. Play/enfocar Game, formar pan → carne → pan sobre una bandeja del fondo izquierdo,
   usando E/G. Esperar a que se estabilice. Puede usarse la misma carne cocinada M4.
3. Apuntar directamente al **pan superior**: comprobar Recognized: Hamburger y
   [F] Finalize Hamburger, aunque el prompt M2 siga ofreciendo recoger ese pan con E.
   Si dice Custom Dish, revisar orden/alineación y que las tres unidades estén
   dentro del volumen. No se fuerza reconocimiento de una pila inválida.
4. Retirar/recolocar con E/G para comprobar actualización. Con manos libres,
   mirar el pan y pulsar F: ahora debe aparecer [E] Pick up Hamburger al mirar el
   conjunto. E lo recoge. Caminar, G, volver a apuntar: nunca Pick up Bun.
5. Probar Cheeseburger y Custom Dish. Confirmar vacío/con manos ocupadas falla;
   paredes, objetos ajenos y distancia bloquean el contexto. Escape libera el
   cursor y F no confirma sin control; clic izquierdo recaptura. Revisar Console.

El agarre M2 mantiene su esfera conservadora y puede rechazar recoger si se
está demasiado cerca del banco o mirando muy abajo. Dar un paso atrás y mirar
el componente superior; no se retiraron límites de colisión para forzar agarres.
Las limitaciones de pila/proxy/desmontaje de [M5](M5.md) permanecen.

## Validación

En una copia nueva `Build/M5FeedbackValidation`, sin reutilizar Library y sin abrir
otro Editor sobre el proyecto del usuario:

- **EditMode: 86/86**, sin fallos ni omitidos. Incluye referencias nuevas en escena,
  superficies explícitas y Player/FinalizeDish con F; conserva regresiones M1–M5.
- **PlayMode: 55/55**, sin fallos ni omitidos. 51 anteriores y cuatro casos nuevos
  cargando PrototypeRestaurant: pila física estabilizada/occlusión del pan,
  reconocimiento y confirmación, raycast Pickup del agregado antes/después de
  soltar, conservación de componentes y colliders individuales deshabilitados;
  paredes, objetos ajenos, alcance/manos ocupadas; lifecycle de acciones propias;
  recepción de F con teclado virtual y rechazo sin control de gameplay.
- La escena entregada coincide semánticamente con los **360 documentos** de la
  generada/testada, conservando todos los fileID/GUID y colliders anteriores.
  Se añaden únicamente 22 líneas: componente de montaje y sus referencias.
  Las cuatro pruebas de flujo se repitieron sobre ese archivo exacto: **4/4**.
  Auditoría: 116 GUID únicos, referencias propias correctas, fuentes idénticas a
  las validadas, main intacto y ningún generado versionado.
- Las primeras pruebas fallaron por distancia insuficiente para agarre M2 y por
  asumir que el asset compartido estaba desactivado. La prueba de teclado requirió
  settings temporales que ignoran foco para batch; se clonan/restauran y no se
  modifica configuración de proyecto. Ningún límite físico M2 se eliminó.

XML locales ignorados: `TestResults/M5Feedback-EditMode.xml`,
`TestResults/M5Feedback-PlayModeComplete.xml`, `TestResults/M5Feedback-DeliveredScene.xml`.
Logs locales ignorados: `Logs/M5FeedbackGenerateNative.log`,
`Logs/M5FeedbackEditMode.log`, `Logs/M5FeedbackPlayModeComplete.log`,
`Logs/M5FeedbackDeliveredScene.log`.

La revisión visual y pulsación F con Game view enfocada requieren comprobación
manual: el Editor batch/nographics no captura el cursor como una sesión interactiva.

## Inventario exacto

Nuevos (5):

```text
Assets/_Project/Scripts/Runtime/Dishes/DishAssemblyInteraction.cs
Assets/_Project/Scripts/Runtime/Dishes/DishAssemblyInteraction.cs.meta
Assets/_Project/Tests/PlayMode/M5AssemblyFocusTests.cs
Assets/_Project/Tests/PlayMode/M5AssemblyFocusTests.cs.meta
Docs/M5-FEEDBACK-FIX.md
```

Modificados (10):

```text
Assets/InputSystem_Actions.inputactions
Assets/_Project/Editor/M1GreyboxBuilder.cs
Assets/_Project/Scenes/PrototypeRestaurant.unity
Assets/_Project/Scripts/Runtime/Dishes/AssemblySurface.cs
Assets/_Project/Scripts/Runtime/Dishes/DishInspectionFeedback.cs
Assets/_Project/Scripts/Runtime/Interaction/Interactable.cs
Assets/_Project/Tests/EditMode/M5DishSceneTests.cs
Docs/Decisions/0006-physical-dish-assembly.md
Docs/M5.md
README.md
```

Sin borrados, cambios de dependencias/settings ni sistemas futuros.

Commits:

- `91df1c0` — `fix: expose assembly recognition and finalize intent through ingredient focus`.
- Cierre documental: `docs: clarify M5 recognition confirmation and manual validation`.

Historial exacto: `git log --oneline 1071d68..fix/m5-assembly-feedback`.
