using System.Data.SQLite;

namespace b2sync;

public class SyncStateTracker : ISyncStateTracker
{
    private readonly FileInfo _dbFile;

    public SyncStateTracker() : this(new FileInfo($"sync-{Guid.NewGuid().ToString()[..8]}.sqlite"))
    {

    }

    public SyncStateTracker(FileInfo databaseFile)
    {
        _dbFile = databaseFile ?? throw new ArgumentNullException(nameof(databaseFile));
    }

    public void Initialize()
    {
        if (!_dbFile.Exists)
        {
            CreateTables();
        }
    }

    public void SetComplete(SyncTask task)
    {
        using var connection = GetAndOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE syncTasks SET complete=TRUE WHERE fileName=@fileName;";
        command.Parameters.AddWithValue("@fileName", task.File.FullName);
        command.ExecuteNonQuery();
    }

    public void SetHash(SyncTask task)
    {
        using var connection = GetAndOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE syncTasks SET hash=@hash WHERE fileName=@fileName;";
        command.Parameters.AddWithValue("@fileName", task.File.FullName);
        command.Parameters.AddWithValue("@hash", task.Hash);
        command.ExecuteNonQuery();
    }

    public void SeedTasks(IReadOnlyList<SyncTask> tasks)
    {
        using var connection = GetAndOpenConnection();
        foreach (var task in tasks)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO syncTasks(fileName, hash, size, complete) VALUES (@fileName, @hash, @size, @complete);";
            command.Parameters.AddWithValue("@fileName", task.File.FullName);
            command.Parameters.AddWithValue("@hash", task.Hash);
            command.Parameters.AddWithValue("@size", task.File.Length);
            command.Parameters.AddWithValue("@complete", 0);
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<SyncTask> GetIncompleteTasks()
    {
        using var connection = GetAndOpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT fileName, hash FROM syncTasks WHERE complete=0;";
        using var reader = command.ExecuteReader();

        var items = new List<SyncTask>();
        while (reader.Read())
        {
            var fileName = reader.GetString(reader.GetOrdinal("fileName"));
            var hash = reader.GetString(reader.GetOrdinal("hash"));
            items.Add(new SyncTask { File = new FileInfo(fileName), Hash = hash });
        }

        return items;
    }

    private void CreateTables()
    {
        const string mainTable = """
                                 CREATE TABLE syncTasks(
                                    rowId INTEGER PRIMARY KEY,
                                    fileName TEXT UNIQUE,
                                    hash TEXT,
                                    size INTEGER,
                                    complete INTEGER
                                 );
                                 """;

        SQLiteConnection.CreateFile(_dbFile.FullName);
        using var dbConnection = GetAndOpenConnection();
        using var command = dbConnection.CreateCommand();
        command.CommandText = mainTable;
        command.ExecuteNonQuery();
    }

    private SQLiteConnection GetAndOpenConnection()
    {
        var dbConnection = new SQLiteConnection($"Data Source={_dbFile.FullName};Version=3;");
        dbConnection.Open();
        return dbConnection;
    }
}
