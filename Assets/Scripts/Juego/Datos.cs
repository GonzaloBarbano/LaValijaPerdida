using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValijaPerdida
{
    /// <summary>Direcciones habilitadas para ubicar palabras (progresión del apartado 4).</summary>
    [Flags]
    public enum Direcciones
    {
        Horizontal = 1,
        Vertical = 2,
        Diagonal = 4,
        AlReves = 8,
        Todas = Horizontal | Vertical | Diagonal | AlReves
    }

    /// <summary>Una página del anotador: una sopa de letras.</summary>
    public class Sopa
    {
        public int Tam;
        public Direcciones Dirs;
        public string[] Palabras;                       // palabras de la lista (visibles)
        public string[] Claves = new string[0];         // palabras clave: se destacan y avanzan la historia
        public string[] Secretas = new string[0];       // palabras secretas escondidas (no están en la lista)
        public string Extra;                            // palabra oculta que cambia el final (PERDON / FALSO)
        public string Tinta;                            // palabra escrita en tinta invisible (se revela con reactivo)
        public int Segundos;                            // > 0: reloj del perseguidor
        public bool Tutorial;
        public string Nota;                             // nota de Carlos al pie de la página
        public int Semilla = 1;
    }

    public class Pregunta
    {
        public string Palabra;
        public string Respuesta;
        public Pregunta(string palabra, string respuesta) { Palabra = palabra; Respuesta = respuesta; }
    }

    public class Interrogatorio
    {
        public string Sospechoso;
        public string Descripcion;
        public string Apertura;
        public Pregunta[] Preguntas;
    }

    public class Opcion
    {
        public string Texto;
        public bool Correcta;
        public string Devolucion;
        public Opcion(string texto, bool correcta, string devolucion) { Texto = texto; Correcta = correcta; Devolucion = devolucion; }
    }

    /// <summary>Combinar pistas y descartar palabras trampa.</summary>
    public class Deduccion
    {
        public string Pregunta;
        public Opcion[] Opciones;
    }

    public class Capitulo
    {
        public string Id;
        public string Numero;          // "Prólogo", "Capítulo 1", ...
        public string Titulo;          // "El tren"
        public string[] Intro;
        public Sopa[] Sopas;
        public Interrogatorio Interrogatorio;
        public Deduccion Deduccion;
        public string Pieza;           // pieza del mapa del tesoro que se obtiene
        public string Pista;           // pista que se anota en la libreta
        public string Siguiente;       // id del capítulo siguiente (null si se decide por rama o es el último)
        public bool Bifurca;           // la primera palabra clave encontrada decide la rama
        public bool Ultimo;            // al terminar se muestra el final
    }

    public class Final
    {
        public string Id;
        public string Titulo;
        public string[] Texto;
    }

    public class Lugar
    {
        public string CapituloId;
        public string Nombre;
        public float X, Y;             // posición en el mapa (sobre un área de 960x1100)
        public string Rama;            // "", "A" o "B"
        public Lugar(string id, string nombre, float x, float y, string rama = "")
        {
            CapituloId = id; Nombre = nombre; X = x; Y = y; Rama = rama;
        }
    }

    /// <summary>Estado de la partida. Se guarda como JSON (apartado 8).</summary>
    [Serializable]
    public class Estado
    {
        public string capitulo = "prologo";
        public int sopa;
        public string fase = "intro";        // intro, sopa, interrogatorio, deduccion, pieza, mapa, final
        public string rama = "";             // "A" (socio) o "B" (ladrona)
        public bool extra;                   // encontró PERDON o FALSO
        public int lupas = 3;
        public int reactivos;
        public int lupasUsadas;
        public float segundosJugados;
        public List<string> claves = new List<string>();
        public List<string> secretas = new List<string>();
        public List<string> pistas = new List<string>();
        public List<string> piezas = new List<string>();
        public List<string> completados = new List<string>();
    }

    public static class Guardado
    {
        private static string Ruta => Path.Combine(Application.persistentDataPath, "partida.json");

        public static bool Existe() => File.Exists(Ruta);

        public static void Guardar(Estado e)
        {
            try { File.WriteAllText(Ruta, JsonUtility.ToJson(e, true)); }
            catch (Exception ex) { Debug.LogWarning("No se pudo guardar: " + ex.Message); }
        }

        public static Estado Cargar()
        {
            try
            {
                if (Existe()) return JsonUtility.FromJson<Estado>(File.ReadAllText(Ruta)) ?? new Estado();
            }
            catch (Exception ex) { Debug.LogWarning("No se pudo cargar: " + ex.Message); }
            return new Estado();
        }

        public static void Borrar()
        {
            try { if (Existe()) File.Delete(Ruta); } catch { }
        }
    }
}
