# Preparación de UPM 0.4.0

Origen: RPGStarterTemplate, commit e904ef7 más los cambios locales de diálogo sin commit presentes al preparar esta actualización. Destino: Test sobre 04b1a03. La versión se actualizó a 0.4.0 en ambos package.json.

## Cambios

- Nuevos DialogueCharacter y DialogueController sustituyen AIConversant y PlayerConversant; se retiran los scripts anteriores y sus metadatos.
- Actualización de triggers, inspector, grafo de diálogo y HUD para los nuevos componentes.
- Datos de diálogo en la configuración de personajes y ajustes de creación de personajes/modelos.
- Actualización del prefab CoreSample y de OutdoorsScene.
- Se conserva el empaquetado Samples~, las rutas UPM y las protecciones de UnityEditor. Los solapamientos se resolvieron incorporando la lógica actual del origen, incluida la actualización de Character.FixedUpdate.

## Migración

Hay cambios incompatibles de API y de componentes serializados. En proyectos existentes, adaptar los scripts que usaban AIConversant/PlayerConversant a DialogueCharacter/DialogueController y revisar los componentes y sus referencias en escenas y prefabs propios. Los scripts nuevos tienen GUID nuevos; no asumir que los componentes antiguos se migran automáticamente.

Para probar el demo, respaldar los cambios propios de samples anteriores, retirarlos de Assets e importar el sample 0.4.0. Las escenas y prefabs exportados no contienen referencias a los GUID de los dos scripts retirados.

No se añadió versionado dentro de los archivos de guardado. La compatibilidad con guardados antiguos no está validada.

## Validación

- Unity 2022.3.17f1: importación real del sample 0.4.0 correcta.
- Compilación de editor y sample, carga e instanciación de 37 UXML y comprobación de rutas físicas de CoreAssets/mapas correctas.
- Compilación para Windows Player correcta: 17 ensamblados. La comprobación final terminó con código 0 y sin errores de compilación.
- En las primeras ejecuciones Unity regeneró referencias de caché a los dos scripts retirados; se repitió la validación para confirmar que desaparecieron esos errores transitorios.
- Todos los .meta actuales del origen se conservaron (salvo Samples.meta exterior). La revisión estática no encontró rutas literales del paquete inexistentes en los scripts del editor, UXML y USS examinados.
- Logs locales: Validation~/import-0.4.0.log y Validation~/validate-0.4.0-final.log. El sample previo se respaldó fuera de Assets en Validation~/Samples-before-0.4.0.

Las comprobaciones de compilación no sustituyen Play Mode ni una build ejecutable. Antes de publicar, probar inicio y cierre de conversaciones, opciones, acciones de entrada/salida, HUD y creación de personajes. Permanece la limitación de navegación del sample fuera del editor documentada anteriormente.

Cambios pendientes de commit y push; main no se modifica. Tag previsto: v0.4.0.
