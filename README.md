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

   * Unity 2022.3+ (compatibildad con Unity 6 no validada)

   * Dependencias (añadidas automáticamente)

       * TextMeshPro (Demo)

       * Cinemachine (Demo)

       * InputSystem (Demo)

       * Newtonsoft.Json (Sistema de guardado)
    

## Instalación

1. En tu proyecto de Unity, abre `Window / Package Manager`.
2. Agrega paquete con git URL:

```json
  "com.burmuruk.rpg-starter-template"
```
3. Copiar el contenido de la carpeta "GameArchitecture" en tu proyecto, ya sea manualmente o aceptando el mensaje que saldrá al inicio:
```json
   "Do you want to copy the base files to Assets/GameArchitecture?"
```

## Estructura

    ├── com.Burmuruk.RPG-Starter-Template/

      ├── GameArchitecture/          # Copiar a Assets

      ├── Tool/                      # Archivos de editor

    └── Samples/                     # Escena de ejemplo
    ├── Results/                     # Carpeta por defecto para ScriptableObjects y prefabs generados

    ├── StreamingAssets/             # Archivos de navegación generados

