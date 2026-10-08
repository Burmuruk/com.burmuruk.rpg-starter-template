# Preparación de UPM 0.3.0

Origen: `RPGStarterTemplate`, commit `e747a8b` (rama `House`).
Destino: rama local `Test`, actualizada por avance rápido a `main` (`08480b0`) antes de copiar.

## Cambios respecto al paquete del proyecto de desarrollo

- Copiar el contenido de `Assets/com.burmuruk.rpg-starter-template` a la raíz del repositorio UPM, sincronizando las carpetas para retirar archivos antiguos.
- Exportar `Samples` como `Samples~`, sin el `Samples.meta` exterior; conservar los GUID de todos los assets internos.
- Actualizar `package.json` a versión `0.3.0` y mantener `samples.path` en `Samples~/RPGStarterDemo`.
- Cambiar las rutas de lectura del editor desde `Assets/com.burmuruk.rpg-starter-template/` a `Packages/com.burmuruk.rpg-starter-template/`. Las rutas de escritura de contenido del usuario permanecen en `Assets`.
- Actualizar las referencias `project://database` de los UXML y corregir sus ubicaciones obsoletas buscando el GUID correspondiente. No regenerar GUID.
- Resolver `NavigationMaps` desde la ubicación del script del sample importado en los dos overrides `LoadNavigationMap`. Esto evita depender del nombre o la versión de la carpeta de importación.
- Proteger con `UNITY_EDITOR` los imports y usos de UnityEditor en Runtime, incluidas las llamadas a `AssetDatabase.Refresh` en `NavSaver` y el atributo `DrawGizmo` en `Spline`. Retirar imports no utilizados.
- Usar `PackageInfo.resolvedPath` para copiar físicamente `CoreAssets` desde un paquete local o de caché. Omitir la inicialización automática con diálogo en modo batch.
- Incluir `Assets/Settings/New Lighting Settings.lighting` y su `.meta` del proyecto original dentro de las escenas del sample: dos escenas lo referencian y antes quedaba fuera de la exportación.
- Añadir `.gitignore` para archivos generados y el proyecto aislado de validación.

En el proyecto de desarrollo únicamente se actualizó la versión del paquete a `0.3.0`. Copiarlo otra vez sobre este repositorio sobrescribe estas adaptaciones: deben conservarse o incorporarse al flujo de exportación.

La versión `0.3.0` señala los cambios en el sistema de guardado; la compatibilidad con guardados anteriores no está garantizada. No se añadió una versión dentro de los archivos de guardado.

## Prueba manual antes de publicar

### Validación automatizada realizada

Las siguientes pruebas se realizaron antes del cambio de numeración, cuando el paquete indicaba `0.2.7`, con Unity 2022.3.17f1 en un proyecto nuevo bajo `Validation~/UnityProject` (ignorado por Git). No se repitieron para el cambio de metadatos a `0.3.0`:

- Instalación como dependencia local UPM y resolución de dependencias: correcta.
- Compilación de editor y del sample importado: correcta.
- Importación real mediante `UnityEditor.PackageManager.UI.Sample.Import`: correcta, a `Assets/Samples/RPG Starter Template/0.2.7/Demo Scene`.
- Carga e instanciación de los 37 `VisualTreeAsset` del paquete: correcta.
- Existencia del directorio físico CoreAssets y de los NavigationMaps importados: correcta.
- Compilación de scripts para StandaloneWindows64 mediante `PlayerBuildInterface.CompilePlayerScripts`: correcta, 17 ensamblados, incluidos Runtime y el sample.
- Ambas ejecuciones finales terminaron con código 0. Logs locales: `Validation~/import.log` y `Validation~/validate.log`. El validador está en `Validation~/UnityProject/Assets/Editor/PackageValidation.cs`.
- Revisión estática: 37 UXML bien formados, sin rutas literales inexistentes del paquete en los scripts del editor/UXML/USS; sin referencias restantes a `Assets/com.burmuruk...` en los archivos adaptados.

Esta prueba no construye un ejecutable ni ejecuta Play Mode. Hay advertencias de compilación de código existente (por ejemplo, campos/eventos sin uso). Las verificaciones manuales siguientes quedan pendientes.

1. Usar un proyecto separado con Unity 2022.3.17f1 y URP 14.0.9.
2. Instalar este `package.json` con Package Manager > Add package from disk.
3. Importar TMP Essential Resources desde Window > TextMeshPro. El demo referencia LiberationSans SDF; instalar el paquete de TextMeshPro por sí solo no copia esos recursos a Assets.
4. Importar el sample Demo Scene desde Package Manager.
5. Configurar Active Input Handling según el demo: utiliza tanto el Input System como llamadas a `UnityEngine.Input` (Both).
6. Copiar CoreAssets mediante RPGTemplate > Copy Core Assets si no se ofreció automáticamente.
7. Añadir las escenas del demo con RPGTemplate > Setup > Add Demo Scenes to Build.
8. Probar personajes, equipo, diálogos, navegación, guardado y carga en Play Mode.
9. Probar un ejecutable antes de anunciar compatibilidad del demo en builds.

Los overrides de navegación del sample siguen siendo exclusivos del editor, como en el origen. Compilar scripts de Player no valida la navegación del demo en un ejecutable. No se modificó ese comportamiento en esta preparación.

## Publicación

Revisar y hacer commit de los cambios en Test; subir Test y validar la instalación por URL Git. Después integrar a main y crear el tag `v0.3.0`. No se han creado commits de preparación, hecho push, integrado a main ni creado tags durante esta revisión.
