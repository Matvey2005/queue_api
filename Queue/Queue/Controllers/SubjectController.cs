using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Queue.Models;
using Queue.Repositories;
using System.Globalization;

namespace Queue.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubjectController : ControllerBase
    {
        private readonly GroupRepository _groupRepository;
        private readonly ScheduleRepository _scheduleRepository;
        private readonly ApplicationDbContext _context;

        public SubjectController(GroupRepository groupRepository, ScheduleRepository scheduleRepository, ApplicationDbContext context)
        {
            _groupRepository = groupRepository;
            _scheduleRepository = scheduleRepository;
            _context = context;
        }

        /// <summary>
        /// Загружает расписание для указанной группы и возвращает его.
        /// </summary>
        /// <param name="groupName">Название группы (например, "220501")</param>
        [HttpGet("schedule/{groupId}")]
        [Authorize]
        public async Task<IActionResult> GetScheduleForGroup(int groupId)
        {
            try
            {
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
                if (group == null)
                    return NotFound($"Группа '{groupId}' не найдена.");

                var dates = await _context.Dates
                    .Where(d => d.GroupId == groupId)
                    .ToListAsync();

                var now = DateTime.Now;

                var filtered = FilterAndSortDates(dates, now);

                return Ok(filtered);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при загрузке расписания: {ex.Message}");
            }
        }


        [HttpGet("subjects/{groupId}")]
        [Authorize]
        public async Task<IActionResult> GetSubjectsByGroupId(int groupId)
        {
            try
            {
                var subjects = await _context.Subjects
                    .Where(s => s.GroupId == groupId)
                    .ToListAsync();

                if (!subjects.Any())
                    return NotFound("Предметы не найдены");

                return Ok(subjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при загрузке предметов: {ex.Message}");
            }
        }

        private List<Dates> FilterAndSortDates(List<Dates> dates, DateTime now)
        {
            var parsed = new List<(Dates item, DateTime date, int index)>();

            foreach (var d in dates)
            {
                string raw = d.Date.Trim();
                int index = 0;

                if (raw.EndsWith("(1)"))
                {
                    index = 1;
                    raw = raw[..^3].Trim();
                }
                else if (raw.EndsWith("(2)"))
                {
                    index = 2;
                    raw = raw[..^3].Trim();
                }

                if (DateTime.TryParseExact(
                        raw,
                        "dd.MM.yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date))
                {
                    parsed.Add((d, date, index));
                }
            }

            return parsed
                .Where(x => x.date >= now.Date)
                .OrderBy(x => x.date)
                .ThenBy(x => x.index) 
                .Select(x =>
                {
                    x.item.Date = x.index > 0
                        ? $"{x.date:dd.MM.yyyy} ({x.index})"
                        : $"{x.date:dd.MM.yyyy}";
                    return x.item;
                })
                .ToList();
        }


    }
}
