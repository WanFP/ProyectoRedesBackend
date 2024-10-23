using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ContaminaDOSApi.Controllers
{
    [ApiController]
    [Route("api/games")]
    public class GameController : ControllerBase
    {
        private static List<JuegoContaminaDOS> juegos = new List<JuegoContaminaDOS>();

        /// <summary>
        /// Game Search
        /// </summary>
        [HttpGet]
        public IActionResult BuscarJuegos([FromQuery] string? name, [FromQuery] string? status, [FromQuery] int page = 0, [FromQuery] int limit = 50)
        {
            var juegosFiltrados = juegos.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                juegosFiltrados = juegosFiltrados.Where(j => j.Nombre.Contains(name));
            }

            if (!string.IsNullOrEmpty(status))
            {
                juegosFiltrados = juegosFiltrados.Where(j => j.Estado == status);
            }

            var result = juegosFiltrados.Skip(page * limit).Take(limit).ToList();
            return Ok(new { status = 200, msg = $"Search returned {result.Count} result(s)", data = result });
        }

      [HttpPost]
public IActionResult CrearJuego([FromBody] CrearJuegoRequest request)
{
    // Verificar si ya existe un juego con el mismo nombre
    if (juegos.Any(j => j.Nombre == request.Name))
    {
        return Conflict(new { status = 409, msg = $"Game '{request.Name}' ya existe." });
    }

    // Crear un nuevo juego con todos los parámetros requeridos
    var nuevoJuego = new JuegoContaminaDOS(request.Name, request.Owner, request.Password ?? "");

    // Agregar el nuevo juego a la lista
    juegos.Add(nuevoJuego);

    return Created("", new { status = 201, msg = "Game Created", data = nuevoJuego });
}


        /// <summary>
        /// Get Game
        /// </summary>
        [HttpGet("{gameId}")]
        public IActionResult ObtenerJuego(string gameId)
        {
            var juego = juegos.FirstOrDefault(j => j.Nombre == gameId);
            if (juego == null)
            {
                return NotFound(new { status = 404, msg = $"Game '{gameId}' no encontrado." });
            }

            return Ok(new { status = 200, msg = "Game Found", data = juego });
        }

        /// <summary>
        /// Join Game
        /// </summary>
        [HttpPut("{gameId}")]
        public IActionResult UnirseJuego(string gameId, [FromBody] UnirseJuegoRequest request)
        {
            var juego = juegos.FirstOrDefault(j => j.Nombre == gameId);
            if (juego == null)
            {
                return NotFound(new { status = 404, msg = $"Game '{gameId}' no encontrado." });
            }

            juego.AgregarJugador(request.Player);

            return Ok(new { status = 200, msg = $"Jugador '{request.Player}' se ha unido al juego '{gameId}'", data = juego });
        }
    }

   public class CrearJuegoRequest
{
    public string Name { get; set; } = "";  // Inicializar con un valor predeterminado
    public string Owner { get; set; } = "";  // Inicializar con un valor predeterminado
    public string? Password { get; set; }  // Marcar como nullable si es opcional
}

public class UnirseJuegoRequest
{
    public string Player { get; set; } = "";  // Inicializar con un valor predeterminado
    public string? Password { get; set; }  // Marcar como nullable si es opcional
}

}
