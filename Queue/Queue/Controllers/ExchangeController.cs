using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Queue.Models;
using System.Security.Claims;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Queue.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public ExchangeController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Notifications>>> GetNotifications()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = idClaim != null ? int.Parse(idClaim.Value) : (int?)null;

            if (userId == null) return Unauthorized();

            var exchanges = _context.Exchanges.Where(x => x.ToUserId == userId);

            var notificattions = exchanges.Select(x => new Notifications
            {
                Id = x.Id,
                UserName = _context.Users.FirstOrDefault(u => u.Id == x.FromUserId).UserName,
                Date = _context.Dates.FirstOrDefault(d => d.Id == x.DateId).Date,
                Number = _context.Queue.FirstOrDefault(q => q.DateId == x.DateId && q.UserId == x.FromUserId).NumberUser,
                SubjectName = _context.Subjects.FirstOrDefault(s => _context.Dates.FirstOrDefault(w => w.Id == x.DateId).SubjectId == s.Id).Name,
                CreatedAt=x.CreatedAt,
            });

            return Ok(notificattions);
        }

        


        [HttpPost]
        public async Task<IActionResult> AddToExchange([FromBody] ExchangeBody exchangeBody)
        {
            var exchange = new Exchange
            {
                FromUserId = exchangeBody.FromUserId,
                ToUserId = exchangeBody.ToUserId,
                CreatedAt = exchangeBody.CreatedAt,
                DateId = exchangeBody.DateId,
            };

            // Проверка на существование аналогичного обмена
            var existingExchange = await _context.Exchanges.FirstOrDefaultAsync(x =>
                exchange.CreatedAt.Substring(0, 10) == x.CreatedAt.Substring(0, 10)
                && x.FromUserId == exchange.FromUserId
                && x.ToUserId == exchange.ToUserId && x.DateId == exchange.DateId);
            //return Conflict(new { message = "Запись на текщий день существует уже существует." }); // Конфликт, запись уже существует
            if (existingExchange != null)
            {
                return Conflict(new { message = "Запись на текщий день существует уже существует." }); // Конфликт, запись уже существует
            }

            User FromUser = _context.Users.FirstOrDefault(x => x.Id == exchange.FromUserId);
            User ToUser = _context.Users.FirstOrDefault(x => x.Id == exchange.ToUserId);
            Dates date = _context.Dates.FirstOrDefault(x => x.Id == exchange.DateId);
            Subject subject = _context.Subjects.FirstOrDefault(x => x.Id == date.SubjectId);
            Queue.Models.Queue queue = _context.Queue.FirstOrDefault(x => x.DateId == date.Id && x.UserId == FromUser.Id);

            _emailService.SendEmailAsync(ToUser.Email, "Обмен", $"Пользователь {FromUser.UserName} " +
                $"({queue.NumberUser}-ое место) хочет поменяться с вами местами на предмет {subject.Name} ({date.Date})");

            await _context.Exchanges.AddAsync(exchange);
            
            try
            {
                await _context.SaveChangesAsync();
                return Ok(); // Успешная операция
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException;
                // Логируем информацию о внутреннем исключении
                string errorMessage = innerException != null ? innerException.Message : "Не удалось сохранить изменения.";
                return BadRequest(new { message = "Ошибка при добавлении записи в базу данных.", error = errorMessage });
            }
        }


        [HttpPatch("{exchangeId}")]
        public async Task<IActionResult> Confirm(int exchangeId)
        {
            var exchange = await _context.Exchanges.FirstOrDefaultAsync(x => x.Id == exchangeId);
            if (exchange == null)
            {
                return NotFound(); // Возвращаем 404, если обмен не найден
            }


            var queueFrom = await _context.Queue.FirstOrDefaultAsync(x => x.UserId == exchange.FromUserId && x.DateId == exchange.DateId);
            var queueTo = await _context.Queue.FirstOrDefaultAsync(x => x.UserId == exchange.ToUserId && x.DateId == exchange.DateId);

            var userFrom = await _context.Users.FirstOrDefaultAsync(x => x.Id == exchange.FromUserId);
            var userTo = await _context.Users.FirstOrDefaultAsync(x => x.Id == exchange.ToUserId);

            var date = await _context.Dates.FirstOrDefaultAsync(x => x.Id == exchange.DateId);

            var subject = await _context.Subjects.FirstOrDefaultAsync(x => x.Id == date.SubjectId);
            // Проверяем, что очереди найдены
            if (queueFrom == null || queueTo == null)
            {
                return NotFound(); // Возвращаем 404, если одна из очередей не найдена
            }

            // Меняем номера пользователей
            int tmpNumber = queueFrom.NumberUser;
            queueFrom.NumberUser = queueTo.NumberUser;
            queueTo.NumberUser = tmpNumber;

            _emailService.SendEmailAsync(userFrom.Email, "Обмен", $"Пользователь {userTo.UserName} " +
                $"согласился поменяться с вами местами на предмет {subject.Name} ({date.Date})");
            _context.Exchanges.Remove(exchange);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{exchangeId}")]
        public async Task<IActionResult> Cansel(int exchangeId)
        {
            var exchange = await _context.Exchanges.FirstOrDefaultAsync(x => x.Id == exchangeId);

            if (exchange == null)
            {
                return NotFound(); // Возвращаем 404, если обмен не найден
            }

            var queueFrom = await _context.Queue.FirstOrDefaultAsync(x => x.UserId == exchange.FromUserId && x.DateId == exchange.DateId);
            var queueTo = await _context.Queue.FirstOrDefaultAsync(x => x.UserId == exchange.ToUserId && x.DateId == exchange.DateId);

            var userFrom = await _context.Users.FirstOrDefaultAsync(x => x.Id == exchange.FromUserId);
            var userTo = await _context.Users.FirstOrDefaultAsync(x => x.Id == exchange.ToUserId);

            var date = await _context.Dates.FirstOrDefaultAsync(x => x.Id == exchange.DateId);

            var subject = await _context.Subjects.FirstOrDefaultAsync(x => x.Id == date.SubjectId);
            // Проверяем, что очереди найдены
            if (queueFrom == null || queueTo == null)
            {
                return NotFound(); // Возвращаем 404, если одна из очередей не найдена
            }

            _emailService.SendEmailAsync(userTo.Email, "Обмен", $"Пользователь {userFrom.UserName} " +
                $"отказался меняться с вами местами на предмет {subject.Name} ({date.Date})");


            _context.Exchanges.Remove(exchange);

            await _context.SaveChangesAsync();

            return Ok();
        }

    }

    public class ExchangeBody
    {
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string CreatedAt { get; set; }
        public int DateId { get; set; }
    }

    public class Notifications
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string SubjectName { get; set; }
        public string Date { get; set; }
        public string CreatedAt { get; set; }
        public int Number {  get; set; }
    }

    
}
