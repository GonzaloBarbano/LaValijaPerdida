# La Valija Perdida

**Una aventura de sopas de letras.**

Carlos Ríos, cazarrecompensas, perdió su valija con el mapa de la Mina Perdida. Para recuperarla tiene que descifrar las palabras clave de su anotador: cada sopa de letras resuelta revela pistas que deciden a dónde ir y cómo termina la historia.

Proyecto académico de la Tecnicatura en Programación, materia **Diseño y Desarrollo de Videojuegos I** (UCES, 3.er bimestre 2026, Prof. Angel Lugo).

## Jugar sin abrir Unity

La build para Windows (.exe) está en la sección **Releases** de este repositorio (o en la carpeta `Build-Windows`). Descomprimir el .zip y ejecutar `La Valija Perdida.exe`. El juego se controla con el mouse: hacer clic en la primera letra y arrastrar hasta la última. `Esc` abre la pausa.

## Resumen

| | |
|---|---|
| **Género** | Puzzle de palabras + aventura narrativa de investigación |
| **Plataforma** | PC (Windows) y Android |
| **Motor** | Unity 6 (6000.6.3f1), 2D, URP |
| **Ambientación** | Lejano Oeste ficticio, Sierra Polvorienta, 1887 |

## Equipo

| Integrante | Matrícula | Rol |
|---|---|---|
| Sosa Jorge David | 153203 | Game Design y Narrativa |
| Barbano Sumay Gonzalo | 152127 | Programación |
| Araujo Milagros | 148197 | Arte, UI y Sonido |

## Estado del desarrollo (2.º parcial)

**Implementado y jugable**

- Flujo completo: portada, menú, prólogo tutorial, 4 capítulos, mapa, interrogatorios, deducciones y finales.
- Sopa de letras generada desde datos: grillas de 6x6 a 12x12, con una dirección nueva por capítulo (horizontal, vertical, diagonal, al revés).
- Selección por arrastre con "imán" a la dirección recta más cercana, validación y estados de color.
- Palabras clave que avanzan la historia y bifurcación en la cantina (SOCIO o LADRONA).
- Lupa (primera letra), reactivo (tinta invisible), 10 palabras secretas, palabras trampa en las deducciones.
- Reloj del perseguidor en el capítulo 4.
- Ajuste dinámico de dificultad: la lupa titila si el jugador se traba, ayuda tras 3 errores seguidos, lupa extra por racha rápida.
- 3 finales + 1 final oculto (las 10 palabras secretas).
- Guardado automático (JSON) y opción "Continuar".
- Música de guitarra y efectos sintetizados por código, con volumen regulable.
- Interfaz diegética (el anotador de Carlos) con la paleta del GDD, en vertical 9:16.

**Pendiente**

- Arte final ilustrado (retratos, lugares, mapa). El juego ya carga automáticamente las imágenes que se agreguen en `Assets/Resources/Arte` (`portada`, `mapa`, `retrato_carlos`, `retrato_carrizo`, `retrato_rosa`, `retrato_vidal`).
- Música grabada (se reemplaza agregando archivos en `Assets/Resources/Audio`: `musica_calma`, `musica_tension`, `musica_final`, `sfx_acierto`, etc.).
- Build para Android y pruebas en celular.
- Logros y versión en inglés (deseables).

## Cómo abrir el proyecto

1. Instalar **Unity Hub** y la versión **Unity 6000.6.3f1**.
2. Clonar este repositorio y en Unity Hub elegir **Agregar → Agregar proyecto desde el disco**.
3. Abrir la escena `Assets/Scenes/Juego.unity` y presionar **Play**.
4. Para ver como en el celular, poner la pestaña **Game** en una resolución vertical (1080x1920).

## Estructura del código

```
Assets/Scripts/Juego/
  Juego.cs       Flujo de pantallas y bucle de juego (arranca solo en la escena "Juego")
  SopaView.cs    Grilla interactiva: arrastre, validación, lupa, reactivo y ayudas
  Generador.cs   Genera cada grilla a partir de sus palabras y direcciones habilitadas
  Contenido.cs   Capítulos, sopas, interrogatorios, deducciones y finales (datos)
  Datos.cs       Modelo de datos, estado de la partida y guardado en JSON
  Sonido.cs      Música y efectos sintetizados (reemplazables por archivos)
  UI.cs          Paleta del GDD y funciones para construir la interfaz por código
Assets/Scripts/  Cell.cs y GridManager.cs: primer prototipo (preentrega), escena Prologo
```

## Organización

Trabajamos con Scrum en sprints semanales y un tablero de Trello (Backlog, Por hacer, En progreso, Hecho).
