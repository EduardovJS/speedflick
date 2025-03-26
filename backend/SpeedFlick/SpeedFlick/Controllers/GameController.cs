using Microsoft.AspNetCore.Authentication;
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

        [HttpGet("login")]
        public IActionResult Login()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/api/jogo/autenticar" }, "Discord");
        }

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
    }
}
