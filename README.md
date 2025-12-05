# RPG Starter Template

**RPG Starter Template** es una herramienta diseñada para agilizar la creación de un juego tipo RPG en Unity. Incluye una arquitectura básica extensible para el juego y herramientas de editor para configurar personajes, estadísticas y más.

## Características

- Arquitectura base para creación de juego RPG.
- Creación de NavMesh para agentes voladores (en desarrollo) y no voladores.
- Sistema de guardado por slots, con información de preview editable.
- Creación y configuración de personajes, estadísticas, armas, etc. mediante ventanas.
- Sistema para recolectar, tirar y spawnear objetos.
- Sistema de buffs.
- Soporte para progresión de niveles.
- Prefabs y ScriptableObjects generados automáticamente.
- Sistema de diálogos y misiones (en desarrollo) con editor de nodos.
- Sistema genérico de inventario.
- Sistema de patrullaje.
- Sistema genérico para interacciones.
- Agentes con movimiento base (A* / Dijkstra) y detección de enemigos. 
- Escena demo.



## Requisitos

   * Unity 2022.3+ (compatibilidad con Unity 6 no validada)

   * URP 3D (Universal Render Pipeline)
El demo y varios materiales usan URP.

   * Dependencias (se instalan automáticamente)

       * TextMeshPro (Demo)

       * Cinemachine (Demo)

       * InputSystem (Demo)

       * Newtonsoft.Json (Sistema de guardado)
    

## Instalación

1. En tu proyecto de Unity, abre `Window > Package Manager`.
2. Selecciona Add package from Git URL…

```json
  "https://github.com/Burmuruk/com.burmuruk.rpg-starter-template.git"
```
3. Al abrir Unity por primera vez tras la instalación, aparecerá un mensaje preguntando si deseas copiar la arquitectura base.
   Selecciona la ruta de tu preferencia.
4. Selecciona Yes para importar los archivos necesarios.
5. (Opcional) Importa la escena demo desde la sección Samples del Package Manager.

## Estructura

    ├── com.Burmuruk.RPG-Starter-Template/

      ├── GameArchitecture/          # Base del juego (se copia a Assets/)

      ├── Tool/                      # Archivos de editor

    └── Samples~//RPGStarterDemo/    # Escena y contenido de demostración
    ├── RPGResults/                     # Carpeta por defecto para ScriptableObjects y prefabs generados

    ├── StreamingAssets/             # Archivos de navegación generados


### Known Issues

- **Pérdida de datos al editar diálogos**
  Al abrir un mismo archivo de diálogo varias veces, el editor limpia ciertos campos y los cambios pueden perderse.
  
  *solución alternativa:* cerrar y volver a abrir la ventana antes de editar de nuevo.

- **Datos del inventario se sobrescriben**
  Si se realizan acciones que recargan datos sin guardar, la información actual puede sobrescribirse.
  
  *solución alternativa:* asegúrate de guardar los cambios antes de ejecutar operaciones que recarguen datos.

- **Modificar enums no actualiza referencias existentes**
  Renombrar, eliminar o reordenar valores de enums usados por el sistema puede causar pérdida de referencias en ScriptableObjects y escenas.
  
  *solución alternativa:* modificar enums únicamente al inicio del proyecto o solo agregar nuevos valores, no reordenarlos.

- **Limitación en sistema de Buffs**
  Solo puede estar activo un buff del mismo tipo por caller.
  (Comportamiento actual por diseño; sujeto a expansión futura.)
  
