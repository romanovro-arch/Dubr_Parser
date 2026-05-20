using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace ForumParser.DbLibrary
{
    public class DbManager
    {
        private readonly string _connectionString;

        public DbManager(string dbPath)
        {
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Messages (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Message TEXT NOT NULL
            );";
            command.ExecuteNonQuery();
        }

        public ForumMessage GetById(long id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Message FROM Messages WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ForumMessage
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    Message = reader.GetString(2)
                };
            }
            return null;
        }

        public List<ForumMessage> GetByName(string name)
        {
            var result = new List<ForumMessage>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Message FROM Messages WHERE Name = $name";
            command.Parameters.AddWithValue("$name", name);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ForumMessage
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    Message = reader.GetString(2)
                });
            }
            return result;
        }

        public void Add(ForumMessage msg)
        {
            // Валидация длин строк по ТЗ
            if (msg.Name.Length > 256) throw new ArgumentException("Name too long");
            if (msg.Message.Length > 8096) throw new ArgumentException("Message too long");

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Messages (Id, Name, Message) VALUES ($id, $name, $message)";
            command.Parameters.AddWithValue("$id", msg.Id);
            command.Parameters.AddWithValue("$name", msg.Name);
            command.Parameters.AddWithValue("$message", msg.Message);
            command.ExecuteNonQuery();
        }

        public void Update(long id, string newMessage)
        {
            if (newMessage.Length > 8096) throw new ArgumentException("Message too long");

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Messages SET Message = $message WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$message", newMessage);
            command.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Messages WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }
    }
}
