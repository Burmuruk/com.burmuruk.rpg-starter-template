# Preparación de UPM 0.3.2

Origen: RPGStarterTemplate, commit `542683d` (base anterior `08d1736`).
Destino: rama Test, sobre `6ee24f4`.

Se sincronizaron 29 archivos nuevos o modificados, incluido package.json con la versión 0.3.2 elegida en el proyecto de desarrollo. Se conservaron Samples~, las rutas de carga del paquete, las protecciones de UnityEditor en Runtime y las demás adaptaciones de las publicaciones anteriores. Los cambios locales existentes en README.md se conservaron.

## Contenido

- Ampliación de etiquetas y configuración de personajes, con utilidades para crear tags y layers.
- Actualización de creación de personajes y ajustes de buffs y armaduras.
- Ajustes del controlador del jugador y su implementación del demo.
- Nuevo prefab EnemyManager y actualización de Core.
- Nuevo prefab Controls, cambios de interfaz y actualización de escenas y prefabs del demo.
- Retirada del campo público de prueba BasicStats.testDir: cualquier código externo que lo utilizara deberá adaptarse.

## Validación

- 37 UXML bien formados, sin rutas literales inexistentes del paquete en los scripts del editor, UXML y USS revisados.
- Todos los .meta del origen se conservaron (excepto Samples.meta exterior, omitido intencionalmente).
- Unity 2022.3.17f1: importación real del sample a `Assets/Samples/RPG Starter Template/0.3.2/Demo Scene`, correcta.
- Compilación de editor y sample, carga e instanciación de 37 UXML y comprobación de rutas de CoreAssets/mapas importados, correctas.
- Compilación de scripts para Windows Player: 17 ensamblados, correcta. Ambas ejecuciones finalizaron con código 0.
- Logs locales ignorados por Git: `Validation~/import-0.3.2.log` y `Validation~/validate-0.3.2.log`. El sample anterior se conservó fuera de Assets en `Validation~/Samples-before-0.3.2`.

No se modifica el formato de los archivos de guardado ni se añade una versión dentro de ellos como parte del empaquetado. La limitación de navegación del sample fuera del editor documentada anteriormente permanece.

Antes de publicar, probar en Play Mode la configuración de personajes, tags/layers, controles, interfaz, combate y guardado/carga. La compilación de scripts no sustituye esa prueba ni una build ejecutable.

Los cambios quedan para revisión y commit en Test. No se realiza push ni se modifica main durante esta preparación. El tag previsto es v0.3.2.
