using Microsoft.AspNetCore.Authentication.Cookies;
using AspNet.Security.OAuth.Discord;
using Microsoft.EntityFrameworkCore;
using SpeedFlick.Context;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Adicionar servi�os ao container
builder.Services.AddControllers();

// banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// configura��o de autentica��o
builder.Services.AddAuthentication(options =>
{
    // Definindo o esquema de autentica��o para o Discord OAuth
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Usando Cookies como esquema principal
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Usando cookies tamb�m para sign-in
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/api/scores/login"; // Caminho para onde o usu�rio ser� redirecionado se n�o estiver autenticado
})
.AddOAuth("Discord", options =>
{
    options.ClientId = builder.Configuration["Discord:ClientId"];
    options.ClientSecret = builder.Configuration["Discord:ClientSecret"];
    options.CallbackPath = "/api/discord/callback";

    options.AuthorizationEndpoint = "https://discord.com/oauth2/authorize";
    options.TokenEndpoint = "https://discord.com/api/oauth2/token";
    options.UserInformationEndpoint = "https://discord.com/api/v10/users/@me";

    // Definindo o escopo da autentica��o (informa��es do usu�rio)
    options.Scope.Add("identify");

    options.SaveTokens = true; // Salvar tokens de acesso

    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            // Fazendo a requisi��o para pegar as informa��es do usu�rio no Discord
            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);

            var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseContentRead, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            // Lendo a resposta JSON com os dados do usu�rio
            var user = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

            // Extraindo os dados necess�rios
            var discordId = user.GetProperty("id").GetString();
            var username = user.GetProperty("username").GetString();
            var avatarHash = user.GetProperty("avatar").GetString();

            // Adicionando os Claims para nome de usu�rio e avatar
            context.Identity.AddClaim(new Claim(ClaimTypes.Name, username));
            context.Identity.AddClaim(new Claim("discord_id", discordId));
            context.Identity.AddClaim(new Claim("avatar", avatarHash));

            // Criando a URL do avatar para mostrar a imagem
            var avatarUrl = $"https://cdn.discordapp.com/avatars/{discordId}/{avatarHash}.png";
            context.Identity.AddClaim(new Claim("avatar_url", avatarUrl));
        }
    };
});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Habilitar autentica��o e autoriza��o
app.UseAuthentication(); // Adicione isso
app.UseAuthorization();

// Configurar Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Habilitar CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
