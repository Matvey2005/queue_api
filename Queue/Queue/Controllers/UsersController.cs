using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Queue.Models;
using Queue.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Queue.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        static string secretKey = "super_super_secret_key_with_256_bits_!!!"; // 256+ бит
        static string issuer = "MyApp";
        static string audience = "MyUsers";
        static SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        private readonly UserRepository repositories;
        private readonly ScheduleRepository scheduleRepository;
        private readonly ApplicationDbContext _context;

        public UsersController(UserRepository repositories, ScheduleRepository scheduleRepository, ApplicationDbContext context)
        {
            this.repositories = repositories;
            this.scheduleRepository = scheduleRepository;
            _context = context;
        }
        string GenerateToken(string username, int id, int minutes)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("registr")]
        public async Task<IActionResult> Registration([FromBody] CreateUserRequest user)
        {
            User createUser = await repositories.Add(
                user.UserName,
                user.Password,
                user.Email,
                user.GroupId,
                user.SubgroupNumber
            );

            if (createUser == null)
                return Unauthorized("Пользователь с таким именем уже существует");

            //// Находим группу пользователя
            //var group = await _context.Groups.FindAsync(user.GroupId);
            //if (group == null)
            //    return BadRequest("Группа не найдена");
            //string scheduleMessage;
            //try
            //{
            //    // Загружаем расписание для этой группы
            //    int addedCount = await scheduleRepository.LoadScheduleForGroupAsync(group.Name, user.GroupId);
            //    scheduleMessage = $"Регистрация прошла успешно. Загружено {addedCount} занятий.";
            //}
            //catch (Exception ex)
            //{
            //    scheduleMessage = $"Регистрация прошла успешно, но при загрузке расписания произошла ошибка: {ex.Message}";
            //}

            //// Возвращаем объект с пользователем и сообщением
            //return Ok(new
            //{
            //    message = scheduleMessage,
            //    user = new
            //    {
            //        createUser.Id,
            //        createUser.UserName,
            //        createUser.Email,
            //        createUser.GroupId,
            //        createUser.SubgroupNumber
            //    }
            //});
            return Ok("Регистрация прошла успешно!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUser loginUser)
        {

            User user = await repositories.GetUser(loginUser.Password, loginUser.Email);

            if (user == null)
                return Unauthorized();

            var accessToken = GenerateToken(user.UserName, user.Id, 15);      // 1 минута
            var refreshToken = GenerateToken(user.UserName, user.Id, 60);    // 60 минут

            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,                // ✅ обязательно, если SameSite=None
                SameSite = SameSiteMode.None, // ✅ иначе куки не отправятся с фронта
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(60)
            });

            // Находим группу пользователя
            var group = await _context.Groups.FindAsync(user.GroupId);
            if (group == null)
                return BadRequest("Группа не найдена");
            string scheduleMessage;
            try
            {
                // Загружаем расписание для этой группы
                int addedCount = await scheduleRepository.LoadScheduleForGroupAsync(group.Name, user.GroupId);
                scheduleMessage = $"Регистрация прошла успешно. Загружено {addedCount} занятий.";
            }
            catch (Exception ex)
            {
                scheduleMessage = $"Регистрация прошла успешно, но при загрузке расписания произошла ошибка: {ex.Message}";
            }

            // Возвращаем объект с пользователем и сообщением
            return Ok(user);

        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("access_token", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = true,                // Должно совпадать с установкой
                SameSite = SameSiteMode.None  // Должно совпадать с установкой
            });

            // Удаляем refresh_token с теми же параметрами, что и при установке
            Response.Cookies.Delete("refresh_token", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = true,                // Должно совпадать с установкой
                SameSite = SameSiteMode.None  // Должно совпадать с установкой
            });
            return Ok("Вы вышли");
        }

        [HttpGet("lengthAllStudentsByGroup/{groupId}")]
        [Authorize]
        public async Task<IActionResult> LengthAllStudentsByGroup(int groupId)
        {
            int length = await _context.Users
                .Where(u => u.GroupId == groupId)
                .CountAsync();

            return Ok(length);
        }

        [HttpGet("lengthAllStudentsBySubgroup/{groupId}/{subgroup}")]
        [Authorize]
        public async Task<IActionResult> LengthAllStudentsBySubgroup(int groupId, int subgroup)
        {
            int length = await _context.Users
                .Where(u => u.GroupId == groupId && u.SubgroupNumber == subgroup)
                .CountAsync();

            return Ok(length);
        }


        //public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        //{
        //    // Другие middleware...

        //    app.Use(async (context, next) =>
        //    {
        //        var handler = new JwtSecurityTokenHandler();
        //        var accessToken = context.Request.Cookies["access_token"];
        //        var refreshToken = context.Request.Cookies["refresh_token"];

        //        ClaimsPrincipal? principal = null;

        //        if (!string.IsNullOrEmpty(accessToken))
        //        {
        //            try
        //            {
        //                principal = handler.ValidateToken(accessToken, new TokenValidationParameters
        //                {
        //                    ValidateIssuer = true,
        //                    ValidIssuer = issuer,
        //                    ValidateAudience = true,
        //                    ValidAudience = audience,
        //                    ValidateIssuerSigningKey = true,
        //                    IssuerSigningKey = signingKey,
        //                    ValidateLifetime = true,
        //                    ClockSkew = TimeSpan.Zero
        //                }, out _);
        //            }
        //            catch (SecurityTokenExpiredException)
        //            {
        //                // Токен просрочен — попробуем обновить
        //            }
        //            catch
        //            {
        //                // access_token невалиден — удалим
        //                context.Response.Cookies.Delete("access_token");
        //            }
        //        }

        //        if (principal == null && !string.IsNullOrEmpty(refreshToken))
        //        {
        //            try
        //            {
        //                var refreshPrincipal = handler.ValidateToken(refreshToken, new TokenValidationParameters
        //                {
        //                    ValidateIssuer = true,
        //                    ValidIssuer = issuer,
        //                    ValidateAudience = true,
        //                    ValidAudience = audience,
        //                    ValidateIssuerSigningKey = true,
        //                    IssuerSigningKey = signingKey,
        //                    ValidateLifetime = true,
        //                    ClockSkew = TimeSpan.Zero
        //                }, out _);

        //                var username = refreshPrincipal.Identity?.Name;
        //                var idClaim = refreshPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        //                var userId = idClaim != null ? int.Parse(idClaim.Value) : (int?)null;
        //                var newAccessToken = GenerateToken(username, userId.Value, 1);

        //                context.Response.Cookies.Append("access_token", newAccessToken, new CookieOptions
        //                {
        //                    HttpOnly = true,
        //                    SameSite = SameSiteMode.Lax,
        //                    Expires = DateTime.UtcNow.AddMinutes(1)
        //                });

        //                // Добавим в заголовок, чтобы JwtBearer его увидел
        //                context.Request.Headers["Authorization"] = $"Bearer {newAccessToken}";
        //            }
        //            catch
        //            {
        //                context.Response.Cookies.Delete("refresh_token");
        //            }
        //        }

        //        // Устанавливаем principal в контексте, если он валиден
        //        if (principal != null)
        //        {
        //            context.User = principal;
        //        }

        //        await next();
        //    });

        //    // Другие middleware...
        //}

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            // Получаем refresh_token из cookie
            var refreshToken = Request.Cookies["refresh_token"];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Нет refresh_token");

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // чтобы не было "запаса" времени
                };

                var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out SecurityToken validatedToken);

                var username = principal.Identity.Name;
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

                if (username == null || userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized("Недопустимый refresh_token");

                // Генерируем новые токены
                var newAccessToken = GenerateToken(username, userId, 1);      // 1 минута
                var newRefreshToken = GenerateToken(username, userId, 60);    // 60 минут

                // Устанавливаем куки
                Response.Cookies.Append("access_token", newAccessToken, new CookieOptions
                {
                    Path = "/",
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    Expires = DateTime.UtcNow.AddMinutes(1)
                });

                Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
                {
                    Path = "/",
                    HttpOnly = true,
                    SameSite = SameSiteMode.None,
                    Secure = true,
                    Expires = DateTime.UtcNow.AddMinutes(60)
                });

                User user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

                return Ok(new { 
                    message = "Токены обновлены",
                    userId = user?.Id, groupId = user?.GroupId, 
                    subgroupNumber = user?.SubgroupNumber 
                });
            }
            catch (SecurityTokenExpiredException)
            {
                return Unauthorized("Refresh token просрочен");
            }
            catch (Exception ex)
            {
                return Unauthorized($"Ошибка проверки refresh token: {ex.Message}");
            }
        }

    }

    public class CreateUserRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public int GroupId { get; set; }
        public int SubgroupNumber { get; set; }
    }

    public class LoginUser
    {
        public string Password { get; set; }
        public string Email { get; set; }
    }
}
