using System.Collections.Generic;
using UnityEngine;

namespace ValijaPerdida
{
    public enum Sfx { Tic, Acierto, Clave, Secreta, Error, Pagina, Lupa, Reactivo, Pieza, Reloj, Alarma }
    public enum Musica { Ninguna, Calma, Tension, Final }

    /// <summary>
    /// Sonido del juego (apartado 7 del GDD).
    /// Música de guitarra y efectos de papel y lápiz sintetizados por código, para que el
    /// juego suene sin depender de archivos externos. Si se agregan archivos en
    /// Assets/Resources/Audio con los nombres indicados, se usan esos en lugar de los sintetizados:
    ///   musica_calma, musica_tension, musica_final, sfx_tic, sfx_acierto, sfx_clave, ...
    /// </summary>
    public class Sonido : MonoBehaviour
    {
        private const int Frecuencia = 22050;

        private static Sonido instancia;

        private AudioSource fuenteMusica;
        private AudioSource fuenteEfectos;
        private readonly Dictionary<Sfx, AudioClip> efectos = new Dictionary<Sfx, AudioClip>();
        private readonly List<AudioClip> notasLetra = new List<AudioClip>();
        private readonly Dictionary<Musica, AudioClip> musicas = new Dictionary<Musica, AudioClip>();
        private Musica actual = Musica.Ninguna;
        private System.Random azar = new System.Random(1887);

        public static float VolumenMusica
        {
            get => PlayerPrefs.GetFloat("volMusica", 0.6f);
            set
            {
                PlayerPrefs.SetFloat("volMusica", Mathf.Clamp01(value));
                if (instancia != null) instancia.fuenteMusica.volume = VolumenMusica;
            }
        }

        public static float VolumenEfectos
        {
            get => PlayerPrefs.GetFloat("volEfectos", 0.8f);
            set
            {
                PlayerPrefs.SetFloat("volEfectos", Mathf.Clamp01(value));
                if (instancia != null) instancia.fuenteEfectos.volume = VolumenEfectos;
            }
        }

        public static void Inicializar(GameObject host)
        {
            if (instancia != null) return;
            instancia = host.AddComponent<Sonido>();
            instancia.Preparar();
        }

        public static void Reproducir(Sfx sfx)
        {
            if (instancia == null) return;
            if (instancia.efectos.TryGetValue(sfx, out var clip) && clip != null)
                instancia.fuenteEfectos.PlayOneShot(clip);
        }

        /// <summary>Nota de guitarra que sube de tono con cada letra del trazo.</summary>
        public static void ReproducirLetra(int indice)
        {
            if (instancia == null || instancia.notasLetra.Count == 0) return;
            var clip = instancia.notasLetra[Mathf.Clamp(indice, 0, instancia.notasLetra.Count - 1)];
            instancia.fuenteEfectos.PlayOneShot(clip, 0.7f);
        }

        public static void Poner(Musica musica)
        {
            if (instancia == null || instancia.actual == musica) return;
            instancia.actual = musica;
            var f = instancia.fuenteMusica;
            f.Stop();
            if (musica == Musica.Ninguna) return;
            if (instancia.musicas.TryGetValue(musica, out var clip) && clip != null)
            {
                f.clip = clip;
                f.Play();
            }
        }

        // ------------------------------------------------------------------ preparación

        private void Preparar()
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
            fuenteMusica.loop = true;
            fuenteMusica.playOnAwake = false;
            fuenteMusica.volume = VolumenMusica;

            fuenteEfectos = gameObject.AddComponent<AudioSource>();
            fuenteEfectos.playOnAwake = false;
            fuenteEfectos.volume = VolumenEfectos;

            // Notas para el trazo: escala menor pentatónica de La, de grave a agudo
            int[] escala = { 57, 60, 62, 64, 67, 69, 72, 74, 76, 79, 81, 84 };
            foreach (int m in escala)
                notasLetra.Add(Clip("letra", Normalizar(Punteo(Midi(m), 0.45f, 0.990f, 0.6f), 0.5f)));

            efectos[Sfx.Tic] = Cargar("sfx_tic") ?? Clip("tic", Normalizar(Papel(0.05f, 0.9f), 0.35f));
            efectos[Sfx.Acierto] = Cargar("sfx_acierto") ?? Clip("acierto", Normalizar(Acorde(new[] { 69, 72, 76 }, 0.06f, 1.2f), 0.7f));
            efectos[Sfx.Clave] = Cargar("sfx_clave") ?? Clip("clave", Normalizar(Acorde(new[] { 57, 64, 69, 72, 76, 81 }, 0.035f, 2.2f), 0.85f));
            efectos[Sfx.Secreta] = Cargar("sfx_secreta") ?? Clip("secreta", Normalizar(Acorde(new[] { 81, 84, 88, 93 }, 0.08f, 1.6f), 0.6f));
            efectos[Sfx.Error] = Cargar("sfx_error") ?? Clip("error", Normalizar(Papel(0.25f, 0.25f), 0.3f));
            efectos[Sfx.Pagina] = Cargar("sfx_pagina") ?? Clip("pagina", Normalizar(Papel(0.45f, 0.5f), 0.4f));
            efectos[Sfx.Lupa] = Cargar("sfx_lupa") ?? Clip("lupa", Normalizar(Campana(1760f, 0.5f), 0.5f));
            efectos[Sfx.Reactivo] = Cargar("sfx_reactivo") ?? Clip("reactivo", Normalizar(Papel(0.9f, 0.08f), 0.35f));
            efectos[Sfx.Pieza] = Cargar("sfx_pieza") ?? Clip("pieza", Normalizar(Acorde(new[] { 64, 69, 73, 76 }, 0.12f, 2.0f), 0.75f));
            efectos[Sfx.Reloj] = Cargar("sfx_reloj") ?? Clip("reloj", Normalizar(Papel(0.03f, 1f), 0.25f));
            efectos[Sfx.Alarma] = Cargar("sfx_alarma") ?? Clip("alarma", Normalizar(Acorde(new[] { 40, 41, 52, 53 }, 0.0f, 1.5f), 0.8f));

            musicas[Musica.Calma] = Cargar("musica_calma") ?? Clip("musica_calma", Normalizar(TemaCalma(), 0.55f));
            musicas[Musica.Tension] = Cargar("musica_tension") ?? Clip("musica_tension", Normalizar(TemaTension(), 0.55f));
            musicas[Musica.Final] = Cargar("musica_final") ?? musicas[Musica.Calma];
        }

        private static AudioClip Cargar(string nombre) => Resources.Load<AudioClip>("Audio/" + nombre);

        private static AudioClip Clip(string nombre, float[] datos)
        {
            var clip = AudioClip.Create(nombre, datos.Length, 1, Frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }

        // ------------------------------------------------------------------ síntesis

        private static float Midi(int nota) => 440f * Mathf.Pow(2f, (nota - 69) / 12f);

        /// <summary>Cuerda pulsada (algoritmo Karplus-Strong): suena parecido a una guitarra.</summary>
        private float[] Punteo(float frecuencia, float duracion, float decaimiento, float brillo)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt(Frecuencia / frecuencia));
            var buffer = new float[n];
            for (int i = 0; i < n; i++) buffer[i] = (float)(azar.NextDouble() * 2 - 1);
            // suavizado inicial: menos brillo = sonido más cálido, como cuerda de nylon
            for (int i = 1; i < n; i++) buffer[i] = Mathf.Lerp(buffer[i - 1], buffer[i], brillo);

            int total = Mathf.RoundToInt(duracion * Frecuencia);
            var salida = new float[total];
            int idx = 0;
            for (int i = 0; i < total; i++)
            {
                int sig = (idx + 1) % n;
                float v = buffer[idx];
                salida[i] = v;
                buffer[idx] = decaimiento * 0.5f * (v + buffer[sig]);
                idx = sig;
            }
            // ataque suave para evitar clics
            for (int i = 0; i < Mathf.Min(40, total); i++) salida[i] *= i / 40f;
            return salida;
        }

        private float[] Acorde(int[] notas, float separacion, float duracion)
        {
            int total = Mathf.RoundToInt((duracion + separacion * notas.Length) * Frecuencia);
            var mezcla = new float[total];
            for (int i = 0; i < notas.Length; i++)
            {
                var p = Punteo(Midi(notas[i]), duracion, 0.996f, 0.5f);
                Sumar(mezcla, p, Mathf.RoundToInt(i * separacion * Frecuencia), 0.6f, false);
            }
            return mezcla;
        }

        /// <summary>Ruido filtrado con envolvente: lápiz, papel, hoja que se da vuelta.</summary>
        private float[] Papel(float duracion, float claridad)
        {
            int total = Mathf.Max(1, Mathf.RoundToInt(duracion * Frecuencia));
            var s = new float[total];
            float previo = 0;
            for (int i = 0; i < total; i++)
            {
                float ruido = (float)(azar.NextDouble() * 2 - 1);
                previo = Mathf.Lerp(previo, ruido, claridad);
                float t = (float)i / total;
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 3f)) * (1f - t);
                s[i] = previo * env;
            }
            return s;
        }

        private static float[] Campana(float frecuencia, float duracion)
        {
            int total = Mathf.RoundToInt(duracion * Frecuencia);
            var s = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / Frecuencia;
                float env = Mathf.Exp(-t * 7f);
                s[i] = env * (Mathf.Sin(2 * Mathf.PI * frecuencia * t) + 0.4f * Mathf.Sin(2 * Mathf.PI * frecuencia * 2.76f * t));
            }
            return s;
        }

        private static void Sumar(float[] destino, float[] fuente, int desde, float ganancia, bool circular)
        {
            for (int i = 0; i < fuente.Length; i++)
            {
                int j = desde + i;
                if (j >= destino.Length)
                {
                    if (!circular) break;
                    j %= destino.Length;
                }
                destino[j] += fuente[i] * ganancia;
            }
        }

        private static float[] Normalizar(float[] s, float pico)
        {
            float max = 0.0001f;
            foreach (float v in s) max = Mathf.Max(max, Mathf.Abs(v));
            float k = pico / max;
            for (int i = 0; i < s.Length; i++) s[i] *= k;
            return s;
        }

        /// <summary>Tema principal: arpegios de guitarra criolla en La menor (Am - Dm - E - Am), 72 bpm.</summary>
        private float[] TemaCalma()
        {
            float corchea = 60f / 72f / 2f;
            int[][] acordes =
            {
                new[] { 45, 57, 60, 64, 69 },   // Am
                new[] { 50, 57, 62, 65, 69 },   // Dm
                new[] { 40, 56, 59, 64, 68 },   // E
                new[] { 45, 57, 60, 64, 69 },   // Am
                new[] { 41, 57, 60, 65, 69 },   // F
                new[] { 50, 57, 62, 65, 69 },   // Dm
                new[] { 40, 56, 59, 62, 68 },   // E7
                new[] { 45, 57, 60, 64, 69 },   // Am
            };
            int[] patron = { 0, 2, 3, 4, 3, 2, 1, 3 };
            int compases = acordes.Length;
            int total = Mathf.RoundToInt(compases * 8 * corchea * Frecuencia);
            var mezcla = new float[total];
            for (int c = 0; c < compases; c++)
            {
                for (int k = 0; k < 8; k++)
                {
                    int nota = acordes[c][patron[k]];
                    bool bajo = patron[k] == 0;
                    var p = Punteo(Midi(nota), bajo ? 2.6f : 1.6f, bajo ? 0.997f : 0.994f, bajo ? 0.35f : 0.5f);
                    int desde = Mathf.RoundToInt((c * 8 + k) * corchea * Frecuencia);
                    Sumar(mezcla, p, desde, bajo ? 0.9f : 0.45f, true);
                }
            }
            return mezcla;
        }

        /// <summary>Tema del reloj del perseguidor: ostinato grave y rápido (Mi - Fa), 104 bpm.</summary>
        private float[] TemaTension()
        {
            float semi = 60f / 104f / 4f;
            int[] notas = { 40, 40, 52, 40, 41, 41, 53, 41 };
            int compases = 4;
            int pasos = compases * 16;
            int total = Mathf.RoundToInt(pasos * semi * Frecuencia);
            var mezcla = new float[total];
            for (int i = 0; i < pasos; i++)
            {
                int desde = Mathf.RoundToInt(i * semi * Frecuencia);
                int nota = notas[(i / 2) % notas.Length];
                if (i % 2 == 0)
                    Sumar(mezcla, Punteo(Midi(nota), 0.7f, 0.993f, 0.4f), desde, 0.8f, true);
                if (i % 4 == 0)
                    Sumar(mezcla, Papel(0.04f, 0.95f), desde, 0.5f, true);
                if (i % 16 == 8)
                    Sumar(mezcla, Punteo(Midi(nota + 19), 1.2f, 0.995f, 0.5f), desde, 0.35f, true);
            }
            return mezcla;
        }
    }
}
