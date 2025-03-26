using Microsoft.AspNetCore.Authentication.Cookies;
using AspNet.Security.OAuth.Discord;
using Microsoft.EntityFrameworkCore;
using SpeedFlick.Context;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços ao container
builder.Services.AddControllers();

// banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Discord";
})
.AddCookie()
.AddOAuth("Discord",
    options =>
    {
        options.ClientId = builder.Configuration["Discord:ClientId"];
        options.ClientSecret = builder.Configuration["Discord:ClientSecret"];


        options.AuthorizationEndpoint = "https://discord.com/oauth2/authorize";
        options.Scope.Add("identify");
        options.CallbackPath = "/api/autenticar";

        options.TokenEndpoint = "https://discord.com/api/oauth2/token";
        options.UserInformationEndpoint = "https://discord.com/api/users/@me";


        options.ClaimActions.MapJsonKey("id", "id");
        options.ClaimActions.MapJsonKey("username", "username");
        options.ClaimActions.MapJsonKey("avatar", "avatar");

        options.Events = new OAuthEvents()
        {
            OnCreatingTicket = async context =>
            {
                // Realiza uma solicitação para o endpoint Get de informações do usuário do Discord 
                var request = new HttpRequestMessage(HttpMethod.Get, options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                // Envia a requesição do request
                var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                // Guarda as informacoes na variavel responseString como uma string JSON
                var responseString = await response.Content.ReadAsStringAsync();

                // Faz a desserializacao e transforma o JSON em um obj
                var user = JsonSerializer.Deserialize<JsonElement>(responseString);

                var userId = user.GetProperty("id").GetString();
                var username = user.GetProperty("username").GetString();
                var avatarhash = user.GetProperty("avatar").GetString();

                // Mapeamento os dados para os claims 
                context.Identity.AddClaim(new Claim("id", userId));
                context.Identity.AddClaim(new Claim("username", username));
                context.Identity.AddClaim(new Claim("avatar", avatarhash));

                // Criando a URL de avatar para mostrar a imagem
                var avatarUrl = $"https://cdn.discordapp.com/avatars/{userId}/{avatarhash}.png";
                context.Identity.AddClaim(new Claim("avatar_url", avatarUrl));

            }
        };
    });


// Configuracao CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

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
