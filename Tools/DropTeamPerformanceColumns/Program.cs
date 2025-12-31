using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

string? envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".env");
if (!File.Exists(envPath)) envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");

string conn = Environment.GetEnvironmentVariable("ConnectionStrings__IdanSurestSecurityConnectionForPrediction");
if (string.IsNullOrEmpty(conn) && File.Exists(envPath))
{
    var lines = await File.ReadAllLinesAsync(envPath);
    foreach (var line in lines)
    {
        var l = line.Trim();
        if (l.StartsWith("#") || string.IsNullOrWhiteSpace(l)) continue;
        var idx = l.IndexOf('=');
        if (idx <= 0) continue;
        var key = l.Substring(0, idx).Trim();
        var val = l.Substring(idx + 1).Trim();
        if (key == "ConnectionStrings__IdanSurestSecurityConnectionForPrediction")
        {
            conn = val;
            break;
        }
    }
}

if (string.IsNullOrEmpty(conn))
{
    Console.Error.WriteLine("Connection string not found. Set env var ConnectionStrings__IdanSurestSecurityConnectionForPrediction or add to .env.");
    return;
}

var sql = "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team1Performance_AverageGoalsConceded\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team1Performance_AverageGoalsScored\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team1Performance_KeyPlayersStatus\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team1Performance_RecentLosses\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team1Performance_RecentWins\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team2Performance_AverageGoalsConceded\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team2Performance_AverageGoalsScored\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team2Performance_KeyPlayersStatus\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team2Performance_RecentLosses\"; \n"
    + "ALTER TABLE \"Predictions\" DROP COLUMN IF EXISTS \"Team2Performance_RecentWins\";";

try
{
    await using var connObj = new NpgsqlConnection(conn);
    await connObj.OpenAsync();
    await using var cmd = new NpgsqlCommand(sql, connObj);
    var affected = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Executed drop statements, result: {affected}");
    // Verify remaining columns
    var checkSql = "SELECT column_name FROM information_schema.columns WHERE table_name = 'Predictions' AND column_name LIKE 'Team%Performance%';";
    await using var checkCmd = new NpgsqlCommand(checkSql, connObj);
    await using var reader = await checkCmd.ExecuteReaderAsync();
    var any = false;
    while (await reader.ReadAsync())
    {
        any = true;
        Console.WriteLine("Remaining column: " + reader.GetString(0));
    }
    if (!any) Console.WriteLine("No TeamPerformance columns found in Predictions table.");
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error executing SQL: " + ex);
}
