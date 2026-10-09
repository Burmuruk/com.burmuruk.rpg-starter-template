# Preparación de UPM 0.3.1

Origen: `RPGStarterTemplate`, rama `House`, commit `08d1736`.
Base anterior del origen: `e747a8b`. Base del UPM: `b86119d`, rama `Test`.

Se sincronizaron 44 archivos nuevos o modificados del paquete de desarrollo, conservando las adaptaciones UPM de 0.3.0: Samples~, rutas del editor/UXML, protección de UnityEditor en Runtime, resolución de mapas del sample y copia física de CoreAssets. Los dos package.json indican 0.3.1.

## Cambios incluidos

- Ajustes de movimiento, orientación hacia objetivos y recuperación después de caídas.
- Ajustes de IA y pausa de movimiento durante cinemáticas.
- Correcciones de selección y recogida de objetos, e interacciones.
- Restauración de pickups con comprobación de IDs y prefabs disponibles.
- Cambios en animaciones, HUD, prefabs, objetos y escenas del demo; nuevo material YellowTarget.

## Compatibilidad

Aunque se eligió publicar como 0.3.1, hay un cambio de firma pública: `PlayerController.OnPickableEnter` y `OnPickableExit` pasan de `Action<bool, string>` a `Action<bool, string, GameObject>`.

Los proyectos con suscriptores propios deben añadir el tercer parámetro a sus callbacks. Por ejemplo, `void HandlePickup(bool available, string label)` pasa a `void HandlePickup(bool available, string label, GameObject pickup)`. El sample incluido ya está adaptado. La compatibilidad de guardados antiguos no se ha comprobado; no se añadió una versión dentro de los archivos de guardado.
