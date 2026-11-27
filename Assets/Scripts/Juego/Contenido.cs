using System.Collections.Generic;
using System.Linq;

namespace ValijaPerdida
{
    /// <summary>
    /// Todo el contenido del juego: capítulos, sopas, interrogatorios, deducciones y finales.
    /// Para cambiar una sopa alcanza con editar sus palabras: la grilla se genera sola.
    /// Las palabras van en MAYÚSCULAS y sin tildes.
    /// </summary>
    public static class Contenido
    {
        public const int TotalSecretas = 10;

        public static readonly Dictionary<string, Capitulo> Capitulos = new List<Capitulo>
        {
            // ============================================================ PRÓLOGO
            new Capitulo
            {
                Id = "prologo", Numero = "Prólogo", Titulo = "El tren",
                Intro = new[]
                {
                    "Sierra Polvorienta, 1887.\n\nEl tren de las 6:40 avanza entre cerros colorados. Afuera, polvo. Adentro, ronquidos y olor a carbón.",
                    "Carlos Ríos, cazarrecompensas, se despierta con la cabeza pesada.\n\nDebajo del asiento ya no está su valija. Adentro iba el mapa de la Mina Perdida.",
                    "Solo le queda su anotador. Carlos escribe todo en clave, escondido en sopas de letras, para que nadie más pueda leerlo.\n\nEs hora de descifrar sus propias notas."
                },
                Sopas = new[]
                {
                    new Sopa
                    {
                        Tam = 6, Dirs = Direcciones.Horizontal, Semilla = 11, Tutorial = true,
                        Palabras = new[] { "VALIJA", "MAPA", "TREN" },
                        Secretas = new[] { "SOL" },
                        Nota = "Anoté tres cosas antes de dormirme. Tocá la primera letra y arrastrá hasta la última."
                    }
                },
                Deduccion = new Deduccion
                {
                    Pregunta = "Juntá tus pistas: VALIJA, MAPA y TREN. ¿Qué pasó?",
                    Opciones = new[]
                    {
                        new Opcion("La valija se cayó por la ventana", false, "Las ventanas no abren: el polvo las tiene trabadas. No pudo caerse."),
                        new Opcion("Alguien la bajó del tren en la estación", true, "El tren paró una sola vez durante la noche: en la estación de Sierra Polvorienta."),
                        new Opcion("La dejé olvidada en casa", false, "Carlos nunca se separa del mapa. Lo tenía al subir al tren.")
                    }
                },
                Pieza = "El tren y la sierra",
                Pista = "La valija desapareció de noche, durante la única parada del tren.",
                Siguiente = "estacion"
            },

            // ============================================================ CAPÍTULO 1
            new Capitulo
            {
                Id = "estacion", Numero = "Capítulo 1", Titulo = "La estación",
                Intro = new[]
                {
                    "La estación de Sierra Polvorienta es un andén de madera, un reloj que atrasa y un sheriff que mira demasiado la hora.",
                    "Carlos abre su anotador. Las palabras clave ahora dan pistas: aparecen en <color=#A3451F><b>rojo óxido</b></color>. Y desde acá, las palabras también pueden ir de arriba hacia abajo."
                },
                Sopas = new[]
                {
                    new Sopa
                    {
                        Tam = 7, Dirs = Direcciones.Horizontal | Direcciones.Vertical, Semilla = 21,
                        Palabras = new[] { "NORTE", "TREN", "RELOJ", "ANDEN", "BOLETO" },
                        Claves = new[] { "NORTE", "RELOJ" },
                        Secretas = new[] { "ORO" },
                        Nota = "El jefe de estación miraba el reloj cada dos minutos... ¿qué esperaba?"
                    },
                    new Sopa
                    {
                        Tam = 7, Dirs = Direcciones.Horizontal | Direcciones.Vertical, Semilla = 22,
                        Palabras = new[] { "CANTINA", "ROJO", "HUELLA", "BAUL", "POLVO" },
                        Claves = new[] { "CANTINA", "ROJO" },
                        Secretas = new[] { "BOTA" },
                        Nota = "En el andén vi un sombrero de un color que no se olvida."
                    }
                },
                Interrogatorio = new Interrogatorio
                {
                    Sospechoso = "Sheriff Carrizo",
                    Descripcion = "Autoridad local. Ayuda, pero con intereses propios.",
                    Apertura = "¿Ríos? Qué sorpresa. Si viene a denunciar algo, que sea rápido: tengo un tren que esperar.",
                    Preguntas = new[]
                    {
                        new Pregunta("NORTE", "El tren de carga salió al norte hace una hora. Le aseguro que nadie bajó una valija de ahí."),
                        new Pregunta("RELOJ", "¿El reloj? Espero a alguien. Asuntos míos, Ríos."),
                        new Pregunta("CANTINA", "Si busca chismes, vaya a la cantina. Ahí todo se sabe y todo se vende."),
                        new Pregunta("ROJO", "¿Un sombrero rojo? Rosa Méndez, carterista. Hace semanas que no la veo... oficialmente.")
                    }
                },
                Deduccion = new Deduccion
                {
                    Pregunta = "Combiná dos pistas. ¿Adónde vas ahora?",
                    Opciones = new[]
                    {
                        new Opcion("NORTE + TREN: seguir el tren de carga", false, "El sheriff juró que nadie bajó nada del tren de carga. Y lo dijo muy rápido."),
                        new Opcion("CANTINA + ROJO: buscar el sombrero rojo en la cantina", true, "Si Rosa Méndez está en el pueblo, está en la cantina."),
                        new Opcion("RELOJ + ANDEN: esperar el próximo tren", false, "Esperar no recupera valijas. Hay que moverse.")
                    }
                },
                Pieza = "El río seco",
                Pista = "Una mujer con sombrero rojo estuvo en el andén. El sheriff la conoce: Rosa Méndez.",
                Siguiente = "cantina"
            },

            // ============================================================ CAPÍTULO 2
            new Capitulo
            {
                Id = "cantina", Numero = "Capítulo 2", Titulo = "La cantina",
                Intro = new[]
                {
                    "La cantina «El Último Trago» huele a whisky barato y a deudas viejas. Un piano desafinado tapa las conversaciones.",
                    "Ahora las palabras también se esconden en diagonal.\n\nY prestá atención: la primera palabra clave que encuentres en la segunda página va a decidir a quién investigás."
                },
                Sopas = new[]
                {
                    new Sopa
                    {
                        Tam = 8, Dirs = Direcciones.Horizontal | Direcciones.Vertical | Direcciones.Diagonal, Semilla = 31,
                        Palabras = new[] { "PIANO", "DEUDA", "APUESTA", "VASO", "MESA" },
                        Claves = new[] { "DEUDA" },
                        Secretas = new[] { "NAIPE" },
                        Nota = "Alguien en esta cantina debe mucho dinero."
                    },
                    new Sopa
                    {
                        Tam = 8, Dirs = Direcciones.Horizontal | Direcciones.Vertical | Direcciones.Diagonal, Semilla = 32,
                        Palabras = new[] { "SOCIO", "LADRONA", "ZORRO", "RANCHO", "MERCADO", "SOMBRERO" },
                        Claves = new[] { "SOCIO", "LADRONA" },
                        Secretas = new[] { "ESPUELA" },
                        Nota = "Escuché dos sospechas. La primera que reconozca decide a quién sigo."
                    }
                },
                Interrogatorio = new Interrogatorio
                {
                    Sospechoso = "Rosa Méndez",
                    Descripcion = "Carterista. Sabe más de lo que dice.",
                    Apertura = "Sombrero rojo, sí, ya sé. Sentate, Ríos. Pero yo no te debo nada.",
                    Preguntas = new[]
                    {
                        new Pregunta("DEUDA", "Tomás Vidal le debe plata a medio pueblo. Al sheriff también, dicen."),
                        new Pregunta("SOCIO", "¿Tu socio? Vidal estuvo acá anoche, pagando deudas con oro que no tenía."),
                        new Pregunta("LADRONA", "¿Ladrona yo? Yo levanto billeteras, no valijas. Las valijas pesan, querido."),
                        new Pregunta("ZORRO", "Al Zorro le dicen así por algo. Pero hasta los zorros tienen miedo de alguien.")
                    }
                },
                Pieza = "La cruz del camino",
                Pista = "Vidal pagó deudas con oro la misma noche del robo. Rosa estaba en la cantina.",
                Bifurca = true
            },

            // ============================================================ RAMA A: EL SOCIO
            new Capitulo
            {
                Id = "rancho", Numero = "Capítulo 3", Titulo = "El rancho de Vidal",
                Intro = new[]
                {
                    "Seguiste la pista del SOCIO. El rancho de Tomás «El Zorro» Vidal está a dos horas a caballo, entre corrales vacíos.",
                    "Desde acá las palabras también pueden estar <b>al revés</b>. Y cuidado: algunas pistas son <b>trampas</b>.\n\nEn la segunda página hay letras escritas con <color=#2F5D62><b>tinta invisible</b></color>: conseguiste un reactivo para revelarlas."
                },
                Sopas = new[]
                {
                    new Sopa
                    {
                        Tam = 10, Dirs = Direcciones.Todas, Semilla = 41,
                        Palabras = new[] { "CABALLO", "CORRAL", "CARTA", "MONTURA", "BARRO" },
                        Claves = new[] { "CARTA", "BARRO" },
                        Secretas = new[] { "CUERVO" },
                        Nota = "Una carta firmada por Vidal, a la vista de todos. Demasiado fácil."
                    },
                    new Sopa
                    {
                        Tam = 10, Dirs = Direcciones.Todas, Semilla = 42,
                        Palabras = new[] { "MINA", "PALA", "DINAMITA", "LAMPARA", "CANDADO" },
                        Claves = new[] { "MINA", "DINAMITA" },
                        Secretas = new[] { "BRUJULA" },
                        Tinta = "DINAMITA",
                        Nota = "Hay algo escrito con tinta invisible. El reactivo lo puede revelar."
                    }
                },
                Interrogatorio = new Interrogatorio
                {
                    Sospechoso = "Tomás «El Zorro» Vidal",
                    Descripcion = "Ex socio de Carlos. ¿Traidor o aliado?",
                    Apertura = "Carlos. Sabía que ibas a venir. Bajá el arma, que de acá no me muevo.",
                    Preguntas = new[]
                    {
                        new Pregunta("CARTA", "Esa carta no la escribí yo. Mirá la letra: yo no sé escribir tan derecho."),
                        new Pregunta("BARRO", "El barro de mis botas es de la mina. Fui anoche, sí. Pero no encontré nada."),
                        new Pregunta("MINA", "La Mina Perdida es tan mía como tuya. Fuimos socios, ¿te acordás?"),
                        new Pregunta("DINAMITA", "Compré dinamita para llegar antes que vos. No para robarte.")
                    }
                },
                Deduccion = new Deduccion
                {
                    Pregunta = "Una de estas pistas es una trampa. ¿Cuál es la verdadera?",
                    Opciones = new[]
                    {
                        new Opcion("La CARTA firmada por Vidal", false, "La letra no es de Vidal. Alguien la escribió para inculparlo: es una pista trampa."),
                        new Opcion("El BARRO lleva a la MINA", true, "Las huellas de barro siguen hacia la mina. Ahí se esconde la verdad."),
                        new Opcion("El CABALLO de Vidal está cansado", false, "Cualquier caballo está cansado en este desierto. No prueba nada.")
                    }
                },
                Pieza = "La entrada de la mina",
                Pista = "La carta que culpa a Vidal es falsa. Alguien quiere que sospeches de él.",
                Siguiente = "mina"
            },
            new Capitulo
            {
                Id = "mina", Numero = "Capítulo 4", Titulo = "La mina",
                Intro = new[]
                {
                    "La boca de la mina respira aire frío. Adentro, el eco repite tus pasos... y otros pasos más.",
                    "Alguien te sigue por los túneles. <b>El reloj del perseguidor</b> está corriendo: resolvé cada página antes de que se acabe el tiempo.",
                    "Una cosa más: Vidal murmuró algo antes de que entraras. Si lo anotaste, va a estar escondido en la última página."
                },
                Sopas = new[]
                {
                    new Sopa
                    {
                        Tam = 12, Dirs = Direcciones.Todas, Semilla = 51, Segundos = 240,
                        Palabras = new[] { "TUNEL", "VAGONETA", "ECO", "VETA", "GRIETA", "DERRUMBE" },
                        Claves = new[] { "TUNEL" },
                        Secretas = new[] { "LINTERNA" },
                        Nota = "Los pasos se acercan. No hay tiempo."
                    },
                    new Sopa
                    {
                        Tam = 12, Dirs = Direcciones.Todas, Semilla = 52, Segundos = 240,
                        Palabras = new[] { "VALIJA", "TESORO", "TRAICION", "SALIDA", "CUEVA" },
                        Claves = new[] { "VALIJA", "TRAICION" },
                        Secretas = new[] { "DIAMANTE", "ARMONICA" },
                        Extra = "PERDON",
                        Nota = "¿Qué me dijo Vidal antes de entrar? Está escrito en algún lado de esta página, fuera de la lista."
                    }
                },
                Pieza = "La Mina Perdida",
                Pista = "La valija está en la mina. Y el que te sigue no es Vidal.",
                Ultimo = true
            },

            // ============================================================ RAMA B: LA LADRONA
            new Capitulo
            {
                Id = "mercado", Numero = "Capítulo 3", Titulo = "El mercado",
                Intro = new[]
                {
                    "Seguiste la pista de la LADRONA. El mercado del pueblo es un laberinto de puestos, telas y gritos. El lugar ideal para que Rosa Méndez trabaje.",
                    "Desde acá las palabras también pueden estar <b>al revés</b>. Y cuidado: algunas pistas son <b>trampas</b>.\n\nEn la segunda página hay letras escritas con <color=#2F5D62><b>tinta invisible</b></color>: conseguiste un reactivo para revelarlas."
                },
                Sopas = new[]
                {
                    new Sopa
                    {
                        Tam = 10, Dirs = Direcciones.Todas, Semilla = 61,
                        Palabras = new[] { "PUESTO", "BILLETERA", "TELA", "FRUTA", "CANASTA" },
                        Claves = new[] { "BILLETERA", "TELA" },
                        Secretas = new[] { "CUERVO" },
                        Nota = "Un sombrero rojo es fácil de copiar. ¿Era Rosa la del andén?"
                    },
                    new Sopa
                    {
                        Tam = 10, Dirs = Direcciones.Todas, Semilla = 62,
                        Palabras = new[] { "VAPOR", "PASAJE", "RIO", "BALANZA", "PLATA" },
                        Claves = new[] { "VAPOR", "PASAJE" },
                        Secretas = new[] { "BRUJULA" },
                        Tinta = "PASAJE",
                        Nota = "Hay algo escrito con tinta invisible. El reactivo lo puede revelar."
                    }
                },
                Interrogatorio = new Interrogatorio
                {
                    Sospechoso = "Rosa Méndez",
                    Descripcion = "Carterista. Sabe más de lo que dice.",
                    Apertura = "Otra vez vos. Está bien, Ríos: te cuento lo que vi. Pero después me dejás tranquila.",
                    Preguntas = new[]
                    {
                        new Pregunta("BILLETERA", "Sí, saqué billeteras en medio mercado. Tu valija no. Pero vi quién la llevaba."),
                        new Pregunta("TELA", "Alguien compró tela roja ayer. Mucha. Como para hacerse un sombrero igual al mío."),
                        new Pregunta("VAPOR", "El vapor sale esta noche del puerto. Y el sheriff tiene un pasaje."),
                        new Pregunta("PASAJE", "Un pasaje a nombre de Carrizo. Raro que un sheriff se vaya justo ahora, ¿no?")
                    }
                },
                Deduccion = new Deduccion
                {
                    Pregunta = "Una de estas pistas es una trampa. ¿Quién se lleva la valija?",
                    Opciones = new[]
                    {
                        new Opcion("Rosa, por su fama de LADRONA", false, "Su fama es la trampa: alguien copió su sombrero rojo para despistarte."),
                        new Opcion("El sheriff Carrizo, con un PASAJE en el VAPOR", true, "Carrizo se va esta noche. Y la valija se va con él."),
                        new Opcion("Vidal, por sus DEUDAS", false, "Vidal no aparece en el mercado. No hay ninguna pista que lo traiga hasta acá.")
                    }
                },
                Pieza = "El muelle del río",
                Pista = "El sombrero rojo del andén era una copia. El sheriff tiene un pasaje para el vapor.",
                Siguiente = "puerto"
            },
            new Capitulo
            {
                Id = "puerto", Numero = "Capítulo 4", Titulo = "El puerto",
                Intro = new[]
                {
                    "El puerto fluvial está cubierto de niebla. El vapor ya calienta las calderas y alguien camina detrás tuyo entre los cajones.",
                    "<b>El reloj del perseguidor</b> está corriendo: resolvé cada página antes de que se acabe el tiempo.",
                    "Rosa te dijo una última cosa: «no todo lo que lleva el sheriff es verdadero». Si lo anotaste, va a estar escondido en la última página."
                },
                Sopas = new[]
                {
                    new Sopa
                    {
                        Tam = 12, Dirs = Direcciones.Todas, Semilla = 71, Segundos = 240,
                        Palabras = new[] { "MUELLE", "CUERDA", "ANCLA", "NIEBLA", "FAROL", "BODEGA" },
                        Claves = new[] { "BODEGA" },
                        Secretas = new[] { "LINTERNA" },
                        Nota = "La sirena del vapor. Quedan minutos."
                    },
                    new Sopa
                    {
                        Tam = 12, Dirs = Direcciones.Todas, Semilla = 72, Segundos = 240,
                        Palabras = new[] { "VALIJA", "SHERIFF", "CAMAROTE", "TRAMPA", "PLACA" },
                        Claves = new[] { "VALIJA", "SHERIFF" },
                        Secretas = new[] { "DIAMANTE", "ARMONICA" },
                        Extra = "FALSO",
                        Nota = "¿Qué me dijo Rosa sobre el sheriff? Está escrito en algún lado de esta página, fuera de la lista."
                    }
                },
                Pieza = "La Mina Perdida",
                Pista = "La valija está en el vapor. El sheriff es el ladrón.",
                Ultimo = true
            }
        }.ToDictionary(c => c.Id);

        public static readonly Lugar[] Lugares =
        {
            new Lugar("prologo", "El tren", 110, 960),
            new Lugar("estacion", "La estación", 330, 790),
            new Lugar("cantina", "La cantina", 520, 590),
            new Lugar("rancho", "Rancho de Vidal", 200, 380, "A"),
            new Lugar("mina", "La mina", 170, 130, "A"),
            new Lugar("mercado", "El mercado", 760, 400, "B"),
            new Lugar("puerto", "El puerto", 790, 140, "B"),
        };

        public static readonly Dictionary<string, Final> Finales = new List<Final>
        {
            new Final
            {
                Id = "tesoro", Titulo = "Final 1 · El tesoro recuperado",
                Texto = new[]
                {
                    "Encontraste la palabra que nadie más vio. Con ella, todo cierra.",
                    "El sheriff Carrizo había robado la valija en la estación. Escribió una carta falsa para culpar a Vidal y copió el sombrero de Rosa para despistarte.",
                    "Con tus pruebas, el pueblo entero se entera. Carrizo pierde la placa, y vos recuperás el mapa de la Mina Perdida.",
                    "Esta vez no vas solo. Vidal te pide perdón por haber querido llegar primero, y Rosa se ofrece a guiarte. A veces, un buen detective también encuentra aliados."
                }
            },
            new Final
            {
                Id = "traicion", Titulo = "Final 2 · La traición",
                Texto = new[]
                {
                    "Al fondo de la mina encontrás la valija. Abierta. Vacía.",
                    "Vidal aparece con una lámpara y te mira sin decir nada. Detrás de él, el sheriff Carrizo guarda el mapa en su chaqueta.",
                    "«Le debía mucho dinero, Carlos», dice Vidal. Nunca vas a saber si te traicionó por miedo o por ambición.",
                    "Quizás había algo más en tus notas. Algo que Vidal intentó decirte antes de entrar."
                }
            },
            new Final
            {
                Id = "trampa", Titulo = "Final 3 · La trampa",
                Texto = new[]
                {
                    "En la bodega del vapor encontrás la valija y, cuando la abrís, la puerta se cierra detrás tuyo.",
                    "Adentro no hay ningún mapa. Solo una nota con la letra del sheriff: «Gracias por traerme hasta acá, Ríos».",
                    "El vapor se aleja del puerto. Carrizo baja en la orilla con el mapa verdadero.",
                    "Quizás había algo más en tus notas. Algo que Rosa intentó decirte sobre lo que llevaba el sheriff."
                }
            },
            new Final
            {
                Id = "oculto", Titulo = "Final oculto · El último anotador",
                Texto = new[]
                {
                    "Encontraste las diez palabras secretas: SOL, ORO, BOTA, NAIPE, ESPUELA, CUERVO, BRUJULA, LINTERNA, DIAMANTE y ARMONICA.",
                    "Leídas en orden, no son pistas sueltas. Son una ruta. Carlos la escribió hace años, antes de dibujar cualquier mapa.",
                    "El mapa de la valija nunca fue el original. El verdadero camino a la Mina Perdida siempre estuvo en el anotador.",
                    "Carlos cierra la libreta, ensilla su caballo y sale al amanecer. Esta vez, nadie lo sigue."
                }
            }
        }.ToDictionary(f => f.Id);

        public static Capitulo Siguiente(Capitulo actual, Estado estado)
        {
            if (actual.Bifurca) return Capitulos[estado.rama == "B" ? "mercado" : "rancho"];
            if (string.IsNullOrEmpty(actual.Siguiente)) return null;
            return Capitulos[actual.Siguiente];
        }
    }
}
