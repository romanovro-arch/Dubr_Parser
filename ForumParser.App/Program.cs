using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ForumParser.DbLibrary;

class Program
{
    static async Task Main(string[] args)
    {
        // Ссылка на ветку обсуждения на Hacker News (открывается без каких-либо блокировок)
        string url = "https://news.ycombinator.com/item?id=352343";
        string dbPath = "forum_data.db";

        // Инициализируем нашу библиотеку БД из Проекта 1
        var db = new DbManager(dbPath);

        using var client = new HttpClient();
        // Добавляем базовый заголовок, чтобы сервер понимал, что это стандартный запрос
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        try
        {
            Console.WriteLine($"Подключение к источнику: {url}...");
            string html = await client.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // На Hacker News каждый комментарий лежит в строке таблицы <tr class="athing comtr">
            var commentNodes = doc.DocumentNode.SelectNodes("//tr[contains(@class, 'comtr')]");

            if (commentNodes == null)
            {
                Console.WriteLine("Ошибка: не удалось найти комментарии на странице. Проверьте URL.");
                return;
            }

            Console.WriteLine($"Найдено {commentNodes.Count} комментариев. Начинаем импорт в БД...");
            int savedCount = 0;

            foreach (var node in commentNodes)
            {
                try
                {
                    // 1. Извлекаем уникальный ID комментария (он записан прямо в атрибут id строки)
                    string rawId = node.GetAttributeValue("id", "0");
                    long id = long.Parse(rawId);

                    // 2. Извлекаем логин пользователя (тег <a> с классом "hnuser")
                    var userNode = node.SelectSingleNode(".//a[@class='hnuser']");
                    string name = userNode != null ? userNode.InnerText.Trim() : "[deleted]";

                    // 3. Извлекаем текст сообщения (тег <span> с классом "commtext")
                    var messageNode = node.SelectSingleNode(".//span[contains(@class, 'commtext')]");

                    // Если комментарий пустой (например, удален), пропускаем его
                    if (messageNode == null) continue;
                    string message = messageNode.InnerHtml.Trim();

                    // Валидация длин строк строго по вашему ТЗ
                    if (name.Length > 256) name = name.Substring(0, 256);
                    if (message.Length > 8096) message = message.Substring(0, 8096);

                    // Проверяем, нет ли уже такого ID в базе, чтобы не ловить дубликаты
                    if (db.GetById(id) == null)
                    {
                        var forumMsg = new ForumMessage { Id = id, Name = name, Message = message };
                        db.Add(forumMsg);
                        savedCount++;
                        Console.WriteLine($"Успешно добавлен пост #{id} от пользователя {name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Предупреждение: пропущен один комментарий из-за ошибки: {ex.Message}");
                }
            }

            Console.WriteLine("\n==============================================");
            Console.WriteLine($"Работа завершена успешно!");
            Console.WriteLine($"Добавлено новых записей в БД: {savedCount}");
            Console.WriteLine("==============================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка при работе парсера: {ex.Message}");
        }
    }
}