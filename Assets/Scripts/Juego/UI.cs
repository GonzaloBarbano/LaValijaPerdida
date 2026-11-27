using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ValijaPerdida
{
    /// <summary>
    /// Paleta del GDD (apartado 6) y funciones para construir la interfaz por código.
    /// Todas las pantallas se arman sobre un marco fijo de 1080x1920 (vertical 9:16).
    /// Las coordenadas (x, y) se miden desde la esquina superior izquierda del marco.
    /// </summary>
    public static class Paleta
    {
        public static readonly Color Sombra = Hex("2B1D16");   // café noir
        public static readonly Color Papel = Hex("F2E6D0");    // papel envejecido
        public static readonly Color Hoja = Hex("FAF3E3");     // hoja clara
        public static readonly Color Tinta = Hex("6B5240");    // tinta sepia
        public static readonly Color Ocre = Hex("E0A843");     // ocre del desierto
        public static readonly Color Oxido = Hex("A3451F");    // rojo óxido
        public static readonly Color Borde = Hex("CDB89A");
        public static readonly Color Madera = Hex("1E140F");   // mesa de trabajo (fondo en PC)
        public static readonly Color Azul = Hex("2F5D62");     // tinta revelada por el reactivo

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        public static Color ConAlfa(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static string Html(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    public static class UI
    {
        public const float Ancho = 1080f;
        public const float Alto = 1920f;

        private static Sprite redondeado;

        /// <summary>Sprite con esquinas redondeadas generado por código (9-slice).</summary>
        public static Sprite Redondeado
        {
            get
            {
                if (redondeado == null) redondeado = CrearRedondeado(64, 18);
                return redondeado;
            }
        }

        private static Sprite CrearRedondeado(int tam, int radio)
        {
            var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[tam * tam];
            for (int y = 0; y < tam; y++)
            {
                for (int x = 0; x < tam; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radio, tam - radio);
                    float cy = Mathf.Clamp(y + 0.5f, radio, tam - radio);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radio - d + 0.5f);
                    px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radio, radio, radio, radio));
        }

        public static RectTransform Nodo(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Ubica un elemento con coordenadas desde la esquina superior izquierda del padre.</summary>
        public static RectTransform Ubicar(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Llenar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(Transform padre, Color color, float x, float y, float w, float h, bool redondo = true)
        {
            var rt = Ubicar(Nodo("Panel", padre), x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (redondo)
            {
                img.sprite = Redondeado;
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Image Fondo(Transform padre, Color color)
        {
            var rt = Llenar(Nodo("Fondo", padre));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Texto(Transform padre, string texto, float tam, Color color,
            float x, float y, float w, float h,
            TextAlignmentOptions alineacion = TextAlignmentOptions.TopLeft, FontStyles estilo = FontStyles.Normal)
        {
            var rt = Ubicar(Nodo("Texto", padre), x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = texto;
            t.fontSize = tam;
            t.color = color;
            t.alignment = alineacion;
            t.fontStyle = estilo;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }

        public static Button Boton(Transform padre, string etiqueta, float x, float y, float w, float h,
            Action alTocar, Color fondo, Color texto, float tamTexto = 44)
        {
            var img = Panel(padre, fondo, x, y, w, h);
            img.raycastTarget = true;
            img.gameObject.name = "Boton " + etiqueta;
            var b = img.gameObject.AddComponent<Button>();
            var colores = b.colors;
            colores.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colores.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            b.colors = colores;
            b.targetGraphic = img;
            var t = Texto(img.transform, etiqueta, tamTexto, texto, 0, 0, w, h, TextAlignmentOptions.Center, FontStyles.Bold);
            t.margin = new Vector4(16, 0, 16, 0);
            b.onClick.AddListener(() =>
            {
                Sonido.Reproducir(Sfx.Tic);
                alTocar?.Invoke();
            });
            return b;
        }

        /// <summary>Botón principal (ocre sobre papel o sobre noir).</summary>
        public static Button BotonPrincipal(Transform padre, string etiqueta, float x, float y, float w, float h, Action alTocar)
        {
            return Boton(padre, etiqueta, x, y, w, h, alTocar, Paleta.Ocre, Paleta.Sombra);
        }

        /// <summary>Botón secundario, con borde en tinta.</summary>
        public static Button BotonSecundario(Transform padre, string etiqueta, float x, float y, float w, float h, Action alTocar, float tamTexto = 40)
        {
            var b = Boton(padre, etiqueta, x, y, w, h, alTocar, Paleta.Hoja, Paleta.Sombra, tamTexto);
            var borde = b.gameObject.AddComponent<Outline>();
            borde.effectColor = Paleta.Tinta;
            borde.effectDistance = new Vector2(3, -3);
            return b;
        }

        /// <summary>Carga un sprite opcional desde Assets/Resources (arte final de Milagros). Devuelve null si no existe.</summary>
        public static Sprite ArteOpcional(string ruta)
        {
            return Resources.Load<Sprite>("Arte/" + ruta);
        }
    }
}
