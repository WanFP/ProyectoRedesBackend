using Microsoft.AspNetCore.Mvc;
using ContaminaDOSApi.Models;  // Asegúrate de tener esta línea
using System.Collections.Generic;
using System.Linq;
using ContaminaDOSApi.data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.InteropServices;

namespace ContaminaDOSApi.Controllers
{
    [ApiController]
    [Route("api/games")]
    public class GameController : ControllerBase
    {
        //private static List<JuegoContaminaDOS> juegos = new List<JuegoContaminaDOS>();
        private readonly ContaminaDosDb _context;

        public GameController(ContaminaDosDb context)
        {
            _context = context;
        }


        /// Game Search
        [HttpGet]
        public async Task<IActionResult> BuscarJuegos([FromQuery] string? name, [FromQuery] string? status, [FromQuery] int page = 0, [FromQuery] int limit = 50)
        {
            var juegosFiltrados = _context.Juegos.Include(j => j.Jugadores).Include(j => j.Rondas).AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                juegosFiltrados = juegosFiltrados.Where(j => j.Nombre.Contains(name));
            }

            if (!string.IsNullOrEmpty(status))
            {
                juegosFiltrados = juegosFiltrados.Where(j => j.Estado == status);
            }

            var result = await juegosFiltrados.Skip(page * limit).Take(limit).ToListAsync();

            var responseData = result.Select(juego => new
            {
                id = juego.Id,
                name = juego.Nombre,
                status = juego.Estado,
                password = !string.IsNullOrEmpty(juego.Password),
                currentRound = juego.ObtenerRondas().LastOrDefault()?.Id ?? "",
                players = juego.Jugadores.Select(j => new
                {
                    id = j.Id,
                    name = j.Nombre,
                    role = j.Rol
                }).ToList(),
                enemies = juego.Jugadores.Where(j => j.Rol == "Psicopata").Select(j => j.Nombre).ToList()
            }).ToList();

            return Ok(new { status = 200, msg = $"La búsqueda retornó {result.Count} resultados", data = responseData });
        }

        /// Game Create
        [HttpPost]
        public IActionResult CrearJuego([FromBody] CrearJuegoRequest request)
        {
            // Verificar si ya existe un juego con el mismo nombre
            if (_context.Juegos.Any(j => j.Nombre == request.Name))
            {
                return Conflict(new { status = 409, msg = $"El juego '{request.Name}' ya existe." });
            }

            // Crear un nuevo juego con todos los parámetros requeridos
            var nuevoJuego = new JuegoContaminaDOS(request.Name, request.Owner, request.Password ?? "");

            // Agregar el propietario como jugador al juego
            nuevoJuego.AgregarJugador(request.Owner, nuevoJuego.Id);

            // Agregar el nuevo juego a la lista
            _context.Juegos.Add(nuevoJuego);

            //Guardar los cambios en la base de datos
            _context.SaveChanges();

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
                    players = nuevoJuego.Jugadores.Select(j => new
                    {
                        id = j.Id,
                        name = j.Nombre,
                        role = j.Rol
                    }).ToList(),
                    enemies = nuevoJuego.Jugadores.Where(j => j.Rol == "Psicopata").Select(j => j.Nombre).ToList()
                }
            });
        }


        /// Get content of the game
        [HttpGet("{gameId}")]
        public IActionResult ObtenerJuego(Guid gameId, [FromHeader] string player, [FromHeader] string password)
        {
            // Find the game by gameId
            var juego = _context.Juegos.Include(j => j.Jugadores).FirstOrDefault(j => j.Id == gameId);
            if (juego == null)
            {
                return NotFound(new { status = 404, msg = $"El juego con ID '{gameId}' no se encontró." });
            }

            // Verify password if necessary (assume a method or property exists to check)
            if (!string.IsNullOrEmpty(juego.Password) && !juego.Password.Equals(password))
            {
                return Unauthorized(new { status = 401, msg = "Contraseña incorrecta." });
            }

            // Include "psicopata" in the response
            var psicopata = juego.Jugadores.FirstOrDefault(j => j.Rol == "Psicopata")?.Nombre ?? "";

            // Construct the response
            return Ok(new
            {
                status = 200,
                msg = "Juego encontrado con éxito.",
                data = new
                {
                    id = juego.Id,
                    name = juego.Nombre,
                    status = juego.Estado,
                    players = juego.Jugadores.Select(j => new
                    {
                        id = j.Id,
                        name = j.Nombre,
                        role = j.Rol
                    }).ToList(),
                    owner = juego.Owner,
                    psicopata = psicopata
                }
            });
        }


        /// Join Game
        [HttpPut("{gameId}")]
        public async Task<IActionResult> UnirseJuego(Guid gameId, [FromBody] PlayerRequest request, [FromHeader] string playerName, [FromHeader] string password)
        {

            var juego = await _context.Juegos.Include(j => j.Jugadores).FirstOrDefaultAsync(j => j.Id == gameId);
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

            Jugador player = juego.AgregarJugador(request.Player, gameId);

            try
            {
                _context.Jugadores.Add(player);
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, new
                {
                    status = 500,
                    msg = "Error de concurrencia al guardar los cambios.",
                    error = ex.Message
                });
            }

            return Ok(new
            {
                status = 200,
                msg = $"El jugador '{request.Player}' se ha unido al juego con ID '{gameId}'",
                data = new
                {
                    id = juego.Id,
                    name = juego.Nombre,
                    status = juego.Estado,
                    players = juego.Jugadores.Select(j => new
                    {
                        id = j.Id,
                        name = j.Nombre,
                        role = j.Rol
                    }),
                    owner = juego.Owner,
                }
            });
        }


        /// Game start
        [HttpHead("{gameId}/start")]
        public async Task<IActionResult> IniciarJuego(Guid gameId, [FromHeader] string playerName, [FromHeader] string password)
        {
            try
            {
                var juego = await _context.Juegos.Include(j => j.Jugadores).FirstOrDefaultAsync(j => j.Id == gameId);
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
                juego.Estado = "iniciado";
                juego.IniciarJuego();

                var nuevaRonda = juego.Rondas.LastOrDefault(); // Obtener la ronda recién creada

                if (nuevaRonda != null)
                {
                    _context.Rondas.Add(nuevaRonda); // Añadir la nueva ronda al contexto
                }

                _context.SaveChanges();
                Response.Headers.TryAdd("X-msg", "Juego iniciado.");

                return Ok(new
                {
                    status = 200,
                    msg = "Juego iniciado con éxito.",
                    data = new
                    {
                        id = juego.Id,
                        name = juego.Nombre,
                        status = juego.Estado,
                        currentRound = nuevaRonda != null ? nuevaRonda.Id : null,
                        players = juego.Jugadores.Select(j => new
                        {
                            id = j.Id,
                            name = j.Nombre,
                            role = j.Rol
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                // Registrar el error para depuración
                Console.WriteLine($"Error al iniciar el juego: {ex.Message}");
                return StatusCode(500, new
                {
                    status = 500,
                    msg = "Error interno al iniciar el juego.",
                    error = ex.Message
                });
            }
        }


        /// Get Rounds
        [HttpGet("{gameId}/rounds")]
        public async Task<IActionResult> ObtenerRondas(Guid gameId, [FromHeader] string playerName, [FromHeader] string password)
        {
            var juego = await _context.Juegos.Include(j => j.Rondas).FirstOrDefaultAsync(j => j.Id == gameId);
            if (juego == null)
            {
                return NotFound(new
                {
                    status = 404,
                    msg = "Juego no encontrado."
                });
            }

            // Validación de la contraseña
            if (!string.IsNullOrEmpty(juego.Password) && juego.Password != password)
            {
                return StatusCode(403, new
                {
                    status = 403,
                    msg = "Contraseña incorrecta."
                });
            }

            // Construir la respuesta con la información de las rondas
            var rondasData = juego.Rondas.Select(ronda => new
            {
                id = ronda.Id,
                leader = ronda.Lider,
                status = ronda.Estado,
                result = ronda.Resultado,
                phase = $"vote{ronda.Fase}",
                group = ronda.Grupo.Any() ? ronda.Grupo : new List<string>(), // Dejar vacío si no se ha propuesto grupo
                votes = ronda.Votos.Any() ? ronda.Votos : new List<Voto>()    // Dejar vacío si no se ha votado
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
        public async Task<IActionResult> ObtenerRonda(Guid gameId, string roundId, [FromHeader] string playerName, [FromHeader] string password)
        {
            var juego = await _context.Juegos.Include(j => j.Rondas).FirstOrDefaultAsync(j => j.Id == gameId);
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
            if (!juego.Jugadores.Any(j => j.Nombre == playerName) && juego.Owner != playerName)
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
                    votes = ronda.Votos.Any() ? ronda.Votos : new List<Voto>()   // Dejar vacío si no se ha votado
                }
            };

            return Ok(response);
        }


        /// Propose Group in Round
        [HttpPatch("{gameId}/rounds/{roundId}")]
        public async Task<IActionResult> ProponerGrupo(Guid gameId, string roundId, [FromHeader] string playerName, [FromHeader] string password, [FromBody] List<string> proposedGroup)
        {
            // Buscar el juego por gameId
            var juego = await _context.Juegos.Include(j => j.Rondas).Include(j => j.Jugadores).FirstOrDefaultAsync(j => j.Id == gameId);
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

            _context.SaveChanges();

            var response = new
            {
                status = 200,
                msg = "Group proposed successfully",
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
        public async Task<IActionResult> VotarEnRonda(
            Guid gameId,
            string roundId,
            [FromHeader(Name = "password")] string? password,
            [FromHeader(Name = "player")] string player,
            [FromBody] VotoRondaRequest request)
        {
            var juego = _context.Juegos
                .Include(j => j.Rondas)
                .ThenInclude(r => r.Votos)
                .Include(j => j.Jugadores)
                .FirstOrDefault(j => j.Id == gameId);
            if (juego == null)
            {
                return NotFound(new { status = 404, msg = "El juego no se encontró." });
            }

            var ronda = juego.Rondas.FirstOrDefault(r => r.Id == roundId);
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

            // Intentar registrar el nuevo voto
            if (ronda.JugadoresQueYaVotaron.Contains(player))
            {
                return Conflict(new { status = 409, msg = "El jugador ya votó en esta ronda." });
            }

            var voto = new Voto
            {
                RondaId = ronda.Id,
                JugadorNombre = player,
                Valor = request.Vote
            };

            ronda.Votos.Add(voto);
            ronda.JugadoresQueYaVotaron.Add(player);
            _context.Votos.Add(voto);
            await _context.SaveChangesAsync();

            // Verificar si todos los jugadores han votado
            if (ronda.Votos.Count == juego.Jugadores.Count)
            {
                int votosAFavor = ronda.Votos.Count(v => v.Valor == true);
                int votosEnContra = ronda.Votos.Count(v => v.Valor == false);

                if (votosEnContra >= votosAFavor)
                {
                    // Propuesta rechazada: reiniciar votos y devolver el estado al método de proponer grupo
                    _context.Votos.RemoveRange(ronda.Votos); // Elimina los votos existentes
                    ronda.Votos.Clear();                     // Limpia la lista de votos en memoria
                    ronda.JugadoresQueYaVotaron.Clear();     // Limpia la lista de jugadores que votaron
                    ronda.Estado = "waiting-on-leader";      // Cambiar el estado al necesario para proponer un nuevo grupo

                    await _context.SaveChangesAsync();
                    return Ok(new
                    {
                        status = 200,
                        msg = "La propuesta fue rechazada. Los votos han sido reiniciados. La ronda está lista para proponer un nuevo grupo.",
                        data = new
                        {
                            estado = ronda.Estado
                        }
                    });
                }
                else
                {
                    // Propuesta aceptada
                    ronda.Estado = "waiting-on-group";
                    await _context.SaveChangesAsync();
                    return Ok(new { status = 200, msg = "La propuesta fue aceptada." });
                }
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
                    votos = ronda.Votos.Select(v => new
                    {
                        v.JugadorNombre,
                        v.Valor
                    }).ToList()
                }
            };
            return Ok(response);
        }




        /// Submit action as Round Group Member
        [HttpPut("{gameId}/rounds/{roundId}")]
        public async Task<IActionResult> AccionEnRonda(Guid gameId, string roundId, [FromHeader(Name = "password")] string? password, [FromHeader(Name = "player")] string player, [FromBody] AccionRondaRequest request)
        {
            var juego = await _context.Juegos
                .Include(j => j.Rondas)
                .ThenInclude(r => r.Acciones)
                .Include(j => j.Jugadores)
                .FirstOrDefaultAsync(j => j.Id == gameId);

            if (juego == null)
            {
                return NotFound(new { status = 404, msg = "El juego no se encontró." });
            }

            var ronda = juego.Rondas.FirstOrDefault(r => r.Id == roundId);
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
            if (!request.Action && jugador.Rol != "Psicopata")
            {
                return StatusCode(403, new { status = 403, msg = "Solo los psicópatas pueden sabotear." });
            }

            var accion = new Accion
            {
                RondaId = ronda.Id,
                JugadorNombre = player,
                Valor = request.Action
            };

            // Registrar la acción utilizando el modelo
            var success = juego.RegistrarAccion(roundId, player, accion);
            if (!success)
            {
                return Conflict(new { status = 409, msg = "No se registró la acción o el jugador ya tomó una acción en esta ronda." });
            }

            try
            {
                // Guardar cambios en la base
                _context.Acciones.Add(accion);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, new
                {
                    status = 500,
                    msg = "Error al guardar los cambios en la base de datos.",
                    error = ex.Message
                });
            }

            return Ok(new
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
                    acciones = ronda.Acciones.Select(a => new { a.JugadorNombre, a.Valor }).ToList()
                }
            });
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
