using Microsoft.AspNetCore.Mvc;
using ContaminaDOSApi.Models;  // Asegúrate de tener esta línea
using System.Collections.Generic;
using System.Linq;

namespace ContaminaDOSApi.Controllers
{
    [ApiController]
    [Route("api/games")]
    public class GameController : ControllerBase
    {
        private static List<JuegoContaminaDOS> juegos = new List<JuegoContaminaDOS>();

        /// Game Search
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
            return Ok(new { status = 200, msg = $"La búsqueda retornó {result.Count} resultados", data = result });
        }

        /// Game Create
       [HttpPost]
        public IActionResult CrearJuego([FromBody] CrearJuegoRequest request)
        {
            // Verificar si ya existe un juego con el mismo nombre
            if (juegos.Any(j => j.Nombre == request.Name))
            {
                return Conflict(new { status = 409, msg = $"El juego '{request.Name}' ya existe." });
            }

            // Crear un nuevo juego con todos los parámetros requeridos
            var nuevoJuego = new JuegoContaminaDOS(request.Name, request.Owner, request.Password ?? "");

            // Agregar el propietario como jugador al juego
            nuevoJuego.AgregarJugador(request.Owner);

            // Agregar el nuevo juego a la lista
            juegos.Add(nuevoJuego);

            // Construir la respuesta con la información específica solicitada
            return Created("", new
            {
                status = 201,
                msg = "Game Created",
                data = new
                {
                    id = nuevoJuego.Id,
                    name = nuevoJuego.Nombre,
                    status = nuevoJuego.Estado,
                    password = !string.IsNullOrEmpty(nuevoJuego.Password), // Indica si la contraseña está configurada
                    currentRound = nuevoJuego.ObtenerRondas().LastOrDefault()?.Id ?? "", // Asume que hay una ronda actual si existe
                    players = nuevoJuego.Jugadores.Select(j => j.Nombre).ToList(),
                    enemies = nuevoJuego.Jugadores.Where(j => j.Rol == "Psicopata").Select(j => j.Nombre).ToList()
                }
            });
        }


        /// Get content of the game
        [HttpGet("{gameId}")]
            public IActionResult ObtenerJuego(Guid gameId, [FromHeader] string player, [FromHeader] string password)
            {
                // Find the game by gameId
                var juego = juegos.FirstOrDefault(j => j.Id == gameId);
                if (juego == null)
                {
                    return NotFound(new { status = 404, msg = $"El juego con ID '{gameId}' no se encontró." });
                }

                // Verify password if necessary (assume a method or property exists to check)
                if (!juego.Password.Equals(password))
                {
                    return Unauthorized(new { status = 401, msg = "Contraseña incorrecta." });
                }

                // Include "psicopata" in the response
                var psicopata = string.IsNullOrEmpty(juego.Psicopata) ? "" : juego.Psicopata;

                // Construct the response
                return Ok(new
                {
                    status = 200,
                    msg = "Juego encontrado con éxito.",
                    data = new
                    {
                        id = juego.Id,
                        nombre = juego.Nombre,
                        estado = juego.Estado,
                        jugadores = juego.Jugadores,
                        owner = juego.Owner,
                    }
                });
            }


        /// Join Game
        [HttpPut("{gameId}")]
            public IActionResult UnirseJuego(Guid gameId, [FromBody] PlayerRequest request, [FromHeader] string playerName, [FromHeader] string password)
            {
                var juego = juegos.FirstOrDefault(j => j.Id == gameId);
                if (juego == null)
                {
                    return NotFound(new
                    {
                        status = 404,
                        msg = $"El juego con ID '{gameId}' no se encontró."
                    });
                }

                // Validación de la contraseña
                if (juego.Password != password)
                {
                    return StatusCode(403, new
                    {
                        status = 403,
                        msg = "Contraseña incorrecta."
                    });
                }

                // Validación de estado del juego
                if (juego.Estado == "iniciado")
                {
                    return Conflict(new
                    {
                        status = 409,
                        msg = "El juego ya está iniciado. No se pueden unir más jugadores."
                    });
                }

                // Validación de nombre de jugador
                if (string.IsNullOrWhiteSpace(request.Player))
                {
                    return BadRequest(new
                    {
                        status = 400,
                        msg = "El nombre del jugador es obligatorio."
                    });
                }

                // Verificar si ya existe un jugador con el mismo nombre
                if (juego.Jugadores.Any(j => j.Nombre == request.Player))
                {
                    return Conflict(new
                    {
                        status = 409,
                        msg = $"Ya existe un jugador con el nombre '{request.Player}' en el juego."
                    });
                }

                // Agregar el jugador si pasa todas las validaciones
                juego.AgregarJugador(request.Player);

                return Ok(new
                {
                    status = 200,
                    msg = $"El jugador '{request.Player}' se ha unido al juego con ID '{gameId}'",
                    data = juego
                });
            }


        /// Game start
        [HttpHead("{gameId}/start")]
            public IActionResult IniciarJuego(Guid gameId, [FromHeader] string playerName, [FromHeader] string password)
            {
                var juego = juegos.FirstOrDefault(j => j.Id == gameId);
                if (juego == null)
                {
                    Response.Headers.TryAdd("X-msg", "Juego no encontrado.");
                    return NotFound();
                }

                // Verificación: solo el propietario puede iniciar el juego
                if (juego.Owner != playerName)
                {
                    Response.Headers.TryAdd("X-msg", "Solo el propietario del juego puede iniciarlo.");
                    return StatusCode(403); // Código 403 Forbidden
                }

               if (juego.Password != password)
                {
                    return StatusCode(403, new
                    {
                        status = 403,
                        msg = "Contraseña incorrecta"
                    });
                }

                // Validación: verificar si el juego ya ha sido iniciado
                if (juego.Estado != "lobby")
                {
                    Response.Headers.TryAdd("X-msg", "Juego ya iniciado o ha finalizado.");
                    return Conflict();
                }

                // Validación: verificar que haya suficientes jugadores para iniciar el juego
                if (juego.Jugadores.Count < 5)
                {
                    Response.Headers.TryAdd("X-msg", "Se necesitan al menos 5 jugadores.");
                    return StatusCode(428); // Código de estado 428 Precondition Required
                }

                // Iniciar el juego si pasa todas las validaciones
                juego.IniciarJuego();
                Response.Headers.TryAdd("X-msg", "Juego iniciado.");
                return Ok();
            }


        /// Get Rounds
        [HttpGet("{gameId}/rounds")]
        public IActionResult ObtenerRondas(Guid gameId, [FromHeader] string playerName, [FromHeader] string password)
        {
            var juego = juegos.FirstOrDefault(j => j.Id == gameId);
            if (juego == null)
            {
                return NotFound(new
                {
                    status = 404,
                    msg = "Juego no encontrado."
                });
            }

            // Validación de la contraseña
            if (juego.Password != password)
            {
                return StatusCode(403, new
                {
                    status = 403,
                    msg = "Contraseña incorrecta."
                });
            }

            // Construir la respuesta con la información de las rondas
            var rondasData = juego.ObtenerRondas().Select(ronda => new
            {
                id = ronda.Id,
                leader = ronda.Lider,
                status = ronda.Estado,
                result = ronda.Resultado,
                phase = $"vote{ronda.Fase}",
                group = ronda.Grupo.Any() ? ronda.Grupo : new List<string>(), // Dejar vacío si no se ha propuesto grupo
                votes = ronda.Votos.Any() ? ronda.Votos : new List<bool>()    // Dejar vacío si no se ha votado
            }).ToList();

            return Ok(new
            {
                status = 200,
                msg = "Rounds found",
                data = rondasData
            });
        }



        /// Show Round
      [HttpGet("{gameId}/rounds/{roundId}")]
        public IActionResult ObtenerRonda(Guid gameId, string roundId, [FromHeader] string playerName, [FromHeader] string password)
        {
            var juego = juegos.FirstOrDefault(j => j.Id == gameId);
            if (juego == null)
            {
                return NotFound(new { status = 404, msg = "Juego no encontrado" });
            }

            // Validación de contraseña
            if (!string.IsNullOrEmpty(juego.Password) && juego.Password != password)
            {
                return StatusCode(403, new { status = 403, msg = "Contraseña incorrecta" });
            }

            // Verificar que se haya proporcionado el nombre del jugador
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return BadRequest(new { status = 400, msg = "Nombre de jugador requerido" });
            }

            // Comprobar si el jugador pertenece al juego
            if (!juego.Jugadores.Any(j => j.Nombre == playerName))
            {
                return StatusCode(403, new { status = 403, msg = "Jugador no pertenece al juego" });
            }

            // Buscar la ronda específica en el juego
            var ronda = juego.ObtenerRonda(roundId);
            if (ronda == null)
            {
                return NotFound(new { status = 404, msg = "Ronda no encontrada" });
            }

            // Devolver los detalles de la ronda, incluyendo la fase dinámica de votación
            var response = new
            {
                status = 200,
                msg = "Joined Game",
                data = new
                {
                    id = ronda.Id,
                    leader = ronda.Lider,
                    status = ronda.Estado,
                    result = ronda.Resultado,
                    phase = $"vote{ronda.Fase}", // Dinámicamente muestra la fase actual de votación
                    group = ronda.Grupo.Any() ? ronda.Grupo : new List<string>(), // Dejar vacío si no se ha propuesto grupo
                    votes = ronda.Votos.Any() ? ronda.Votos : new List<bool>()    // Dejar vacío si no se ha votado
                }
            };

            return Ok(response);
        }


        /// Propose Group in Round
        [HttpPatch("{gameId}/rounds/{roundId}")]
        public IActionResult ProponerGrupo(Guid gameId, string roundId, [FromHeader] string playerName, [FromHeader] string password, [FromBody] List<string> proposedGroup)
        {
            // Buscar el juego por gameId
            var juego = juegos.FirstOrDefault(j => j.Id == gameId);
            if (juego == null)
            {
                return NotFound(new { status = 404, msg = "Juego no encontrado" });
            }

            // Validación de contraseña
            if (!string.IsNullOrEmpty(juego.Password) && juego.Password != password)
            {
                return StatusCode(403, new { status = 403, msg = "Contraseña incorrecta" });
            }

            // Verificar que se haya proporcionado el nombre del jugador
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return BadRequest(new { status = 400, msg = "Nombre de jugador requerido" });
            }

            // Comprobar si el jugador pertenece al juego
            if (!juego.Jugadores.Any(j => j.Nombre == playerName))
            {
                return StatusCode(403, new { status = 403, msg = "Jugador no pertenece al juego" });
            }

            // Buscar la ronda específica en el juego
            var ronda = juego.ObtenerRonda(roundId);
            if (ronda == null)
            {
                return NotFound(new { status = 404, msg = "Ronda no encontrada" });
            }

            // Validar que solo el líder de la ronda pueda proponer el grupo
            if (ronda.Lider != playerName)
            {
                return StatusCode(403, new { status = 403, msg = "Solo el líder de la ronda puede proponer el grupo" });
            }

            // Verificar si el estado actual permite proponer un grupo
            if (ronda.Estado != "waiting-on-leader")
            {
                return Conflict(new { status = 409, msg = "Ya se ha propuesto un grupo para esta ronda." });
            }

            // Verificar si el grupo propuesto contiene jugadores que existen en el juego
            foreach (var jugador in proposedGroup)
            {
                if (!juego.Jugadores.Any(j => j.Nombre == jugador))
                {
                    return BadRequest(new { status = 400, msg = $"El jugador '{jugador}' no es parte del juego." });
                }
            }

            // Validar el tamaño del grupo propuesto según la década y la cantidad de jugadores
            int jugadoresTotales = juego.Jugadores.Count;
            int decadaActual = juego.DecadaActual;
            int tamanoEsperado = juego.ObtenerTamanioGrupoParaDecada(decadaActual, jugadoresTotales);

            if (proposedGroup.Count != tamanoEsperado)
            {
                return BadRequest(new { status = 400, msg = $"La cantidad de jugadores propuestos debe ser exactamente {tamanoEsperado} para esta ronda." });
            }

            // Asignar el grupo propuesto y cambiar el estado de la ronda
            ronda.Grupo = proposedGroup;
            ronda.Estado = "voting"; // Cambiar el estado a votación

            var response = new
            {
                status = 200,
                msg = "Group Created",
                data = new
                {
                    id = ronda.Id,
                    leader = ronda.Lider,
                    status = ronda.Estado,
                    result = ronda.Resultado,
                    phase = $"vote{ronda.Fase}",
                    group = ronda.Grupo,
                    votes = ronda.Votos
                }
            };

            return Ok(response);
        }




        /// Vote for Group Proposal in Round
        [HttpPost("{gameId}/rounds/{roundId}")]
        public IActionResult VotarEnRonda(
            Guid gameId,
            string roundId,
            [FromHeader(Name = "password")] string? password,
            [FromHeader(Name = "player")] string player,
            [FromBody] VotoRondaRequest request)
        {
            var juego = juegos.FirstOrDefault(j => j.Id == gameId);
            if (juego == null)
            {
                return NotFound(new { status = 404, msg = "El juego no se encontró." });
            }

            var ronda = juego.ObtenerRonda(roundId);
            if (ronda == null)
            {
                return NotFound(new { status = 404, msg = "La ronda no se encontró." });
            }

            // Validación de contraseña
            if (!string.IsNullOrEmpty(juego.Password) && juego.Password != password)
            {
                return StatusCode(403, new { status = 403, msg = "Contraseña incorrecta" });
            }

            // Verificar que el jugador pertenece al juego
            if (!juego.Jugadores.Any(j => j.Nombre == player))
            {
                return StatusCode(403, new { status = 403, msg = "El jugador no pertenece al juego" });
            }

            // Validación de estado para permitir el voto
            if (ronda.Estado != "voting")
            {
                return StatusCode(428, new { status = 428, msg = "La ronda no está en estado de votación" });
            }

            // Intentar registrar el voto
            var success = juego.RegistrarVoto(roundId, player, request.Vote);
            if (!success)
            {
                return Conflict(new { status = 409, msg = "No se registró el voto o el jugador ya votó en esta ronda." });
            }
                var response = new
                {
                    status = 200,
                    msg = "Voto registrado con éxito",
                    data = new
                    {
                        estado = ronda.Estado,
                        lider = ronda.Lider,
                        fase = ronda.Fase,
                        grupo = ronda.Grupo,
                        resultado = ronda.Resultado,
                        votos = ronda.Votos
                    }
                };

                return Ok(response);

        }



        /// Submit action as Round Group Member
        [HttpPut("{gameId}/rounds/{roundId}")]
        public IActionResult AccionEnRonda(Guid gameId, string roundId, [FromHeader(Name = "password")] string? password, [FromHeader(Name = "player")] string player, [FromBody] AccionRondaRequest request)
        {
            var juego = juegos.FirstOrDefault(j => j.Id == gameId);
            if (juego == null)
            {
                return NotFound(new { status = 404, msg = "El juego no se encontró." });
            }

            var ronda = juego.ObtenerRonda(roundId);
            if (ronda == null)
            {
                return NotFound(new { status = 404, msg = "La ronda no se encontró." });
            }

            // Validación de contraseña
            if (!string.IsNullOrEmpty(juego.Password) && juego.Password != password)
            {
                return StatusCode(403, new { status = 403, msg = "Contraseña incorrecta" });
            }

            // Verificar que el jugador pertenece al juego
            var jugador = juego.Jugadores.FirstOrDefault(j => j.Nombre == player);
            if (jugador == null)
            {
                return StatusCode(403, new { status = 403, msg = "El jugador no pertenece al juego" });
            }

            // Validación de estado para permitir la acción
            if (ronda.Estado != "waiting-on-group")
            {
                return StatusCode(428, new { status = 428, msg = "La ronda no está en estado de espera de acción." });
            }

            // Validación de rol: solo los psicópatas pueden sabotear (action = false)
            if (request.Action == false && jugador.Rol != "Psicopata")
            {
                return StatusCode(403, new { status = 403, msg = "Solo los psicópatas pueden sabotear." });
            }

            // Intentar registrar la acción
            var success = juego.RegistrarAccion(roundId, player, request.Action);
            if (!success)
            {
                return Conflict(new { status = 409, msg = "No se registró la acción o el jugador ya tomó una acción en esta ronda." });
            }

            var  response = new
            {
                status = 200,
                msg = "Acción registrada con éxito",
                data = new
                {
                    estado = ronda.Estado,
                    lider = ronda.Lider,
                    fase = ronda.Fase,
                    grupo = ronda.Grupo,
                    resultado = ronda.Resultado,
                    votos = ronda.Votos
                }
            };

            return Ok(response);

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

    public class VotoRondaRequest
    {
        public bool Vote { get; set; }
    }

    public class ProponerGrupoRequest
    {
        public List<string> Group { get; set; } = new List<string>();
    }

    public class AccionRondaRequest
    {
        public bool Action { get; set; }
    }

}
