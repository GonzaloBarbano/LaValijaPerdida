using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ValijaPerdida
{
    public enum TipoPalabra { Lista, Clave, Secreta, Extra }

    /// <summary>
    /// Grilla interactiva de una sopa de letras: selección por arrastre "imantada" a la
    /// dirección recta más cercana, validación, estados de color, lupa, reactivo y ayudas.
    /// </summary>
    public class SopaView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private enum EstadoCelda { Normal, Encontrada, Clave, Secreta }

        public event Action<string, TipoPalabra> AlEncontrar;
        public event Action AlFallar;
        public event Action AlCompletar;
        public event Action AlInteractuar;

        public Sopa Sopa { get; private set; }
        public HashSet<string> Encontradas { get; } = new HashSet<string>();
        public bool Bloqueada { get; set; }

        private Grilla grilla;
        private int n;
        private float celda;
        private RectTransform rt;
        private Image[,] fondos;
        private TextMeshProUGUI[,] letras;
        private EstadoCelda[,] estados;
        private bool[,] invisibles;
        private bool tintaRevelada;
        private readonly HashSet<Vector2Int> marcasLupa = new HashSet<Vector2Int>();
        private Func<string, bool> secretaYaEncontrada;

        private Vector2Int inicio;
        private readonly List<Vector2Int> linea = new List<Vector2Int>();
        private bool arrastrando;
        private RectTransform mano;
        private Coroutine rutinaMano;

        private static readonly Vector2Int[] Ocho =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
            new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)
        };

        public static SopaView Crear(Transform padre, Sopa sopa, float x, float y, float lado, Func<string, bool> secretaYaEncontrada)
        {
            var rt = UI.Ubicar(UI.Nodo("Grilla", padre), x, y, lado, lado);
            var fondo = rt.gameObject.AddComponent<Image>();
            fondo.color = new Color(1, 1, 1, 0);   // invisible pero recibe los toques
            var v = rt.gameObject.AddComponent<SopaView>();
            v.rt = rt;
            v.secretaYaEncontrada = secretaYaEncontrada;
            v.Construir(sopa, lado);
            return v;
        }

        private void Construir(Sopa sopa, float lado)
        {
            Sopa = sopa;
            grilla = Generador.Generar(sopa);
            n = sopa.Tam;
            celda = lado / n;
            fondos = new Image[n, n];
            letras = new TextMeshProUGUI[n, n];
            estados = new EstadoCelda[n, n];
            invisibles = new bool[n, n];

            if (!string.IsNullOrEmpty(sopa.Tinta) && grilla.Posiciones.TryGetValue(sopa.Tinta, out var tinta))
            {
                // solo se ocultan las letras que no comparten celda con otra palabra
                var otras = new HashSet<Vector2Int>(grilla.Posiciones.Where(kv => kv.Key != sopa.Tinta).SelectMany(kv => kv.Value));
                foreach (var p in tinta) if (!otras.Contains(p)) invisibles[p.x, p.y] = true;
            }

            float margen = Mathf.Max(2f, celda * 0.06f);
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    var img = UI.Panel(rt, Color.clear, c * celda + margen, r * celda + margen, celda - 2 * margen, celda - 2 * margen);
                    var t = UI.Texto(img.transform, "", celda * 0.56f, Paleta.Sombra, 0, 0, celda - 2 * margen, celda - 2 * margen,
                        TextAlignmentOptions.Center, FontStyles.Bold);
                    fondos[r, c] = img;
                    letras[r, c] = t;
                }
            }
            Pintar();
        }

        // ------------------------------------------------------------------ entrada

        public void OnPointerDown(PointerEventData e)
        {
            if (Bloqueada) return;
            if (!Celda(e, out var rc, out _)) return;
            AlInteractuar?.Invoke();
            arrastrando = true;
            inicio = rc;
            ActualizarLinea(new List<Vector2Int> { rc });
        }

        public void OnDrag(PointerEventData e)
        {
            if (!arrastrando || Bloqueada) return;
            Celda(e, out _, out var continuo);
            var d = continuo - new Vector2(inicio.x, inicio.y);
            var nueva = new List<Vector2Int> { inicio };
            if (d.magnitude >= 0.5f)
            {
                // imantado: se toma la dirección recta más cercana de las ocho
                float angulo = Mathf.Atan2(d.x, d.y);
                int idx = ((Mathf.RoundToInt(angulo / (Mathf.PI / 4f)) % 8) + 8) % 8;
                var dir = Ocho[idx];
                float proy = (d.x * dir.x + d.y * dir.y) / (dir.x * dir.x + dir.y * dir.y);
                int largo = Mathf.Max(0, Mathf.RoundToInt(proy));
                for (int i = 1; i <= largo; i++)
                {
                    var p = inicio + dir * i;
                    if (p.x < 0 || p.x >= n || p.y < 0 || p.y >= n) break;
                    nueva.Add(p);
                }
            }
            ActualizarLinea(nueva);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!arrastrando) return;
            arrastrando = false;
            Validar();
        }

        private bool Celda(PointerEventData e, out Vector2Int rc, out Vector2 continuo)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 lp);
            float fila = -lp.y / celda - 0.5f;
            float col = lp.x / celda - 0.5f;
            continuo = new Vector2(fila, col);
            rc = new Vector2Int(Mathf.RoundToInt(fila), Mathf.RoundToInt(col));
            bool dentro = rc.x >= 0 && rc.x < n && rc.y >= 0 && rc.y < n;
            rc.x = Mathf.Clamp(rc.x, 0, n - 1);
            rc.y = Mathf.Clamp(rc.y, 0, n - 1);
            return dentro;
        }

        private void ActualizarLinea(List<Vector2Int> nueva)
        {
            if (nueva.Count != linea.Count) Sonido.ReproducirLetra(nueva.Count - 1);
            linea.Clear();
            linea.AddRange(nueva);
            Pintar();
        }

        // ------------------------------------------------------------------ validación

        private void Validar()
        {
            string sel = new string(linea.Select(p => grilla.Letras[p.x, p.y]).ToArray());
            string rev = new string(sel.Reverse().ToArray());
            var celdas = new List<Vector2Int>(linea);
            linea.Clear();

            if (sel.Length < 2) { Pintar(); return; }

            string palabra = null;
            TipoPalabra tipo = TipoPalabra.Lista;

            foreach (string p in Sopa.Palabras)
            {
                if (Encontradas.Contains(p)) continue;
                if (p == sel || p == rev)
                {
                    palabra = p;
                    tipo = Sopa.Claves.Contains(p) ? TipoPalabra.Clave : TipoPalabra.Lista;
                    break;
                }
            }
            if (palabra == null)
            {
                foreach (string p in Sopa.Secretas)
                {
                    if (Encontradas.Contains(p) || secretaYaEncontrada(p)) continue;
                    if (p == sel || p == rev) { palabra = p; tipo = TipoPalabra.Secreta; break; }
                }
            }
            if (palabra == null && !string.IsNullOrEmpty(Sopa.Extra) && !Encontradas.Contains(Sopa.Extra)
                && (Sopa.Extra == sel || Sopa.Extra == rev))
            {
                palabra = Sopa.Extra;
                tipo = TipoPalabra.Extra;
            }

            if (palabra == null)
            {
                Sonido.Reproducir(Sfx.Error);
                AlFallar?.Invoke();
                StartCoroutine(Temblar());
                Pintar();
                return;
            }

            Encontradas.Add(palabra);
            var estado = tipo == TipoPalabra.Clave || tipo == TipoPalabra.Extra ? EstadoCelda.Clave
                : tipo == TipoPalabra.Secreta ? EstadoCelda.Secreta : EstadoCelda.Encontrada;
            foreach (var p in celdas)
            {
                if (estados[p.x, p.y] == EstadoCelda.Normal || estado == EstadoCelda.Clave) estados[p.x, p.y] = estado;
                marcasLupa.Remove(p);
                if (invisibles[p.x, p.y]) invisibles[p.x, p.y] = false;
            }
            Sonido.Reproducir(tipo == TipoPalabra.Lista ? Sfx.Acierto : tipo == TipoPalabra.Secreta ? Sfx.Secreta : Sfx.Clave);
            Pintar();
            AlEncontrar?.Invoke(palabra, tipo);

            if (Completa())
            {
                Bloqueada = true;
                AlCompletar?.Invoke();
            }
        }

        public bool Completa() => Sopa.Palabras.All(p => Encontradas.Contains(p));

        // ------------------------------------------------------------------ herramientas y ayudas

        /// <summary>Lupa: marca la primera letra de una palabra que falta.</summary>
        public string UsarLupa()
        {
            var pendientes = Sopa.Palabras.Where(p => !Encontradas.Contains(p) && grilla.Posiciones.ContainsKey(p)
                && !marcasLupa.Contains(grilla.Posiciones[p][0])).ToList();
            if (pendientes.Count == 0) return null;
            string p0 = pendientes[0];
            marcasLupa.Add(grilla.Posiciones[p0][0]);
            Pintar();
            return p0;
        }

        public bool TieneTinta => !string.IsNullOrEmpty(Sopa.Tinta) && !tintaRevelada;

        /// <summary>Reactivo: revela las letras escritas en tinta invisible.</summary>
        public void UsarReactivo()
        {
            tintaRevelada = true;
            Pintar();
        }

        /// <summary>Ayuda automática (3 errores seguidos): ilumina la fila o columna donde empieza una palabra.</summary>
        public void ResaltarAyuda()
        {
            var p = Sopa.Palabras.FirstOrDefault(w => !Encontradas.Contains(w) && grilla.Posiciones.ContainsKey(w));
            if (p == null) return;
            StartCoroutine(Destello(grilla.Posiciones[p][0].x));
        }

        private IEnumerator Destello(int fila)
        {
            for (int k = 0; k < 3; k++)
            {
                for (int c = 0; c < n; c++)
                    if (estados[fila, c] == EstadoCelda.Normal) fondos[fila, c].color = Paleta.ConAlfa(Paleta.Ocre, 0.35f);
                yield return new WaitForSeconds(0.35f);
                Pintar();
                yield return new WaitForSeconds(0.25f);
            }
        }

        /// <summary>Tutorial del prólogo: una mano muestra cómo arrastrar sobre una palabra.</summary>
        public void MostrarMano(string palabra)
        {
            if (!grilla.Posiciones.ContainsKey(palabra) || mano != null) return;
            var img = UI.Panel(rt, Paleta.ConAlfa(Paleta.Oxido, 0.75f), 0, 0, celda * 0.5f, celda * 0.5f);
            img.sprite = UI.Redondeado;
            mano = img.rectTransform;
            rutinaMano = StartCoroutine(MoverMano(grilla.Posiciones[palabra]));
        }

        public void OcultarMano()
        {
            if (rutinaMano != null) StopCoroutine(rutinaMano);
            if (mano != null) Destroy(mano.gameObject);
            mano = null;
        }

        private IEnumerator MoverMano(List<Vector2Int> pos)
        {
            Vector2 Centro(Vector2Int p) => new Vector2(p.y * celda + celda * 0.25f, -(p.x * celda + celda * 0.25f));
            while (mano != null)
            {
                Vector2 a = Centro(pos[0]), b = Centro(pos[pos.Count - 1]);
                mano.anchoredPosition = a;
                yield return new WaitForSeconds(0.4f);
                for (float t = 0; t < 1f && mano != null; t += Time.deltaTime / 1.4f)
                {
                    mano.anchoredPosition = Vector2.Lerp(a, b, Mathf.SmoothStep(0, 1, t));
                    yield return null;
                }
                yield return new WaitForSeconds(0.6f);
            }
        }

        private IEnumerator Temblar()
        {
            Vector2 origen = rt.anchoredPosition;
            for (int i = 0; i < 6; i++)
            {
                rt.anchoredPosition = origen + new Vector2((i % 2 == 0 ? 1 : -1) * 6f, 0);
                yield return new WaitForSeconds(0.03f);
            }
            rt.anchoredPosition = origen;
        }

        // ------------------------------------------------------------------ dibujo

        private void Pintar()
        {
            var sel = new HashSet<Vector2Int>(linea);
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    var p = new Vector2Int(r, c);
                    var img = fondos[r, c];
                    var t = letras[r, c];
                    var borde = img.GetComponent<Outline>();

                    Color fondo = Color.clear, texto = Paleta.Sombra;
                    switch (estados[r, c])
                    {
                        case EstadoCelda.Encontrada: fondo = Paleta.Ocre; break;
                        case EstadoCelda.Clave: fondo = Paleta.Oxido; texto = Color.white; break;
                        case EstadoCelda.Secreta: fondo = Paleta.Tinta; texto = Paleta.Hoja; break;
                    }
                    if (sel.Contains(p)) { fondo = Paleta.ConAlfa(Paleta.Ocre, 0.45f); texto = Paleta.Sombra; }

                    bool lupa = marcasLupa.Contains(p);
                    if (lupa && borde == null)
                    {
                        borde = img.gameObject.AddComponent<Outline>();
                        borde.effectColor = Paleta.Oxido;
                        borde.effectDistance = new Vector2(4, -4);
                    }
                    if (borde != null) borde.enabled = lupa;
                    if (lupa && estados[r, c] == EstadoCelda.Normal)
                    {
                        texto = Paleta.Oxido;
                        if (fondo == Color.clear) fondo = Paleta.ConAlfa(Paleta.Oxido, 0.12f);
                    }

                    img.color = fondo;
                    char letra = grilla.Letras[r, c];
                    if (invisibles[r, c] && !tintaRevelada)
                    {
                        t.text = "·";
                        t.color = Paleta.ConAlfa(Paleta.Tinta, 0.5f);
                    }
                    else
                    {
                        t.text = letra.ToString();
                        t.color = invisibles[r, c] && estados[r, c] == EstadoCelda.Normal && !sel.Contains(p) ? Paleta.Azul : texto;
                    }
                }
            }
        }
    }
}
