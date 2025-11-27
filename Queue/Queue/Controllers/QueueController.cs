using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Queue.Models;
using Queue.Repositories;
using Queue.Services;
using System.ComponentModel.DataAnnotations;

namespace Queue.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly DatesRepository _datesRepository;
        private readonly QueueRepository _queueRepository;
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;

        public QueueController(DatesRepository datesRepository, QueueRepository queueRepository, ApplicationDbContext context, EmailService emailService)
        {
            _datesRepository = datesRepository;
            _queueRepository = queueRepository;
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetQueueByDateId(
            [FromQuery] int groupId, [FromQuery] int subjectId, [FromQuery] int subgroupNumber, [FromQuery] int dateId)
        {
            try
            {
                var date = await _context.Dates
                    .FirstOrDefaultAsync(s => s.Id == dateId);
                   

                if (date == null)
                    return NotFound("Ошибка в выборе даты");

                var queueData = await _context.Queue
                    .Where(q => q.DateId == date.Id)
                    .Join(_context.Users,
                        q => q.UserId,
                        u => u.Id,
                    (q, u) => new QueueItem
                    {
                        Id = q.Id,
                        UserId = q.UserId,
                        UserName = u.UserName,
                        DateId = q.DateId,
                        NumberUser = q.NumberUser
                    })
                    .ToListAsync();


                return Ok(queueData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при загрузке предметов: {ex.Message}");
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddQueue([FromBody] AddQueueItem newQueue)
        {
            bool isAdd = await _queueRepository.Add(newQueue.UserId, newQueue.DateId, newQueue.NumberUser);

            //User user = await _context.Users.FirstOrDefaultAsync(x => x.Id == newQueue.UserId);
            //Dates date = await _context.Dates.FirstOrDefaultAsync(d => d.Id == newQueue.DateId);

            //Subject subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == date.SubjectId);
            if (!isAdd) return Conflict(new { message = "Это место уже занято" });
           // _emailService.SendEmailAsync(user?.Email, "Очередь", $"<p>Пользователь {user?.UserName} занял {newQueue.NumberUser}-ое место в очереди на предмет {subject?.Name} на {date.Date}"); ;

            return Ok(isAdd);
        }

        [HttpDelete("{queueId}")]
        [Authorize]
        public async Task<IActionResult> DeleteQueue(int queueId)
        {
            bool isDelete = await _queueRepository.Delete(queueId);

            return Ok(isDelete);
        }
    }


    class QueueItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string UserName { get; set; }

        public int DateId { get; set; }

        public int NumberUser { get; set; }
    }

    public class AddQueueItem
    {
        public int UserId { get; set; }
        public int DateId { get; set; }

        public int NumberUser { get; set; }
    }
}
