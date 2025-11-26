// Подключаем драйвер для работы с MySQL.
using MySql.Data.MySqlClient;
// Нужен для System (не используется в этом файле, но стандартная практика).
using System;
// Подключаем 'DataTable' (таблица в памяти) и другие классы ADO.NET.
using System.Data;

namespace ProEquipmentRental
{
    // 'static' класс-помощник (helper) для всех операций с базой данных.
    public static class Database
    {
        // Cтрока подключения: адрес сервера, порт, юзер, пароль, имя БД.
        private static string connectionString = "server=127.0.0.1;port=3306;user=root;password=;database=rental_app;";

        /// <summary>
        /// Получает список магазинов из БД с возможностью поиска.
        /// </summary>
        /// <param name="search">Строка для поиска (по названию или описанию). По умолчанию - пустая.</param>
        /// <returns>DataTable с результатами.</returns>
        public static DataTable GetShops(string search = "")
        {
            // Создаем пустую таблицу в памяти для хранения результатов.
            DataTable dt = new DataTable();

            // 'using' автоматически закроет соединение 'conn' при выходе из блока,
            // даже если произойдет ошибка.
            using (var conn = new MySqlConnection(connectionString))
            {
                // Открываем физическое соединение с MySQL.
                conn.Open();

                // Текст SQL-запроса. Ищем совпадения (LIKE) в полях 'name' и 'description'.
                // '@search' — это ПАРАМЕТР. Он ОБЯЗАТЕЛЕН для защиты от SQL-инъекций.
                string query = "SELECT * FROM shops WHERE name LIKE @search OR description LIKE @search";

                // Создаем команду, связывая SQL-запрос (query) с соединением (conn).
                // 'using' также автоматически очистит ресурсы команды 'cmd'.
                using (var cmd = new MySqlCommand(query, conn))
                {
                    // Задаем значение для параметра '@search' из SQL-запроса.
                    // Добавляем '%' (wildcard), чтобы SQL искал *частичное* совпадение.
                    // (Например, "стол" -> "%стол%")
                    cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                    // 'DataAdapter' — это "мост", который выполняет команду 
                    // и знает, как "залить" данные в C#-таблицу (DataTable).
                    using (var da = new MySqlDataAdapter(cmd))
                    {
                        // Выполняем запрос и заполняем (Fill) наш 'DataTable' (dt) данными.
                        da.Fill(dt);
                    }
                }
            } // 'conn' автоматически закрывается здесь

            // Возвращаем заполненную таблицу (или пустую, если ничего не найдено).
            return dt;
        }
    }
}