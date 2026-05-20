    using System;
    using Xunit;
    using ForumParser.DbLibrary;

namespace ForumParser.Tests
{

    public class DbManagerTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly DbManager _db;

        public DbManagerTests()
        {
            // Создаем изолированную БД для каждого теста
            _testDbPath = $"test_{Guid.NewGuid()}.db";
            _db = new DbManager(_testDbPath);
        }

        public void Dispose()
        {
            // 1. Очищаем пул соединений SQLite, чтобы закрыть все скрытые дескрипторы файла
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            // 2. Добавляем небольшую паузу на освобождение ресурсов (опционально, для стабильности на медленных ПК)
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // 3. Теперь файл гарантированно разблокирован — удаляем его
            if (System.IO.File.Exists(_testDbPath))
            {
                try
                {
                    System.IO.File.Delete(_testDbPath);
                }
                catch (IOException)
                {
                    // Если операционная система всё ещё не успела освободить файл, 
                    // этот блок предотвратит падение всего тест-рана
                }
            }
        }

        [Fact]
        public void Add_ShouldInsertRecord_WhenDataIsValid()
        {
            var msg = new ForumMessage { Id = 101, Name = "User1", Message = "Hello" };
            _db.Add(msg);

            var retrieved = _db.GetById(101);
            Assert.NotNull(retrieved);
            Assert.Equal("User1", retrieved.Name);
        }

        [Fact]
        public void Add_ShouldThrowException_WhenNameExceedsLimit()
        {
            var longName = new string('A', 257);
            var msg = new ForumMessage { Id = 102, Name = longName, Message = "Test" };

            Assert.Throws<ArgumentException>(() => _db.Add(msg));
        }

        [Fact]
        public void GetById_ShouldReturnNull_WhenIdDoesNotExist()
        {
            var result = _db.GetById(99999);
            Assert.Null(result);
        }

        [Fact]
        public void GetById_ShouldReturnCorrectRecord()
        {
            _db.Add(new ForumMessage { Id = 5, Name = "Tester", Message = "Text" });
            var result = _db.GetById(5);
            Assert.Equal("Tester", result.Name);
        }

        // TODO: Добавьте по 2 теста для методов GetByName, Update и Delete
    }
}