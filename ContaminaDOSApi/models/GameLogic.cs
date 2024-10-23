using System;
using System.Collections.Generic;
using System.Linq;

public class JuegoContaminaDOS
{
    public string Nombre { get; set; }
    public string Estado { get; set; } = "lobby";
    public List<Jugador> Jugadores { get; private set; } = new List<Jugador>();
    public Jugador? LiderActual { get; private set; }
    public int DecadaActual { get; private set; } = 1;
    public int PuntosCiudadanos { get; private set; } = 0;
    public int PuntosPsicopatas { get; private set; } = 0;
    public string Owner { get; private set; }  // Agregado
    public string Password { get; private set; }  // Agregado
    private Random random = new Random();

    public JuegoContaminaDOS(string nombre, string owner, string password)
    {
        Nombre = nombre;
        Owner = owner;  // Asignación
        Password = password;  // Asignación
    }

    public void AgregarJugador(string nombreJugador)
    {
        if (Jugadores.Count < 10)
        {
            Jugadores.Add(new Jugador(nombreJugador));  // Crear un nuevo jugador
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
        int cantidadCiudadanos = cantidadJugadores - cantidadPsicopatas;

        // Asignar psicópatas
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

        // Asignar ciudadanos ejemplares
        foreach (var jugador in Jugadores)
        {
            if (jugador.Rol == null)
            {
                jugador.Rol = "Ciudadano Ejemplar";
            }
        }

        Console.WriteLine("Roles asignados:");
        foreach (var jugador in Jugadores)
        {
            Console.WriteLine($"{jugador.Nombre} es {jugador.Rol}");
        }
    }

    public void IniciarJuego()
    {
        if (Jugadores.Count >= 5 && Jugadores.Count <= 10)
        {
            Estado = "rounds";
            AsignarRoles();
            Console.WriteLine("El juego ha comenzado.");
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
        Console.WriteLine($"Nueva década {DecadaActual}. El líder es {LiderActual.Nombre}");

        ProponerGrupo();
    }

    public void ProponerGrupo()
    {
        int tamanioGrupo = ObtenerTamanioGrupoParaDecada(DecadaActual, Jugadores.Count);
        Console.WriteLine($"{LiderActual.Nombre} propone un grupo de {tamanioGrupo} jugadores.");

        // Aquí se debería implementar la lógica de la propuesta del grupo y votación
        EvaluarResultadoGrupo();
    }

    public void EvaluarResultadoGrupo()
    {
        bool haySabotaje = Jugadores.Any(j => j.Rol == "Psicopata" && random.Next(0, 2) == 1);  // Aleatoriamente decidir si hay sabotaje

        if (haySabotaje)
        {
            PuntosPsicopatas++;
            Console.WriteLine("¡Sabotaje! Los psicópatas ganan esta década.");
        }
        else
        {
            PuntosCiudadanos++;
            Console.WriteLine("Los ciudadanos ejemplares logran salvar el planeta esta década.");
        }

        DecadaActual++;
        ComenzarNuevaDecada();
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

    private int ObtenerTamanioGrupoParaDecada(int decada, int jugadores)
    {
        int[,] grupoPorDecada = {
            { 1, 2, 2, 2, 3 },
            { 3, 3, 3, 4, 4 },
            { 2, 4, 3, 4, 4 },
            { 3, 3, 4, 5, 5 },
            { 3, 4, 4, 5, 5 }
        };

        return grupoPorDecada[decada - 1, jugadores - 5];
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
