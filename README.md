# La Valija Perdida

**Una aventura de sopas de letras.**

Carlos Ríos, cazarrecompensas, perdió su valija con un mapa del tesoro. Para recuperarla tiene que descifrar las palabras clave de su anotador: cada sopa de letras resuelta revela pistas que deciden a dónde ir y cómo termina la historia.

Proyecto académico de la Tecnicatura en Programación, materia **Diseño y Desarrollo de Videojuegos I** (UCES, 3.er bimestre 2026, Prof. Angel Lugo).

## Resumen

| | |
|---|---|
| **Género** | Puzzle de palabras + aventura narrativa de investigación |
| **Plataforma** | PC y celulares Android |
| **Motor** | Unity 6 (6000.6.3f1), 2D |
| **Ambientación** | Lejano Oeste ficticio, Sierra Polvorienta, 1887 |

## Equipo

| Integrante | Matrícula | Rol |
|---|---|---|
| Sosa Jorge David | 153203 | Game Design y Narrativa |
| Barbano Sumay Gonzalo | 152127 | Programación |
| Araujo Milagros | 148197 | Arte, UI y Sonido |

## Estado actual del desarrollo (preentrega)

**Implementado**
- Grilla de sopa de letras 6x6 generada por código (prólogo "El tren").
- Selección de letras por arrastre en horizontal, vertical y diagonal, también al revés.
- Validación de palabras y lista de palabras con tachado al encontrarlas.
- Feedback visual por color (normal, en selección, encontrada).

**Planificado**
- Palabras clave que abren caminos de la historia y guion ramificado (3 finales + 1 oculto).
- Mapa por piezas, interrogatorios y elección de destino.
- Herramientas de investigación (lupa y reactivo) y palabras secretas.
- Grillas de hasta 12x12, palabras trampa y reloj del perseguidor.
- Arte final, música y efectos de sonido.
- Sistema de guardado y builds para PC y Android.

## Cómo abrir el proyecto

1. Instalar **Unity Hub** y la versión **Unity 6000.6.3f1** (con *Android Build Support* si se quiere compilar para Android).
2. Clonar este repositorio.
3. En Unity Hub: **Add → Add project from disk** y seleccionar la carpeta clonada.
4. Abrir la escena `Assets/Scenes/Prologo.unity` y presionar **Play**.
5. Para una vista de celular, usar la pestaña **Simulator** o la **Game** en resolución vertical (1080x1920).

## Estructura del repositorio

```
Assets/
  Scenes/     Escenas del juego (Prologo)
  Scripts/    Cell.cs, GridManager.cs
  Prefabs/    Cell (celda de la grilla)
  Art/        Recursos gráficos (en desarrollo)
Packages/
ProjectSettings/
```

## Organización

Trabajamos con Scrum en sprints semanales y un tablero de Trello (Backlog, Por hacer, En progreso, Hecho).
