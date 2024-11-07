using System;
using System.Collections.Generic;
using System.Linq;


namespace ContaminaDOSApi.Models
{
    public class JuegoContaminaDOS
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Nombre { get; set; }
        public string Estado { get; set; } = "lobby";
        public List<Jugador> Jugadores { get; private set; } = new List<Jugador>();
        public Jugador? LiderActual { get; private set; }
        public int DecadaActual { get; private set; } = 1;
        public int PuntosCiudadanos { get; private set; } = 0;
        public int PuntosPsicopatas { get; private set; } = 0;
        public string Owner { get; private set; }
        public string Password { get; private set; }
        public string? Psicopata { get; set; }  // Propiedad Psicopata opcional

        private Random random = new Random();
        private List<Ronda> Rondas { get; set; } = new List<Ronda>();

        public JuegoContaminaDOS(string nombre, string owner, string password)
        {
            Nombre = nombre;
            Owner = owner;
            Password = password;
        }

        public void AgregarJugador(string nombreJugador)
        {
            if (Jugadores.Count < 10)
            {
                Jugadores.Add(new Jugador(nombreJugador));
                Console.WriteLine($"{nombreJugador} se ha unido al juego.");
            }
            else
            {
                Console.WriteLine("El juego ya tiene el máximo de jugadores.");
            }
        }

        public void AsignarRoles()
        {
            int cantidadJugadores = Jugadores.Count;
            int cantidadPsicopatas = ObtenerCantidadPsicopatas(cantidadJugadores);

            List<int> indicesAsignados = new List<int>();
            while (indicesAsignados.Count < cantidadPsicopatas)
            {
                int index = random.Next(cantidadJugadores);
                if (!indicesAsignados.Contains(index))
                {
                    Jugadores[index].Rol = "Psicopata";
                    indicesAsignados.Add(index);
                }
            }

            foreach (var jugador in Jugadores)
            {
                if (jugador.Rol == null)
                {
                    jugador.Rol = "Ciudadano Ejemplar";
                }
            }
        }

        public void IniciarJuego()
        {
            if (Jugadores.Count >= 5 && Jugadores.Count <= 10)
            {
                Console.WriteLine("Iniciando juego. Asignando roles y comenzando la primera década...");
                Estado = "rounds";
                AsignarRoles();
                ComenzarNuevaDecada();
            }
            else
            {
                Console.WriteLine("No hay suficientes jugadores para comenzar.");
            }
        }

        public void ComenzarNuevaDecada()
        {
            if (PuntosCiudadanos >= 3 || PuntosPsicopatas >= 3)
            {
                FinalizarJuego();
                return;
            }

            LiderActual = Jugadores[random.Next(Jugadores.Count)];
            var nuevaRonda = new Ronda
            {
                Id = Guid.NewGuid().ToString(),
                Lider = LiderActual.Nombre
            };
            Rondas.Add(nuevaRonda);

            Console.WriteLine($"Nueva década {DecadaActual}. El líder es {LiderActual.Nombre}. Total rondas: {Rondas.Count}");
            DecadaActual++;
        }

        public List<Ronda> ObtenerRondas()
        {
            return Rondas;
        }

        public Ronda? ObtenerRonda(string roundId)
        {
            return Rondas.FirstOrDefault(r => r.Id == roundId);
        }

        public bool ProponerGrupo(string roundId, List<string> group)
        {
            var ronda = ObtenerRonda(roundId);
            if (ronda == null || ronda.Estado != "waiting-on-leader")
            {
                return false;
            }

            ronda.Grupo = group;
            ronda.Estado = "voting";
            ronda.Votos.Clear(); // Limpiar los votos anteriores
            return true;
        }

       public bool RegistrarVoto(string roundId, string jugador, bool vote)
        {
            var ronda = ObtenerRonda(roundId);
            if (ronda == null || ronda.Estado != "voting")
            {
                return false;
            }

            // Verificar si el jugador ya ha votado en esta ronda
            if (ronda.JugadoresQueYaVotaron.Contains(jugador))
            {
                Console.WriteLine($"El jugador '{jugador}' ya ha votado en esta ronda.");
                return false; // Rechazar si ya ha votado
            }

            // Registrar el voto y añadir el jugador a la lista de votantes
            ronda.Votos.Add(vote);
            ronda.JugadoresQueYaVotaron.Add(jugador);

            // Verificar si todos los jugadores del grupo han votado
            if (ronda.Votos.Count == Jugadores.Count)
            {
                int votosAFavor = ronda.Votos.Count(v => v == true);
                int votosEnContra = ronda.Votos.Count(v => v == false);

                if (votosEnContra >= votosAFavor) // Si hay más votos en contra o hay un empate
                {
                    ronda.IntentosDePropuesta++;

                    if (ronda.IntentosDePropuesta >= 3)
                    {
                        // Los psicópatas ganan la ronda después de 3 intentos fallidos
                        ronda.Estado = "ended";
                        ronda.Resultado = "enemies";
                        PuntosPsicopatas++;
                        Console.WriteLine("¡Los psicópatas ganan la ronda después de 3 intentos fallidos de propuesta!");
                    
                    
                        if(PuntosPsicopatas>=3)
                        {
                            FinalizarJuego();
                        }
                        else{
                            ComenzarNuevaDecada();
                        }}
                    else
                    {
                        // Reiniciar la ronda para permitir una nueva propuesta de grupo
                        ronda.Estado = "waiting-on-leader";
                        ronda.Votos.Clear();
                        ronda.JugadoresQueYaVotaron.Clear();
                        Console.WriteLine($"El grupo fue rechazado. Intentos de propuesta: {ronda.IntentosDePropuesta}");
                    }
                }
                else
                {
                    // El grupo fue aceptado, cambiar el estado a "waiting-on-group" para continuar
                    ronda.Estado = "waiting-on-group";
                    ronda.Fase++;
                    Console.WriteLine("El grupo fue aceptado. Continuando con la ronda.");
                }
            }

            return true;
        }



        public bool RegistrarAccion(string roundId, string jugador, bool action)
        {
            var ronda = ObtenerRonda(roundId);
            if (ronda == null || ronda.Estado != "waiting-on-group")
            {
                return false;
            }

            // Verificar si el jugador ya ha tomado una acción en esta ronda
            if (ronda.JugadoresQueYaTomaronAccion.Contains(jugador))
            {
                Console.WriteLine($"El jugador '{jugador}' ya realizó una acción en esta ronda.");
                return false; // Rechazar si ya ha tomado una acción
            }

            // Verificar que el jugador esté en el grupo de la ronda
            if (!ronda.Grupo.Contains(jugador))
            {
                Console.WriteLine($"El jugador '{jugador}' no forma parte del grupo propuesto para esta ronda.");
                return false; // Rechazar si el jugador no está en el grupo
            }

            // Registrar la acción y el jugador como quien ha tomado acción
            ronda.Acciones.Add(action);
            ronda.JugadoresQueYaTomaronAccion.Add(jugador);

            // Si todos los jugadores del grupo han realizado una acción, evaluar el resultado de la ronda
            if (ronda.JugadoresQueYaTomaronAccion.Count == ronda.Grupo.Count)
            {
                EvaluarResultadoRonda(ronda);
            }

            return true;
        }



        private void EvaluarResultadoRonda(Ronda ronda)
        {
            bool sabotaje = ronda.Acciones.Any(a => a == false);

            if (sabotaje)
            {
                PuntosPsicopatas++;
                ronda.Resultado = "enemies";
                Console.WriteLine("¡Sabotaje! Los psicópatas ganan esta ronda.");
            }
            else
            {
                PuntosCiudadanos++;
                ronda.Resultado = "citizens";
                Console.WriteLine("Los ciudadanos ejemplares logran salvar el planeta esta ronda.");
            }

            ronda.Estado = "ended";
            if (PuntosCiudadanos >= 3 || PuntosPsicopatas >= 3)
            {
                FinalizarJuego();
            }
            else{
                ComenzarNuevaDecada();
            }
        }

        public void FinalizarJuego()
        {
            if (PuntosCiudadanos >= 3)
            {
                Console.WriteLine("Los ciudadanos ejemplares ganan el juego.");
            }
            else if (PuntosPsicopatas >= 3)
            {
                Console.WriteLine("Los psicópatas ganan el juego.");
            }
            Estado = "ended";
        }

        public int ObtenerTamanioGrupoParaDecada(int decada, int jugadores)
        {
            int[,] grupoPorDecada = {
                { 2, 2, 2, 3, 3, 3 },
                { 3, 3, 3, 4, 4, 4 },
                { 2, 4, 3, 4, 4, 4 },
                { 3, 3, 4, 5, 5, 5 },
                { 3, 4, 4, 5, 5, 5 }
            };

            int filaDecada = decada - 2;       // Ahora la fila representa la década
            int columnaJugadores = jugadores - 5; // La columna representa el número de jugadores

            Console.WriteLine($"[Depuración] Fila para década ({decada}): {filaDecada}, Columna para jugadores ({jugadores}): {columnaJugadores}");

            return grupoPorDecada[filaDecada, columnaJugadores];
        }


        private int ObtenerCantidadPsicopatas(int jugadores)
        {
            int[] psicopatasPorJugadores = { 2, 2, 3, 3, 3, 4 };
            return psicopatasPorJugadores[jugadores - 5];
        }
    }

    public class Jugador
    {
        public string Nombre { get; set; }
        public string? Rol { get; set; }

        public Jugador(string nombre)
        {
            Nombre = nombre;
        }
    }
    public class PlayerRequest
    {
        public string Player { get; set; }
    }

    public class GroupUpdateRequest
    {
        public List<string> Group { get; set; }
    }


    public class Ronda
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Estado { get; set; } = "waiting-on-leader";
        public string Lider { get; set; }
        public int Fase { get; set; } = 1;
        public List<string> Grupo { get; set; } = new List<string>();
        public string Resultado { get; set; } = "none";
        public List<bool> Votos { get; set; } = new List<bool>();
        public List<bool> Acciones { get; set; } = new List<bool>();
        public HashSet<string> JugadoresQueYaVotaron { get; set; } = new HashSet<string>();
        public HashSet<string> JugadoresQueYaTomaronAccion { get; set; } = new HashSet<string>();
        public int IntentosDePropuesta { get; set; } = 0;



    }
}
