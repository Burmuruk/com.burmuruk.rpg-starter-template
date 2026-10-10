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
    
       * Shader Graph (Demo)

       * Cinemachine (Demo)

       * InputSystem (Demo)

       * Newtonsoft.Json (Sistema de guardado)
    

## Instalación

1. En tu proyecto de Unity, abre `Window > Package Manager`
2. Selecciona Add package from Git URL…

```json
  https://github.com/Burmuruk/com.burmuruk.rpg-starter-template.git"
```

  O bien, descarga este proyecto y selecciona Add package from disk. Abre "package.json" de la raíz
3. Al abrir Unity por primera vez tras la instalación, aparecerá un mensaje preguntando si deseas copiar los assets básicos
4. Seleccionar Yes si se desea copiar los archivos muestra
5. (Opcional) Importa la escena demo desde la sección Samples del Package Manager


## Ejecución de escena demo

Antes de abrir el demo, importa **Window > TextMeshPro > Import TMP Essential Resources** y configura **Project Settings > Player > Active Input Handling** en **Both**. El demo utiliza recursos de TextMeshPro y ambos sistemas de entrada. Usa un proyecto con URP configurado.

Para abrir es demo incluido debes seguir los siguientes pasos:

1. Importar el demo desde el Package Manager
2. Dar clic en RPGTemplate > Setup > Add Demo Scenes to Build
3. Dentro de la carpeta de samples, abrir la escena "MainMenuSample"


### Errores conocidos

- **Modificar enums no actualiza referencias existentes**
  Renombrar, eliminar o reordenar valores de enums usados por el sistema puede causar pérdida de referencias en ScriptableObjects y escenas.
  
  *solución alternativa:* modificar enums únicamente al inicio del proyecto o solo agregar nuevos valores, no reordenarlos.

  - **No es posible navegar por escaleras o pendientes muy inclinadas**
  No se detectan bien las esquinas de escaleras o las resultantes de juntar colliders.
  
  *solución alternativa:* Reducir la pendiente o modificar la densidad de la malla.

- **Limitación en sistema de Buffs**
  Buffs desconectados del sistema de guardado.
  (Comportamiento desconectado por cambio en proceso.)

  - **No hay Carga de datos temporales en demo**
  No todos los valores se cargan al regresar a una escena ya completada.
  
