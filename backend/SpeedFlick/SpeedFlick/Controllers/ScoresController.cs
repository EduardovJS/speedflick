using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SpeedFlick.Context;
using SpeedFlick.Models;
using System.Security.Claims;
using System;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ScoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public ScoresController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("Callback") };
        return Challenge(properties, "Discord");
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        var result = await HttpContext.AuthenticateAsync("Discord");

        if (!result.Succeeded)
            return Unauthorized();

        // Pegando os dados do usuário a partir dos Claims
        var discordId = result.Principal.FindFirst("discord_id")?.Value;
        var username = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
        var avatarUrl = result.Principal.FindFirst("avatar_url")?.Value;

        // Verificando se os dados necessários estão presentes
        if (discordId == null || username == null || avatarUrl == null)
        {
            return BadRequest("Erro ao obter os dados do usuário.");
        }

        // Criando ou atualizando o usuário no banco de dados
        var existingUser = await _context.UserScores
            .FirstOrDefaultAsync(x => x.UserId == discordId);

        if (existingUser == null)
        {
            var user = new UserScore
            {
                UserId = discordId,
                Username = username,
                AvatarUrl = avatarUrl
            };
            _context.UserScores.Add(user);
            await _context.SaveChangesAsync();
        }

        return Redirect("http://127.0.0.1:5500/index.html"); // Redireciona de volta para o jogo
    }

    // Endpoint para obter as informações do usuário logado
    [HttpGet("userinfo")]
    public IActionResult GetUserInfo()
    {
        var userId = User.FindFirst("discord_id")?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = _context.UserScores.FirstOrDefault(u => u.UserId == userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new { userId = user.UserId, username = user.Username, avatarUrl = user.AvatarUrl });
    }

    // Endpoint para obter o ranking
    [HttpGet("ranking")]
    public IActionResult GetRanking()
    {
        var ranking = _context.UserScores
            .OrderByDescending(x => x.Score)  // Ordena pelo score
            .Take(10)
            .ToList();

        return Ok(ranking);
    }

    // Endpoint para salvar a pontuação
    [HttpPost("save")]
    public async Task<IActionResult> SaveScore([FromBody] UserScore userScore)
    {
        if (userScore == null || string.IsNullOrEmpty(userScore.UserId))
        {
            return BadRequest("Dados inválidos.");
        }

        var existingUser = await _context.UserScores
            .FirstOrDefaultAsync(x => x.UserId == userScore.UserId);

        if (existingUser != null)
        {
            existingUser.Score = userScore.Score;
        }
        else
        {
            _context.UserScores.Add(userScore);
        }

        await _context.SaveChangesAsync();

        return Ok();
    }
}
