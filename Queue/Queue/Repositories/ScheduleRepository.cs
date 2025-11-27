using Microsoft.EntityFrameworkCore;
using Queue.Models;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace Queue.Repositories
{
    public class ScheduleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ScheduleRepository(ApplicationDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        public async Task<int> LoadScheduleForGroupAsync(string groupNumber, int groupId)
        {
            var url = $"https://iis.bsuir.by/api/v1/schedule?studentGroup={groupNumber}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var scheduleData = JsonSerializer.Deserialize<BsuirScheduleResponse>(json, _jsonOptions);
            if (scheduleData == null)
                return 0;

            // Собираем все уроки
            var lessons = new List<BsuirLessonDto>();
            if (scheduleData.Schedules != null)
            {
                foreach (var kv in scheduleData.Schedules)
                    if (kv.Value != null)
                        lessons.AddRange(kv.Value);
            }

            // Фильтруем только ЛР и ПЗ
            lessons = lessons
                .Where(l => l.LessonTypeAbbrev == "ЛР" || l.LessonTypeAbbrev == "ПЗ")
                .ToList();

            if (lessons.Count == 0)
                return 0;

            var culture = CultureInfo.InvariantCulture;

            // Подготавливаем словарь (date, subject) -> список подгрупп
            var occurrences = new Dictionary<(string date, string subject), List<int>>(StringPairComparer.Instance);

            foreach (var lesson in lessons)
            {
                if (string.IsNullOrWhiteSpace(lesson.Subject))
                    continue;

                if (!DateTime.TryParseExact(lesson.StartLessonDate ?? string.Empty, "dd.MM.yyyy", culture, DateTimeStyles.None, out var start))
                    continue;

                if (!DateTime.TryParseExact(lesson.EndLessonDate ?? string.Empty, "dd.MM.yyyy", culture, DateTimeStyles.None, out var end))
                    end = start;

                int subgroup = lesson.NumSubgroup == 0 ? -1 : lesson.NumSubgroup;

                // Добавляем все даты с шагом 4 недели
                for (var d = start.Date; d <= end.Date; d = d.AddDays(28))
                {
                    var dateStr = d.ToString("dd.MM.yyyy", culture);
                    var key = (dateStr, lesson.Subject);

                    if (!occurrences.ContainsKey(key))
                        occurrences[key] = new List<int>();

                    if (!occurrences[key].Contains(subgroup))
                        occurrences[key].Add(subgroup);
                }
            }

            // Получаем список всех предметов (только для этой группы)
            var subjectNames = occurrences.Select(x => x.Key.subject).Distinct().ToList();
            var existingSubjects = await _context.Subjects
                .Where(s => subjectNames.Contains(s.Name) && s.GroupId == groupId)
                .ToListAsync();

            var subjectNameToId = existingSubjects.ToDictionary(s => s.Name, s => s.Id);

            // Добавляем новые предметы
            var newSubjects = subjectNames
                .Where(name => !subjectNameToId.ContainsKey(name))
                .Select(name => new Subject { Name = name, GroupId = groupId })
                .ToList();

            if (newSubjects.Count > 0)
            {
                await _context.Subjects.AddRangeAsync(newSubjects);
                await _context.SaveChangesAsync();

                var allSubjects = await _context.Subjects
                    .Where(s => subjectNames.Contains(s.Name) && s.GroupId == groupId)
                    .ToListAsync();

                subjectNameToId = allSubjects.ToDictionary(s => s.Name, s => s.Id);
            }

            // Обрабатываем даты
            int addedCount = 0;
            var existingDates = await _context.Dates
                .Where(d => d.GroupId == groupId)
                .ToListAsync();

            var existingDateSet = new HashSet<(int, string, int)>(
                existingDates.Select(d => (d.SubjectId, d.Date, d.ForSubgroup))
            );

            foreach (var kv in occurrences)
            {
                var (date, subjectName) = kv.Key;
                var subgroups = kv.Value;
                var subjectId = subjectNameToId[subjectName];

                if (subgroups.Count == 1)
                {
                    var subgroup = subgroups[0];
                    if (!existingDateSet.Contains((subjectId, date, subgroup)))
                    {
                        await _context.Dates.AddAsync(new Dates
                        {
                            SubjectId = subjectId,
                            Date = date,
                            ForSubgroup = subgroup,
                            GroupId = groupId
                        });
                        existingDateSet.Add((subjectId, date, subgroup));
                        addedCount++;
                    }
                }
                else
                {
                    for (int i = 0; i < subgroups.Count; i++)
                    {
                        var dateIndexed = $"{date} ({i + 1})";
                        var subgroup = subgroups[i];

                        if (!existingDateSet.Contains((subjectId, dateIndexed, subgroup)))
                        {
                            await _context.Dates.AddAsync(new Dates
                            {
                                SubjectId = subjectId,
                                Date = dateIndexed,
                                ForSubgroup = subgroup,
                                GroupId = groupId
                            });
                            existingDateSet.Add((subjectId, dateIndexed, subgroup));
                            addedCount++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return addedCount;
        }


        // --- DTO структуры ---
        private class BsuirScheduleResponse
        {
            public string StudentGroup { get; set; } = string.Empty;
            public Dictionary<string, List<BsuirLessonDto>> Schedules { get; set; } = new();
        }

        private class BsuirLessonDto
        {
            public string Subject { get; set; } = string.Empty;
            public string LessonTypeAbbrev { get; set; } = string.Empty;
            public string StartLessonTime { get; set; } = string.Empty;
            public string EndLessonTime { get; set; } = string.Empty;
            public int NumSubgroup { get; set; }
            public string DateLesson { get; set; } = string.Empty;
            public string StartLessonDate { get; set; } = string.Empty;
            public string EndLessonDate { get; set; } = string.Empty;
        }

        // --- Компаратор для пар (date, subject) ---
        private class StringPairComparer : IEqualityComparer<(string, string)>
        {
            public static readonly StringPairComparer Instance = new();

            public bool Equals((string, string) x, (string, string) y) =>
                string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string, string) obj) =>
                HashCode.Combine(obj.Item1.ToLowerInvariant(), obj.Item2.ToLowerInvariant());
        }
    }
}
