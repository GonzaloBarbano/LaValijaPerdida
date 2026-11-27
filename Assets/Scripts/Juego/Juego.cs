using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ValijaPerdida
{
    /// <summary>
    /// Arranca el juego automáticamente en la escena "Juego".
    /// No hace falta armar nada a mano en el editor: todas las pantallas se construyen por código.
    /// </summary>
    public static class Arranque
    {
        public const string NombreEscena = "Juego";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (SceneManager.GetActiveScene().name != NombreEscena) return;
            if (UnityEngine.Object.FindAnyObjectByType<Juego>() != null) return;
            new GameObject("Juego").AddComponent<Juego>();
        }
    }

    /// <summary>
    /// Controlador principal: flujo de pantallas y bucle de juego
    /// (llegar a un lugar, abrir la página, resolver la sopa, interrogar y deducir, elegir el próximo destino).
    /// </summary>
    public class Juego : MonoBehaviour
    {
        private RectTransform marco;
        private RectTransform pantalla;
        private RectTransform capaSuperior;
        private Estado estado;
        private Capitulo cap;
        private bool enPartida;
        private bool pausado;

        // estado de la página de sopa
        private SopaView vista;
        private Sopa sopa;
        private int indiceSopa;
        private TextMeshProUGUI txtLista, txtNota, txtReloj, txtSecretas, txtLupas, txtReactivos;
        private Button btnLupa, btnReactivo;
        private float inactivo, tiempoEnSopa, tiempoRestante;
        private int erroresSeguidos, ultimoSegundo;
        private bool relojActivo, pulsandoLupa, sopaTerminada;

        // ================================================================== inicio

        private void Awake()
        {
            Application.targetFrameRate = 60;
#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.Portrait;
#elif UNITY_STANDALONE && !UNITY_EDITOR
            int alto = Mathf.RoundToInt(Screen.currentResolution.height * 0.85f);
            Screen.SetResolution(Mathf.RoundToInt(alto * 9f / 16f), alto, FullScreenMode.Windowed);
#endif
            if (Camera.main == null)
            {
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Paleta.Madera;
            }
            // sin AudioListener no se escucha nada
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UI.Ancho, UI.Alto);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            // mesa de trabajo de Carlos (se ve a los costados en PC)
            UI.Fondo(canvasGo.transform, Paleta.Madera);

            marco = UI.Nodo("Marco", canvasGo.transform);
            marco.anchorMin = marco.anchorMax = new Vector2(0.5f, 0.5f);
            marco.pivot = new Vector2(0.5f, 0.5f);
            marco.sizeDelta = new Vector2(UI.Ancho, UI.Alto);
            marco.anchoredPosition = Vector2.zero;
            marco.gameObject.AddComponent<RectMask2D>();

            Sonido.Inicializar(gameObject);
        }

        private void Start() => Portada();

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && enPartida)
            {
                if (pausado) CerrarCapa(); else Pausa();
            }
            if (pausado || estado == null) return;
            if (enPartida) estado.segundosJugados += Time.deltaTime;
            if (vista == null || sopaTerminada) return;

            tiempoEnSopa += Time.deltaTime;
            inactivo += Time.deltaTime;

            // tutorial: la mano aparece si el jugador no encontró ninguna palabra todavía
            if (sopa.Tutorial && vista.Encontradas.Count == 0 && tiempoEnSopa > 2f) vista.MostrarMano(sopa.Palabras[0]);

            // ajuste dinámico: si el jugador se traba, la lupa titila
            float limite = sopa.Tutorial ? 30f : 60f;
            if (inactivo > limite && estado.lupas > 0 && !pulsandoLupa)
            {
                pulsandoLupa = true;
                StartCoroutine(Pulsar(btnLupa.transform, () => pulsandoLupa));
                Nota("¿Trabado? La lupa muestra la primera letra de una palabra.");
            }

            // reloj del perseguidor
            if (relojActivo)
            {
                tiempoRestante -= Time.deltaTime;
                int s = Mathf.Max(0, Mathf.CeilToInt(tiempoRestante));
                txtReloj.text = $"Perseguidor  {s / 60}:{s % 60:00}";
                txtReloj.color = s <= 30 ? Paleta.Oxido : Paleta.Sombra;
                if (s != ultimoSegundo && s <= 30) Sonido.Reproducir(Sfx.Reloj);
                ultimoSegundo = s;
                if (tiempoRestante <= 0) PerseguidorAlcanza();
            }
        }

        // ================================================================== utilidades de pantalla

        private RectTransform NuevaPantalla(Color fondo)
        {
            CerrarCapa();
            StopAllCoroutines();
            pulsandoLupa = false;
            vista = null;
            relojActivo = false;
            if (pantalla != null) Destroy(pantalla.gameObject);
            pantalla = UI.Llenar(UI.Nodo("Pantalla", marco));
            pantalla.gameObject.AddComponent<Image>().color = fondo;
            pantalla.SetAsFirstSibling();
            return pantalla;
        }

        private RectTransform AbrirCapa()
        {
            CerrarCapa();
            pausado = true;
            capaSuperior = UI.Llenar(UI.Nodo("Capa", marco));
            capaSuperior.gameObject.AddComponent<Image>().color = Paleta.ConAlfa(Paleta.Sombra, 0.88f);
            return capaSuperior;
        }

        private void CerrarCapa()
        {
            if (capaSuperior != null) Destroy(capaSuperior.gameObject);
            capaSuperior = null;
            pausado = false;
        }

        private void Guardar()
        {
            if (estado != null) Guardado.Guardar(estado);
        }

        private void Encabezado(Transform padre, string arriba, string titulo, Color colorTitulo)
        {
            UI.Texto(padre, arriba.ToUpper(), 30, Paleta.Oxido, 50, 50, 980, 44, TextAlignmentOptions.TopLeft, FontStyles.Bold).characterSpacing = 6;
            UI.Texto(padre, titulo, 62, colorTitulo, 50, 92, 980, 90, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        }

        private IEnumerator Pulsar(Transform t, Func<bool> seguir)
        {
            float k = 0;
            while (t != null && seguir())
            {
                k += Time.deltaTime * 6f;
                t.localScale = Vector3.one * (1f + Mathf.Sin(k) * 0.08f);
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        private static string Retrato(string sospechoso)
        {
            if (sospechoso.Contains("Carrizo")) return "carrizo";
            if (sospechoso.Contains("Rosa")) return "rosa";
            if (sospechoso.Contains("Vidal")) return "vidal";
            return "carlos";
        }

        // ================================================================== portada y menú

        private void Portada()
        {
            enPartida = false;
            var p = NuevaPantalla(Paleta.Sombra);
            Sonido.Poner(Musica.Calma);

            var arte = UI.ArteOpcional("portada");
            if (arte != null)
            {
                var img = UI.Llenar(UI.Nodo("Arte", p)).gameObject.AddComponent<Image>();
                img.sprite = arte;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            UI.Panel(p, Paleta.Ocre, 140, 560, 800, 6, false);
            UI.Texto(p, "La Valija\nPerdida", 150, Paleta.Ocre, 0, 620, 1080, 420, TextAlignmentOptions.Center, FontStyles.Bold);
            UI.Panel(p, Paleta.Ocre, 140, 1060, 800, 6, false);
            UI.Texto(p, "Una aventura de sopas de letras", 46, Paleta.Papel, 0, 1100, 1080, 70, TextAlignmentOptions.Center);
            UI.Texto(p, "Sierra Polvorienta, 1887", 36, Paleta.Borde, 0, 1180, 1080, 60, TextAlignmentOptions.Center, FontStyles.Italic);
            var tocar = UI.Texto(p, "Tocá para abrir el anotador", 40, Paleta.Hoja, 0, 1600, 1080, 70, TextAlignmentOptions.Center);
            StartCoroutine(Pulsar(tocar.transform, () => true));

            var b = p.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { Sonido.Reproducir(Sfx.Pagina); Menu(); });
        }

        private void Menu()
        {
            enPartida = false;
            estado = null;
            var p = NuevaPantalla(Paleta.Papel);
            Sonido.Poner(Musica.Calma);

            UI.Texto(p, "ANOTADOR DE CARLOS RÍOS", 30, Paleta.Oxido, 0, 170, 1080, 50, TextAlignmentOptions.Center, FontStyles.Bold).characterSpacing = 8;
            UI.Texto(p, "La Valija Perdida", 96, Paleta.Sombra, 0, 230, 1080, 140, TextAlignmentOptions.Center, FontStyles.Bold);
            UI.Panel(p, Paleta.Borde, 240, 400, 600, 4, false);

            float y = 520;
            if (Guardado.Existe())
            {
                var guardado = Guardado.Cargar();
                string donde = Contenido.Capitulos.TryGetValue(guardado.capitulo, out var c) ? $"{c.Numero} · {c.Titulo}" : "";
                UI.BotonPrincipal(p, "Continuar", 190, y, 700, 140, () => { estado = Guardado.Cargar(); Reanudar(); });
                UI.Texto(p, donde, 32, Paleta.Tinta, 190, y + 150, 700, 50, TextAlignmentOptions.Center);
                y += 230;
                UI.BotonSecundario(p, "Nueva partida", 190, y, 700, 130, NuevaPartida);
            }
            else
            {
                UI.BotonPrincipal(p, "Nueva partida", 190, y, 700, 140, NuevaPartida);
            }
            y += 180;
            UI.BotonSecundario(p, "Cómo se juega", 190, y, 700, 120, ComoSeJuega);
            y += 160;
            UI.BotonSecundario(p, "Opciones", 190, y, 700, 120, Opciones);
            y += 160;
            UI.BotonSecundario(p, "Créditos", 190, y, 700, 120, Creditos);
#if UNITY_STANDALONE && !UNITY_EDITOR
            y += 160;
            UI.BotonSecundario(p, "Salir", 190, y, 700, 120, Application.Quit);
#endif
            UI.Texto(p, "UCES · Diseño y Desarrollo de Videojuegos I · 2026", 28, Paleta.Tinta, 0, 1820, 1080, 50, TextAlignmentOptions.Center);
        }

        private void NuevaPartida()
        {
            Guardado.Borrar();
            estado = new Estado();
            IniciarCapitulo("prologo");
        }

        private void Reanudar()
        {
            if (!Contenido.Capitulos.TryGetValue(estado.capitulo, out cap)) { NuevaPartida(); return; }
            enPartida = true;
            switch (estado.fase)
            {
                case "sopa": JugarSopa(Mathf.Clamp(estado.sopa, 0, cap.Sopas.Length - 1)); break;
                case "interrogatorio": Interrogar(); break;
                case "deduccion": Deducir(); break;
                case "pieza": MostrarPieza(); break;
                case "mapa": Mapa(); break;
                case "final": MostrarFinal(); break;
                default: Intro(); break;
            }
        }

        private void ComoSeJuega()
        {
            var c = AbrirCapa();
            UI.Panel(c, Paleta.Hoja, 60, 160, 960, 1600);
            UI.Texto(c, "Cómo se juega", 64, Paleta.Sombra, 110, 210, 860, 90, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UI.Texto(c,
                "<b>Marcar palabras.</b> Tocá la primera letra y arrastrá hasta la última, en línea recta.\n\n" +
                "<b>Palabras clave</b> (en <color=#A3451F>rojo óxido</color>): son pistas. Con ellas interrogás sospechosos y deducís adónde ir.\n\n" +
                "<b>Lupa:</b> marca la primera letra de una palabra. Son pocas: usalas bien.\n\n" +
                "<b>Reactivo:</b> revela letras escritas con tinta invisible.\n\n" +
                "<b>Palabras secretas:</b> hay 10 escondidas que no están en la lista. Cada una da una lupa extra... y quizás algo más.\n\n" +
                "<b>El reloj del perseguidor:</b> en el último capítulo, alguien te sigue. Resolvé antes de que te alcance.",
                38, Paleta.Sombra, 110, 320, 860, 1250);
            UI.BotonPrincipal(c, "Entendido", 290, 1600, 500, 120, CerrarCapa);
        }

        private void Opciones()
        {
            var c = AbrirCapa();
            UI.Panel(c, Paleta.Hoja, 90, 500, 900, 820);
            UI.Texto(c, "Opciones", 64, Paleta.Sombra, 140, 550, 800, 90, TextAlignmentOptions.TopLeft, FontStyles.Bold);

            void Fila(string nombre, float y, Func<float> leer, Action<float> escribir)
            {
                UI.Texto(c, nombre, 42, Paleta.Sombra, 140, y + 30, 360, 70, TextAlignmentOptions.Left);
                var valor = UI.Texto(c, "", 44, Paleta.Sombra, 650, y + 30, 160, 70, TextAlignmentOptions.Center, FontStyles.Bold);
                void Mostrar() => valor.text = Mathf.RoundToInt(leer() * 10).ToString();
                UI.BotonSecundario(c, "-", 520, y + 20, 110, 90, () => { escribir(leer() - 0.1f); Mostrar(); }, 56);
                UI.BotonSecundario(c, "+", 830, y + 20, 110, 90, () => { escribir(leer() + 0.1f); Mostrar(); }, 56);
                Mostrar();
            }
            Fila("Música", 680, () => Sonido.VolumenMusica, v => Sonido.VolumenMusica = v);
            Fila("Efectos", 820, () => Sonido.VolumenEfectos, v => { Sonido.VolumenEfectos = v; Sonido.Reproducir(Sfx.Acierto); });
            UI.BotonPrincipal(c, "Listo", 290, 1120, 500, 120, CerrarCapa);
        }

        private void Creditos()
        {
            var c = AbrirCapa();
            UI.Panel(c, Paleta.Hoja, 60, 300, 960, 1300);
            UI.Texto(c, "Créditos", 64, Paleta.Sombra, 110, 350, 860, 90, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UI.Texto(c,
                "<b>Sosa Jorge David</b>\nGame Design y Narrativa\n\n" +
                "<b>Barbano Sumay Gonzalo</b>\nProgramación\n\n" +
                "<b>Araujo Milagros</b>\nArte, UI y Sonido\n\n" +
                "Música de guitarra y efectos sintetizados por código.\n\n" +
                "Tecnicatura en Programación · UCES\nDiseño y Desarrollo de Videojuegos I\nProf. Angel Lugo · 2026",
                38, Paleta.Sombra, 110, 470, 860, 1000);
            UI.BotonPrincipal(c, "Volver", 290, 1440, 500, 120, CerrarCapa);
        }

        // ================================================================== capítulo: intro

        private void IniciarCapitulo(string id)
        {
            cap = Contenido.Capitulos[id];
            estado.capitulo = id;
            estado.sopa = 0;
            estado.fase = "intro";
            enPartida = true;
            Guardar();
            Intro();
        }

        private void Intro()
        {
            Narracion($"{cap.Numero} · {cap.Titulo}", cap.Intro, "Abrir el anotador", () => JugarSopa(0));
        }

        private void Narracion(string encabezado, string[] paginas, string textoFinal, Action alTerminar, int pagina = 0)
        {
            var p = NuevaPantalla(Paleta.Sombra);
            Sonido.Reproducir(Sfx.Pagina);
            UI.Texto(p, encabezado.ToUpper(), 32, Paleta.Ocre, 0, 260, 1080, 60, TextAlignmentOptions.Center, FontStyles.Bold).characterSpacing = 6;
            UI.Panel(p, Paleta.Ocre, 440, 340, 200, 4, false);
            UI.Texto(p, paginas[pagina], 50, Paleta.Papel, 110, 440, 860, 1000, TextAlignmentOptions.Top);
            UI.Texto(p, $"{pagina + 1} / {paginas.Length}", 32, Paleta.Borde, 0, 1500, 1080, 50, TextAlignmentOptions.Center);
            bool ultima = pagina == paginas.Length - 1;
            UI.BotonPrincipal(p, ultima ? textoFinal : "Seguir", 240, 1600, 600, 140, () =>
            {
                if (ultima) alTerminar();
                else Narracion(encabezado, paginas, textoFinal, alTerminar, pagina + 1);
            });
        }

        // ================================================================== página de sopa

        private void JugarSopa(int i)
        {
            indiceSopa = i;
            sopa = cap.Sopas[i];
            estado.fase = "sopa";
            estado.sopa = i;
            if (!string.IsNullOrEmpty(sopa.Tinta) && estado.reactivos < 1) estado.reactivos = 1;
            Guardar();

            var p = NuevaPantalla(Paleta.Papel);
            Sonido.Reproducir(Sfx.Pagina);
            Sonido.Poner(sopa.Segundos > 0 ? Musica.Tension : Musica.Calma);
            sopaTerminada = false;
            inactivo = 0;
            tiempoEnSopa = 0;
            erroresSeguidos = 0;
            ultimoSegundo = -1;

            // encabezado: ubicación
            UI.Texto(p, $"{cap.Numero} · PÁG. {i + 1}/{cap.Sopas.Length}".ToUpper(), 28, Paleta.Oxido, 40, 40, 560, 44, TextAlignmentOptions.TopLeft, FontStyles.Bold).characterSpacing = 4;
            UI.Texto(p, cap.Titulo, 56, Paleta.Sombra, 40, 80, 560, 80, TextAlignmentOptions.TopLeft, FontStyles.Bold);

            // herramientas, separadas de la grilla
            btnLupa = UI.BotonSecundario(p, "", 600, 40, 140, 130, UsarLupa);
            UI.Texto(btnLupa.transform, "Lupa", 30, Paleta.Sombra, 0, 70, 140, 50, TextAlignmentOptions.Center);
            txtLupas = UI.Texto(btnLupa.transform, "", 52, Paleta.Oxido, 0, 8, 140, 70, TextAlignmentOptions.Center, FontStyles.Bold);

            btnReactivo = UI.BotonSecundario(p, "", 760, 40, 140, 130, UsarReactivo);
            UI.Texto(btnReactivo.transform, "Reactivo", 26, Paleta.Sombra, 0, 74, 140, 50, TextAlignmentOptions.Center);
            txtReactivos = UI.Texto(btnReactivo.transform, "", 52, Paleta.Azul, 0, 8, 140, 70, TextAlignmentOptions.Center, FontStyles.Bold);
            btnReactivo.gameObject.SetActive(!string.IsNullOrEmpty(sopa.Tinta));

            UI.BotonSecundario(p, "II", 920, 40, 120, 130, Pausa, 48);

            // fila de información
            txtSecretas = UI.Texto(p, "", 30, Paleta.Tinta, 40, 190, 600, 50, TextAlignmentOptions.Left);
            txtReloj = UI.Texto(p, "", 40, Paleta.Sombra, 540, 185, 500, 60, TextAlignmentOptions.Right, FontStyles.Bold);

            // página del anotador
            var hoja = UI.Panel(p, Paleta.Hoja, 40, 250, 1000, 1030);
            var sombra = hoja.gameObject.AddComponent<Shadow>();
            sombra.effectColor = Paleta.ConAlfa(Paleta.Sombra, 0.15f);
            sombra.effectDistance = new Vector2(6, -8);
            UI.Texto(p, $"Anotador de Carlos · página {i + 1}", 28, Paleta.Oxido, 70, 268, 900, 44, TextAlignmentOptions.Left, FontStyles.Italic);
            vista = SopaView.Crear(p, sopa, 70, 320, 940, w => estado.secretas.Contains(w));
            vista.AlEncontrar += AlEncontrar;
            vista.AlFallar += AlFallar;
            vista.AlCompletar += AlCompletar;
            vista.AlInteractuar += () => inactivo = 0;

            // lista de palabras
            txtLista = UI.Texto(p, "", 46, Paleta.Sombra, 40, 1305, 1000, 200, TextAlignmentOptions.Top);
            txtLista.characterSpacing = 4;
            txtLista.lineSpacing = 20;

            // nota de Carlos
            UI.Panel(p, Paleta.Sombra, 40, 1530, 1000, 220);
            txtNota = UI.Texto(p, "", 38, Paleta.Papel, 80, 1555, 920, 180, TextAlignmentOptions.MidlineLeft);

            UI.BotonSecundario(p, "Libreta", 40, 1780, 1000, 110, Libreta, 40);

            tiempoRestante = sopa.Segundos;
            relojActivo = sopa.Segundos > 0;
            if (relojActivo) Sonido.Reproducir(Sfx.Alarma);

            Nota(sopa.Nota);
            Refrescar();
        }

        private void Nota(string texto)
        {
            if (txtNota != null) txtNota.text = $"<color={Paleta.Html(Paleta.Ocre)}><b>Carlos:</b></color> «{texto}»";
        }

        private void Refrescar()
        {
            if (vista == null) return;
            var partes = new List<string>();
            foreach (string w in sopa.Palabras)
            {
                if (vista.Encontradas.Contains(w)) partes.Add($"<s><color={Paleta.Html(Paleta.Tinta)}>{w}</color></s>");
                else if (sopa.Claves.Contains(w)) partes.Add($"<color={Paleta.Html(Paleta.Oxido)}><b>{w}</b></color>");
                else partes.Add(w);
            }
            txtLista.text = string.Join("    ", partes);
            txtSecretas.text = $"Secretas {estado.secretas.Count}/{Contenido.TotalSecretas}   ·   Piezas {estado.piezas.Count}/5";
            txtLupas.text = estado.lupas.ToString();
            txtReactivos.text = estado.reactivos.ToString();
            btnLupa.interactable = estado.lupas > 0;
            btnReactivo.interactable = estado.reactivos > 0 && vista.TieneTinta;
        }

        private void AlEncontrar(string palabra, TipoPalabra tipo)
        {
            inactivo = 0;
            erroresSeguidos = 0;
            pulsandoLupa = false;
            if (sopa.Tutorial) vista.OcultarMano();

            switch (tipo)
            {
                case TipoPalabra.Clave:
                    if (!estado.claves.Contains(palabra)) estado.claves.Add(palabra);
                    if (cap.Bifurca && string.IsNullOrEmpty(estado.rama) && (palabra == "SOCIO" || palabra == "LADRONA"))
                    {
                        estado.rama = palabra == "SOCIO" ? "A" : "B";
                        Nota(palabra == "SOCIO"
                            ? "¡SOCIO! Lo primero que reconozco es a Vidal. Voy a investigar a mi viejo socio."
                            : "¡LADRONA! El sombrero rojo no fue casualidad. Voy a seguir a Rosa Méndez.");
                    }
                    else Nota($"{palabra}: una palabra clave. Me sirve para interrogar.");
                    break;
                case TipoPalabra.Secreta:
                    if (!estado.secretas.Contains(palabra)) estado.secretas.Add(palabra);
                    estado.lupas++;
                    Nota($"Palabra secreta: {palabra} ({estado.secretas.Count}/{Contenido.TotalSecretas}). Gané una lupa.");
                    break;
                case TipoPalabra.Extra:
                    estado.extra = true;
                    Nota(palabra == "PERDON"
                        ? "PERDON. Eso fue lo que dijo Vidal. Ahora entiendo de qué lado está."
                        : "FALSO. El pasaje del sheriff es falso: nunca pensó subirse al vapor.");
                    break;
                default:
                    if (sopa.Tutorial && vista.Encontradas.Count == 1) Nota("¡Eso es! Ahora buscá las otras.");
                    break;
            }
            Refrescar();
        }

        private void AlFallar()
        {
            erroresSeguidos++;
            if (erroresSeguidos >= 3)
            {
                erroresSeguidos = 0;
                vista.ResaltarAyuda();
                Nota("Tranquilo. Mirá la fila que se ilumina: ahí empieza una palabra.");
            }
        }

        private void UsarLupa()
        {
            if (vista == null || estado.lupas <= 0) return;
            if (vista.UsarLupa() == null) { Nota("La lupa ya marcó todo lo que podía en esta página."); return; }
            estado.lupas--;
            estado.lupasUsadas++;
            pulsandoLupa = false;
            inactivo = 0;
            Sonido.Reproducir(Sfx.Lupa);
            Nota("La lupa marcó la primera letra de una palabra.");
            Refrescar();
        }

        private void UsarReactivo()
        {
            if (vista == null || estado.reactivos <= 0 || !vista.TieneTinta) return;
            estado.reactivos--;
            vista.UsarReactivo();
            Sonido.Reproducir(Sfx.Reactivo);
            Nota("El reactivo reveló letras escritas con tinta invisible, en azul.");
            Refrescar();
        }

        private void AlCompletar()
        {
            sopaTerminada = true;
            relojActivo = false;
            StartCoroutine(PaginaResuelta());
        }

        private IEnumerator PaginaResuelta()
        {
            yield return new WaitForSeconds(0.9f);
            bool racha = !sopa.Tutorial && sopa.Segundos == 0 && tiempoEnSopa < sopa.Tam * 9f;
            if (racha) estado.lupas++;

            var c = AbrirCapa();
            UI.Panel(c, Paleta.Hoja, 90, 560, 900, 760);
            UI.Texto(c, "Página resuelta", 64, Paleta.Sombra, 0, 620, 1080, 90, TextAlignmentOptions.Center, FontStyles.Bold);
            int s = Mathf.RoundToInt(tiempoEnSopa);
            string detalle = $"Tiempo: {s / 60}:{s % 60:00}";
            var claves = sopa.Claves.Where(k => vista.Encontradas.Contains(k)).ToList();
            if (claves.Count > 0) detalle += $"\n\nPalabras clave: <color={Paleta.Html(Paleta.Oxido)}><b>{string.Join(", ", claves)}</b></color>";
            if (racha) detalle += "\n\n¡Racha rápida! Ganaste una lupa.";
            UI.Texto(c, detalle, 40, Paleta.Sombra, 150, 740, 780, 400, TextAlignmentOptions.Top);
            UI.BotonPrincipal(c, "Seguir", 290, 1150, 500, 130, SiguientePaso);
            Sonido.Reproducir(Sfx.Pieza);
        }

        private void SiguientePaso()
        {
            if (indiceSopa + 1 < cap.Sopas.Length) { JugarSopa(indiceSopa + 1); return; }
            if (cap.Interrogatorio != null) Interrogar();
            else if (cap.Deduccion != null) Deducir();
            else MostrarPieza();
        }

        private void PerseguidorAlcanza()
        {
            relojActivo = false;
            sopaTerminada = true;
            vista.Bloqueada = true;
            Sonido.Reproducir(Sfx.Alarma);
            var c = AbrirCapa();
            UI.Panel(c, Paleta.Oxido, 90, 600, 900, 660);
            UI.Texto(c, "¡El perseguidor te alcanzó!", 58, Color.white, 130, 660, 820, 160, TextAlignmentOptions.Center, FontStyles.Bold);
            UI.Texto(c, "Carlos escapa por un pasadizo lateral, pero pierde la página. Tiene que empezar a descifrarla de nuevo.",
                40, Paleta.Hoja, 150, 840, 780, 250, TextAlignmentOptions.Top);
            UI.Boton(c, "Intentar de nuevo", 240, 1100, 600, 120, () => JugarSopa(indiceSopa), Paleta.Hoja, Paleta.Sombra);
        }

        // ================================================================== interrogatorio y deducción

        private void Interrogar()
        {
            estado.fase = "interrogatorio";
            Guardar();
            var it = cap.Interrogatorio;
            var p = NuevaPantalla(Paleta.Sombra);
            Sonido.Poner(Musica.Calma);
            Encabezado(p, "Interrogatorio", it.Sospechoso, Paleta.Ocre);

            // retrato (arte final opcional en Resources/Arte/retrato_<nombre>)
            var marcoRetrato = UI.Panel(p, Paleta.Tinta, 50, 230, 300, 360);
            var arte = UI.ArteOpcional("retrato_" + Retrato(it.Sospechoso));
            if (arte != null)
            {
                var img = UI.Panel(marcoRetrato.transform, Color.white, 10, 10, 280, 340, false);
                img.sprite = arte;
                img.preserveAspect = true;
            }
            else
            {
                UI.Texto(marcoRetrato.transform, it.Sospechoso.Substring(0, 1), 200, Paleta.Hoja, 0, 40, 300, 280, TextAlignmentOptions.Center, FontStyles.Bold);
            }
            UI.Texto(p, it.Descripcion, 36, Paleta.Borde, 390, 240, 640, 200, TextAlignmentOptions.TopLeft, FontStyles.Italic);

            var globo = UI.Panel(p, Paleta.Hoja, 50, 630, 980, 420);
            var dialogo = UI.Texto(globo.transform, $"«{it.Apertura}»", 42, Paleta.Sombra, 40, 30, 900, 360, TextAlignmentOptions.MidlineLeft);

            UI.Texto(p, "Preguntá usando tus palabras clave:", 36, Paleta.Ocre, 50, 1090, 980, 60, TextAlignmentOptions.Left, FontStyles.Bold);
            var preguntadas = new HashSet<string>();
            Button seguir = null;
            for (int k = 0; k < it.Preguntas.Length; k++)
            {
                var pr = it.Preguntas[k];
                float x = 50 + (k % 2) * 500, y = 1170 + (k / 2) * 160;
                Button b = null;
                b = UI.BotonSecundario(p, $"¿{pr.Palabra}?", x, y, 480, 130, () =>
                {
                    preguntadas.Add(pr.Palabra);
                    dialogo.text = $"«{pr.Respuesta}»";
                    b.GetComponent<Image>().color = Paleta.Borde;
                    Sonido.Reproducir(Sfx.Pagina);
                    if (preguntadas.Count == it.Preguntas.Length) seguir.interactable = true;
                }, 40);
            }
            seguir = UI.BotonPrincipal(p, "Seguir investigando", 190, 1760, 700, 120, () =>
            {
                if (cap.Deduccion != null) Deducir(); else MostrarPieza();
            });
            seguir.interactable = false;
        }

        private void Deducir()
        {
            estado.fase = "deduccion";
            Guardar();
            var d = cap.Deduccion;
            var p = NuevaPantalla(Paleta.Papel);
            Encabezado(p, $"{cap.Numero} · Deducción", "Combinar pistas", Paleta.Sombra);
            UI.Texto(p, d.Pregunta, 48, Paleta.Sombra, 50, 260, 980, 220, TextAlignmentOptions.TopLeft, FontStyles.Bold);

            var devolucion = UI.Texto(p, "", 40, Paleta.Sombra, 50, 1240, 980, 300, TextAlignmentOptions.Top);
            Button seguir = UI.BotonPrincipal(p, "Anotar en la libreta", 190, 1720, 700, 130, MostrarPieza);
            seguir.gameObject.SetActive(false);

            for (int k = 0; k < d.Opciones.Length; k++)
            {
                var op = d.Opciones[k];
                Button b = null;
                b = UI.BotonSecundario(p, op.Texto, 50, 520 + k * 230, 980, 200, () =>
                {
                    if (op.Correcta)
                    {
                        Sonido.Reproducir(Sfx.Clave);
                        b.GetComponent<Image>().color = Paleta.Ocre;
                        devolucion.text = $"<color={Paleta.Html(Paleta.Tinta)}><b>¡Eso es!</b></color> {op.Devolucion}";
                        seguir.gameObject.SetActive(true);
                    }
                    else
                    {
                        Sonido.Reproducir(Sfx.Error);
                        b.interactable = false;
                        devolucion.text = $"<color={Paleta.Html(Paleta.Oxido)}><b>No cierra.</b></color> {op.Devolucion}";
                    }
                }, 40);
            }
        }

        // ================================================================== pieza del mapa, mapa y finales

        private void MostrarPieza()
        {
            estado.fase = "pieza";
            if (!estado.completados.Contains(cap.Id)) estado.completados.Add(cap.Id);
            if (!estado.piezas.Contains(cap.Id)) estado.piezas.Add(cap.Id);
            if (!estado.pistas.Contains(cap.Pista)) estado.pistas.Add(cap.Pista);
            Guardar();

            var p = NuevaPantalla(Paleta.Sombra);
            Sonido.Reproducir(Sfx.Pieza);
            UI.Texto(p, "PIEZA DEL MAPA CONSEGUIDA", 34, Paleta.Ocre, 0, 300, 1080, 60, TextAlignmentOptions.Center, FontStyles.Bold).characterSpacing = 6;
            UI.Texto(p, cap.Pieza, 72, Paleta.Papel, 60, 380, 960, 200, TextAlignmentOptions.Center, FontStyles.Bold);

            // cinco recortes del mapa: se completan uno por capítulo
            for (int k = 0; k < 5; k++)
            {
                bool tiene = k < estado.piezas.Count;
                var pieza = UI.Panel(p, tiene ? Paleta.Hoja : Paleta.ConAlfa(Paleta.Papel, 0.12f), 115 + k * 175, 650, 150, 190);
                pieza.transform.localRotation = Quaternion.Euler(0, 0, (k % 2 == 0 ? -1 : 1) * 4f);
                if (tiene) UI.Texto(pieza.transform, (k + 1).ToString(), 64, Paleta.Oxido, 0, 50, 150, 90, TextAlignmentOptions.Center, FontStyles.Bold);
            }
            UI.Texto(p, $"Piezas {estado.piezas.Count}/5", 36, Paleta.Borde, 0, 880, 1080, 60, TextAlignmentOptions.Center);

            UI.Panel(p, Paleta.Hoja, 90, 1010, 900, 330);
            UI.Texto(p, "Anotado en la libreta:", 32, Paleta.Oxido, 140, 1040, 800, 50, TextAlignmentOptions.Left, FontStyles.Bold);
            UI.Texto(p, cap.Pista, 40, Paleta.Sombra, 140, 1100, 800, 220, TextAlignmentOptions.TopLeft);

            UI.BotonPrincipal(p, cap.Ultimo ? "Abrir la valija" : "Ver el mapa", 190, 1600, 700, 140, () =>
            {
                if (cap.Ultimo) MostrarFinal(); else Mapa();
            });
        }

        private void Mapa()
        {
            estado.fase = "mapa";
            Guardar();
            var destino = Contenido.Siguiente(cap, estado);
            var p = NuevaPantalla(Paleta.Papel);
            Sonido.Poner(Musica.Calma);
            Encabezado(p, "Mapa", "Sierra Polvorienta", Paleta.Sombra);

            const float ox = 60, oy = 250;
            var area = UI.Panel(p, Paleta.Hoja, ox, oy, 960, 1160);
            var arteMapa = UI.ArteOpcional("mapa");
            if (arteMapa != null)
            {
                var img = UI.Panel(area.transform, Color.white, 0, 0, 960, 1160);
                img.sprite = arteMapa;
                img.type = Image.Type.Simple;
            }

            var lugares = Contenido.Lugares.ToDictionary(l => l.CapituloId);
            string[][] caminos = { new[] { "prologo", "estacion" }, new[] { "estacion", "cantina" }, new[] { "cantina", "rancho" },
                                   new[] { "rancho", "mina" }, new[] { "cantina", "mercado" }, new[] { "mercado", "puerto" } };
            foreach (var cm in caminos)
            {
                var a = lugares[cm[0]];
                var b = lugares[cm[1]];
                bool recorrido = estado.completados.Contains(a.CapituloId) && (estado.completados.Contains(b.CapituloId) || destino?.Id == b.CapituloId);
                Linea(area.transform, new Vector2(a.X + 60, a.Y + 60), new Vector2(b.X + 60, b.Y + 60),
                    recorrido ? Paleta.Tinta : Paleta.ConAlfa(Paleta.Tinta, 0.18f), recorrido ? 8 : 5);
            }

            foreach (var l in Contenido.Lugares)
            {
                bool hecho = estado.completados.Contains(l.CapituloId);
                bool esDestino = destino != null && destino.Id == l.CapituloId;
                bool oculto = !string.IsNullOrEmpty(l.Rama) && !string.IsNullOrEmpty(estado.rama) && l.Rama != estado.rama;
                bool desconocido = !string.IsNullOrEmpty(l.Rama) && string.IsNullOrEmpty(estado.rama);

                Color fondo = hecho ? Paleta.Tinta : esDestino ? Paleta.Ocre : Paleta.ConAlfa(Paleta.Borde, 0.5f);
                string nombre = oculto || desconocido ? "???" : l.Nombre;
                var nodo = UI.Boton(area.transform, hecho ? "" : esDestino ? "!" : "", l.X, l.Y, 120, 120, null, fondo, Paleta.Sombra, 60);
                nodo.interactable = esDestino;
                if (esDestino)
                {
                    string id = l.CapituloId;
                    nodo.onClick.AddListener(() => IniciarCapitulo(id));
                    StartCoroutine(Pulsar(nodo.transform, () => true));
                }
                var etiqueta = UI.Texto(area.transform, nombre, 32, oculto ? Paleta.ConAlfa(Paleta.Tinta, 0.4f) : Paleta.Sombra,
                    l.X - 90, l.Y + 128, 300, 50, TextAlignmentOptions.Top, esDestino ? FontStyles.Bold : FontStyles.Normal);
                if (l.X > 700) etiqueta.rectTransform.anchoredPosition = new Vector2(l.X - 180, -(l.Y + 128));
            }

            string texto;
            if (destino != null)
            {
                texto = $"Próximo destino: <b>{destino.Titulo}</b>. Tocá el lugar resaltado para viajar.";
                if (cap.Bifurca)
                    texto = (estado.rama == "A" ? "Tu primera palabra clave fue <b>SOCIO</b>: vas tras Vidal. " : "Tu primera palabra clave fue <b>LADRONA</b>: vas tras Rosa. ") + texto;
            }
            else texto = "La investigación terminó.";
            UI.Texto(p, texto, 38, Paleta.Sombra, 60, 1440, 960, 160, TextAlignmentOptions.Top);
            UI.Texto(p, $"Piezas del mapa {estado.piezas.Count}/5   ·   Secretas {estado.secretas.Count}/{Contenido.TotalSecretas}   ·   Lupas {estado.lupas}",
                30, Paleta.Tinta, 60, 1620, 960, 50, TextAlignmentOptions.Center);
            UI.BotonSecundario(p, "Libreta", 60, 1720, 460, 120, Libreta, 40);
            UI.BotonSecundario(p, "Menú", 560, 1720, 460, 120, Menu, 40);
        }

        private static void Linea(Transform padre, Vector2 a, Vector2 b, Color color, float grosor)
        {
            var rt = UI.Nodo("Camino", padre);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var medio = (a + b) / 2f;
            rt.anchoredPosition = new Vector2(medio.x, -medio.y);
            rt.sizeDelta = new Vector2(Vector2.Distance(a, b), grosor);
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0, 0, -ang);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private void MostrarFinal()
        {
            estado.fase = "final";
            Guardar();
            string id = estado.extra ? "tesoro" : estado.rama == "B" ? "trampa" : "traicion";
            var f = Contenido.Finales[id];
            Sonido.Poner(Musica.Final);
            Narracion(f.Titulo, f.Texto, "Seguir", () =>
            {
                if (estado.secretas.Count >= Contenido.TotalSecretas)
                {
                    var oculto = Contenido.Finales["oculto"];
                    Narracion(oculto.Titulo, oculto.Texto, "Seguir", () => Resumen(f, true));
                }
                else Resumen(f, false);
            });
        }

        private void Resumen(Final f, bool oculto)
        {
            enPartida = false;
            var p = NuevaPantalla(Paleta.Papel);
            Encabezado(p, "Fin de la investigación", f.Titulo, Paleta.Sombra);
            int s = Mathf.RoundToInt(estado.segundosJugados);
            string texto =
                $"Camino: <b>{(estado.rama == "B" ? "la ladrona (mercado y puerto)" : "el socio (rancho y mina)")}</b>\n\n" +
                $"Palabras secretas: <b>{estado.secretas.Count}/{Contenido.TotalSecretas}</b>{(oculto ? "  (¡final oculto!)" : "")}\n\n" +
                $"Piezas del mapa: <b>{estado.piezas.Count}/5</b>\n\n" +
                $"Lupas usadas: <b>{estado.lupasUsadas}</b>\n\n" +
                $"Tiempo de juego: <b>{s / 3600}h {s / 60 % 60:00}m</b>\n\n\n" +
                "<i>Hay tres finales y uno oculto. Probá el otro camino o buscá las palabras que te faltaron.</i>";
            UI.Texto(p, texto, 42, Paleta.Sombra, 60, 300, 960, 1200, TextAlignmentOptions.TopLeft);
            Guardado.Borrar();
            UI.BotonPrincipal(p, "Volver al menú", 190, 1700, 700, 140, Menu);
        }

        // ================================================================== libreta y pausa

        private void Libreta()
        {
            if (estado == null) return;
            var c = AbrirCapa();
            UI.Panel(c, Paleta.Hoja, 50, 80, 980, 1760);
            UI.Texto(c, "Libreta de Carlos", 60, Paleta.Sombra, 100, 120, 880, 90, TextAlignmentOptions.TopLeft, FontStyles.Bold);

            string pistas = estado.pistas.Count == 0 ? "<i>Todavía no anotaste pistas.</i>"
                : string.Join("\n", estado.pistas.Select(x => "- " + x));
            UI.Texto(c, $"<color={Paleta.Html(Paleta.Oxido)}><b>PISTAS</b></color>\n{pistas}", 32, Paleta.Sombra, 100, 230, 880, 640);

            var conocidos = new List<string> { "<b>Carlos Ríos</b>: vos. Metódico y desconfiado." };
            if (estado.completados.Contains("prologo"))
                conocidos.Add("<b>Sheriff Carrizo</b>: autoridad local. Ayuda, pero con intereses propios.");
            if (estado.completados.Contains("estacion"))
                conocidos.Add("<b>Rosa Méndez</b>: carterista del sombrero rojo. Sabe más de lo que dice.");
            if (estado.completados.Contains("cantina") || estado.claves.Contains("SOCIO"))
                conocidos.Add("<b>Tomás «El Zorro» Vidal</b>: ex socio. ¿Traidor o aliado?");
            UI.Texto(c, $"<color={Paleta.Html(Paleta.Oxido)}><b>SOSPECHOSOS</b></color>\n{string.Join("\n", conocidos)}", 32, Paleta.Sombra, 100, 900, 880, 520);

            UI.Texto(c, $"Piezas del mapa {estado.piezas.Count}/5  ·  Secretas {estado.secretas.Count}/{Contenido.TotalSecretas}  ·  Lupas {estado.lupas}",
                30, Paleta.Tinta, 100, 1450, 880, 50, TextAlignmentOptions.Center);
            UI.BotonPrincipal(c, "Cerrar", 290, 1640, 500, 120, CerrarCapa);
        }

        private void Pausa()
        {
            if (!enPartida) return;
            var c = AbrirCapa();
            UI.Texto(c, "Pausa", 80, Paleta.Ocre, 0, 480, 1080, 120, TextAlignmentOptions.Center, FontStyles.Bold);
            UI.BotonPrincipal(c, "Continuar", 240, 700, 600, 130, CerrarCapa);
            UI.BotonSecundario(c, "Libreta", 240, 870, 600, 120, Libreta);
            UI.BotonSecundario(c, "Opciones", 240, 1030, 600, 120, Opciones);
            UI.BotonSecundario(c, "Volver al menú", 240, 1190, 600, 120, Menu);
            UI.Texto(c, "El progreso se guarda al empezar cada página.", 32, Paleta.Borde, 0, 1360, 1080, 60, TextAlignmentOptions.Center);
        }
    }
}
