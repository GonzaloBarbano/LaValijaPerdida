using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValijaPerdida
{
    /// <summary>
    /// Arma la grilla de una sopa a partir de sus palabras y de las direcciones habilitadas.
    /// El diseñador solo escribe las palabras en Contenido.cs: la ubicación se genera
    /// con una semilla fija, así la misma página sale siempre igual.
    /// </summary>
    public class Grilla
    {
        public char[,] Letras;
        public readonly Dictionary<string, List<Vector2Int>> Posiciones = new Dictionary<string, List<Vector2Int>>();
    }

    public static class Generador
    {
        // Letras de relleno con frecuencia parecida al español
        private const string Relleno = "AAAAAEEEEEIIIOOOOUUSSSRRRNNNLLLDDTTTCCCMMPPBGVFHJQZYX";

        public static Grilla Generar(Sopa sopa)
        {
            var palabras = new List<string>();
            palabras.AddRange(sopa.Palabras);
            palabras.AddRange(sopa.Secretas);
            if (!string.IsNullOrEmpty(sopa.Extra)) palabras.Add(sopa.Extra);
            palabras = palabras.Distinct().OrderByDescending(p => p.Length).ToList();

            var dirs = DireccionesDe(sopa.Dirs);
            for (int intento = 0; intento < 400; intento++)
            {
                var azar = new System.Random(sopa.Semilla * 7919 + intento);
                var g = new Grilla { Letras = new char[sopa.Tam, sopa.Tam] };
                bool ok = true;
                foreach (string p in palabras)
                {
                    if (!Ubicar(g, p, dirs, azar)) { ok = false; break; }
                }
                if (!ok) continue;

                for (int r = 0; r < sopa.Tam; r++)
                    for (int c = 0; c < sopa.Tam; c++)
                        if (g.Letras[r, c] == '\0')
                            g.Letras[r, c] = Relleno[azar.Next(Relleno.Length)];
                return g;
            }
            Debug.LogError("No se pudo generar la sopa: revisar que las palabras entren en la grilla de " + sopa.Tam);
            return null;
        }

        private static List<Vector2Int> DireccionesDe(Direcciones d)
        {
            // Vector2Int(x = fila, y = columna)
            var lista = new List<Vector2Int>();
            if ((d & Direcciones.Horizontal) != 0) lista.Add(new Vector2Int(0, 1));
            if ((d & Direcciones.Vertical) != 0) lista.Add(new Vector2Int(1, 0));
            if ((d & Direcciones.Diagonal) != 0) { lista.Add(new Vector2Int(1, 1)); lista.Add(new Vector2Int(-1, 1)); }
            if ((d & Direcciones.AlReves) != 0)
            {
                foreach (var v in lista.ToList()) lista.Add(new Vector2Int(-v.x, -v.y));
            }
            return lista;
        }

        private static bool Ubicar(Grilla g, string palabra, List<Vector2Int> dirs, System.Random azar)
        {
            int n = g.Letras.GetLength(0);
            for (int t = 0; t < 300; t++)
            {
                var d = dirs[azar.Next(dirs.Count)];
                int r0 = azar.Next(n), c0 = azar.Next(n);
                int r1 = r0 + d.x * (palabra.Length - 1), c1 = c0 + d.y * (palabra.Length - 1);
                if (r1 < 0 || r1 >= n || c1 < 0 || c1 >= n) continue;

                bool cabe = true;
                for (int i = 0; i < palabra.Length; i++)
                {
                    char actual = g.Letras[r0 + d.x * i, c0 + d.y * i];
                    if (actual != '\0' && actual != palabra[i]) { cabe = false; break; }
                }
                if (!cabe) continue;

                var pos = new List<Vector2Int>();
                for (int i = 0; i < palabra.Length; i++)
                {
                    int r = r0 + d.x * i, c = c0 + d.y * i;
                    g.Letras[r, c] = palabra[i];
                    pos.Add(new Vector2Int(r, c));
                }
                g.Posiciones[palabra] = pos;
                return true;
            }
            return false;
        }
    }
}
