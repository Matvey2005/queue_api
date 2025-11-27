using Microsoft.EntityFrameworkCore;
using Queue.Models;
using System.Text.Json;

namespace Queue.Repositories
{
    public class GroupRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public GroupRepository(ApplicationDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        // Метод для загрузки групп с iis.bsuir.by
        public async Task<List<Group>> FetchGroupsFromBsuirAsync()
        {
            var url = "https://iis.bsuir.by/api/v1/student-groups";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            // Десериализация ответа
            var groups = JsonSerializer.Deserialize<List<BsuirGroupDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (groups == null)
                return new List<Group>();

            // Конвертируем в формат нашей модели
            var entities = groups.Select(g => new Group
            {
                Name = g.Name
            }).ToList();

            return entities;
        }

        // Метод для записи в базу
        public async Task<int> SaveGroupsToDatabaseAsync()
        {
            var groups = await FetchGroupsFromBsuirAsync();

            // Проверяем, чтобы не было дублей
            var existingNames = await _context.Groups.Select(g => g.Name).ToListAsync();
            var newGroups = groups.Where(g => !existingNames.Contains(g.Name)).ToList();

            if (newGroups.Any())
            {
                await _context.Groups.AddRangeAsync(newGroups);
                return await _context.SaveChangesAsync();
            }

            return 0;
        }

        public async Task<List<Group>> GetAllGroupsAsync()
        {
            return await _context.Groups
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        private class BsuirGroupDto
        {
            public string Name { get; set; }
        }
    }
}
