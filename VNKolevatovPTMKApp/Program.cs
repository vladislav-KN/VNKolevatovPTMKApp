using System.Globalization;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using NpgsqlTypes;

namespace PTMKApp;

class Program
{
    static int tableWidth = 200;
    private static async Task Main(string[] args)
    {   
        //проверяем есть ли аргументы
        if (args is null || args.Length == 0)
        {
            return;
        }
        //выгружаем настройки
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
 
        IConfiguration config = builder.Build();
        //получаем подключение
        DataBaseSettings dbSettings = new DataBaseSettings()
        {
            ConnectionString = config["ConnectionStrings:DataBaseConnection"],
            DbName = config["ConnectionStrings:DataBaseName"]
        };
        //проверяем подключение
        NpgsqlConnection conn = new NpgsqlConnection(dbSettings.ConnectionString); 
        conn.Open(); 
        if (conn.State != System.Data.ConnectionState.Open)
        {
            Console.WriteLine($"Не удалось подключится к Базе данных.\nСтатус подключения к postgreSQL: {conn.State}");
            return;
        }
        conn.Close();
        
        //ждём проверки создания базы данных
        await InitDatabase(dbSettings);
        
        using var con = new NpgsqlConnection(dbSettings.ToString());
        //проверяем подключение
        try
        {
            con.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            if (int.TryParse(args[0], out int index))
            {
                string[] argumentsArray = new string[args.Length-1];
                for (int i = 1, j = 0; i < args.Length; i++, j++)
                {
                    argumentsArray[j] = args[i];
                }
                using var comand = new NpgsqlCommand();
                comand.Connection = con;
                Console.WriteLine(await CompleateTask(comand, con, index, argumentsArray));

            }
            con.Close();
        }
        
    }
 
    private static async Task<string> CompleateTask(NpgsqlCommand cmd, NpgsqlConnection conn, int taskNumber, string[]? arg)
    {
        switch (taskNumber)
        {
            case 1:
                //удаляем таблицу и создаём её заного
                cmd.CommandText = "DROP TABLE IF EXISTS People";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"CREATE TABLE People(
                fio VARCHAR(800) NOT NULL, gender BOOLEAN NOT NULL, birthday DATE NOT NULL)";
                cmd.ExecuteNonQuery();

                return "Таблица создана";
            case 2:
                //добавить введённое значение
                if (arg is null || arg.Length == 0 || arg.Length != 3)
                {
                    return "Введите данные";
                }
                if (string.IsNullOrEmpty(arg[0]))
                {
                    return "Введите ФИО";
                }
                if (string.IsNullOrEmpty(arg[1]) || !DateTime.TryParseExact(arg[1], "dd.MM.yyyy", CultureInfo.GetCultureInfo("ru-RU"),
                        DateTimeStyles.None, out DateTime date))
                {
                    return "Введите введите дату рождение в формате dd.MM.yyyy";
                }

                if (string.IsNullOrEmpty(arg[2]) || !(arg[2] == "М" || arg[2] == "Ж"))
                {
                    return "Введите введите дату рождение в формате dd.MM.yyyy";
                }

                Gender gender = arg[2] == "М" ? Gender.Male : Gender.Female;
               
                return AddData(cmd,new Entity(arg[0],date,gender));
                
            case 3:
                //отображение записей в консоли
                cmd.CommandText = @"SELECT DISTINCT fio, birthday, DATE_PART('year', AGE(NOW(),birthday)) as years_old,  
	CASE
    	WHEN gender THEN 'М.'
		ELSE 'Ж.'
	END
FROM people";
                using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                {
                    PrintLine();
                    PrintRow("ФИО", "Дата рождения", "Пол", "Возраст");
                    PrintLine();
                    while (rdr.Read())
                    {
                        PrintRow( rdr.GetString(0), rdr.GetDateTime(1).ToString("d"),
                            rdr.GetDouble(2).ToString(), rdr.GetString(3));
                    }
                    PrintLine();
                }

                return "Вывод информации окончен";
            case 4:
                //Создаем записи
                return SendData(conn);
            case 5:
                //удаляем таблицу и создаём её заного
                try
                {
                    cmd.CommandText = "DROP INDEX fio_gender_index";
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    //ничего не делать
                }

                return "Время выполнения: " + SearchQuery(cmd);
            case 6:
                //удаляем таблицу и создаём её заного
                try
                {
                    cmd.CommandText = "CREATE INDEX fio_gender_index ON people(fio,gender);";
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    cmd.CommandText = "DROP INDEX fio_gender_index";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE INDEX fio_gender_index ON people(fio,gender);";
                    cmd.ExecuteNonQuery();
                }
                return "Время выполнения: " + SearchQuery(cmd);

            default:
                return "Нет такой команды";
        }
        
    }
    private static void drawTextProgressBar(int progress, int total)
    {
        //draw empty progress bar
        Console.CursorLeft = 0;
        Console.Write("["); //start
        Console.CursorLeft = 32;
        Console.Write("]"); //end
        Console.CursorLeft = 1;
        float onechunk = 30.0f / total;
 
        //draw filled part
        int position = 1;
        for (int i = 0; i < onechunk * progress; i++)
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.CursorLeft = position++;
            Console.Write(" ");
        }

        //draw unfilled part
        for (int i = position; i <= 31; i++)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.CursorLeft = position++;
            Console.Write(" ");
        }

        //draw totals
        Console.CursorLeft = 35;
        Console.BackgroundColor = ConsoleColor.Black;
        Console.Write(progress.ToString() + " of " + total.ToString()+"    "); //blanks at the end remove any excess
    }

    private static string SearchQuery(NpgsqlCommand cmd)
    {
        cmd.CommandText = "SELECT * FROM people WHERE gender AND fio LIKE 'F'";
        var startTime = System.Diagnostics.Stopwatch.StartNew();
        cmd.ExecuteNonQuery();
        startTime.Stop();
        var resultTime = startTime.Elapsed;
 
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
            resultTime.Hours, 
            resultTime.Minutes, 
            resultTime.Seconds,
            resultTime.Milliseconds);
        return elapsedTime;
    }
    
    private static string SendData(NpgsqlConnection conn)
    {
        for (int i = 0; i < 1000100; i++)
        {
            using (var writer = conn.BeginBinaryImport(
                       "copy people from STDIN (FORMAT BINARY)"))
            {
                int banchSize = 20000;
                for (int j = 0; j < banchSize && i < 1000100; j++, i++)
                {
                    drawTextProgressBar(i, 1000100);
                    var record = new Entity();
                    if (i > 100)
                    {
                        record.Randomize();
                    }
                    else
                    {
                        record.Randomize("F");
                    }

                    writer.StartRow();
                    writer.Write(record.FIO, NpgsqlDbType.Varchar);
                    writer.Write(record.IsMale());
                    writer.Write(record.BirthDay.Value.Date, NpgsqlDbType.Date);
                    
                }

                writer.Complete();
            }
        }
        return "Все записи успешно сохранены";
    }
    
    
    
    /// <summary>
    /// Функция добавления данных в бд
    /// </summary>
    /// <param name="cmd">командная строка для отправки команд в postgre</param>
    /// <param name="data">данные для отправки</param>
    /// <returns></returns>
    private static string AddData(NpgsqlCommand cmd,  Entity data)
    {
        cmd.CommandText = @"INSERT INTO People(fio, gender, birthday) VALUES(@fio, @gender, @birthday)";
        cmd.Parameters.AddWithValue("@fio", data.FIO);
        cmd.Parameters.AddWithValue("@birthday", data.BirthDay);
        cmd.Parameters.AddWithValue("@gender", data.IsMale());
        cmd.Prepare();
        cmd.ExecuteNonQuery();
        return $"Данные ФИО: {data.FIO} Дата рождения: {data.BirthDay?.ToString("yyyy-MM-dd")} Пол:{data.IsMale()} добавлены";
    }

    /// <summary>
    /// Создает базу данных при её отсутсвии
    /// </summary>
    /// <param name="settings">Параметр хранящий в себе данные о подключении к базе данных</param>
    private static async Task InitDatabase(DataBaseSettings settings)
    {
       
        using var connection = new NpgsqlConnection(settings.ConnectionString);
        var sqlDbCount = $"SELECT COUNT(*) FROM pg_database WHERE datname = '{settings.DbName}';";
        var dbCount = await connection.ExecuteScalarAsync<int>(sqlDbCount);
        if (dbCount == 0)
        {
            var sql = $"CREATE DATABASE \"{settings.DbName}\"";
            await connection.ExecuteAsync(sql);
        }
    }
    static void PrintLine()
    {
        Console.WriteLine(new string('-', tableWidth));
    }

    static void PrintRow(params string[] columns)
    {
        int width = (tableWidth - columns.Length) / columns.Length;
        string row = "|";

        foreach (string column in columns)
        {
            row += AlignCentre(column, width) + "|";
        }

        Console.WriteLine(row);
    }

    static string AlignCentre(string text, int width)
    {
        text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

        if (string.IsNullOrEmpty(text))
        {
            return new string(' ', width);
        }
        else
        {
            return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
        }
    }
}
 
 