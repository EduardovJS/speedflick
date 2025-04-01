using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedFlick.Context;
using SpeedFlick.Models;

namespace SpeedFlick.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JogoController : Controller
    {
        private readonly AppDbContext _context;

        public JogoController(AppDbContext context)
        {
            _context = context;
        }

        // Inicia a autenticação com Discord
        [HttpGet("login")]
        public IActionResult Login()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/api/jogo/autenticar" }, "Discord");
        }

        // Autenticação e criação do usuário no banco de dados
        [HttpGet("autenticar")]
        public IActionResult Callback()
        {
            var userId = User.FindFirst("id")?.Value;
            var username = User.FindFirst("username")?.Value;
            var avatarUrl = User.FindFirst("avatar_url")?.Value;

            var userbd = new UserScore
            {
                UserId = userId,
                Username = username,
                AvatarUrl = avatarUrl
            };
            _context.Add(userbd);
            _context.SaveChanges();


            //  Retornando em obj JSON para teste
            var userProfile = new
            {
                UserId = userId,
                Username = username,
                AvatarUrl = avatarUrl
            };
            return Json(userProfile); // Redireciona para o método Conta()
        }

        // Verifica se o usuário está autenticado
        [HttpGet("verify")]
        public IActionResult Verify()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Ok();
            }
            else
            {
                return Unauthorized();
            }
        }

        // Atualiza a pontuação do usuário
        [HttpPost("update-score")]
        public IActionResult UpdateScore([FromBody] UserScore userScore)
        {
            var existingUser = _context.UserScores.FirstOrDefault(u => u.UserId == userScore.UserId);
            if (existingUser != null)
            {
                existingUser.Score = userScore.Score;
                _context.SaveChanges();
            }
            return Ok();
        }

        // Obtém o ranking dos jogadores
        [HttpGet("ranking")]
        public IActionResult GetRanking()
        {
            var ranking = _context.UserScores.OrderByDescending(u => u.Score).Take(10).ToList();
            return Ok(ranking);
        }
    }
}
