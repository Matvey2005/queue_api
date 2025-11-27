using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Queue.Repositories;
 // <-- если твой DbContext в этом неймспейсе

namespace Queue.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly GroupRepository _repository;
        private readonly ApplicationDbContext _context; // <-- добавляем контекст

        public GroupController(GroupRepository repository, ApplicationDbContext context)
        {
            _repository = repository;
            _context = context; // <-- сохраняем его
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllGroups()
        {
            var groups = await _repository.GetAllGroupsAsync();
            return Ok(groups);
        }

        // GET: api/group/sync
        [HttpGet("sync")]
        public async Task<IActionResult> SyncGroups()
        {
            // Сохраняем новые группы в базу
            var added = await _repository.SaveGroupsToDatabaseAsync();

            // Загружаем все группы после обновления
            var allGroups = await _context.Groups
                .Select(g => new
                {
                    g.Id,
                    g.Name
                })
                .OrderBy(g => g.Name)
                .ToListAsync();

            // Возвращаем и сообщение, и список групп
            return Ok(new
            {
                message = $"Добавлено {added} новых групп.",
                total = allGroups.Count,
                groups = allGroups
            });
        }

        
    }
}
