// ✅ Compatible with .NET 6, .NET 7, .NET 8

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("🚀 Generating SQL insert scripts...");

string basePath = AppDomain.CurrentDomain.BaseDirectory;

// Define paths for your JSON files
string countriesPath = Path.Combine(basePath, "Countries.json");
string statesPath = Path.Combine(basePath, "States.json");
string citiesPath = Path.Combine(basePath, "Cities.json");

// Generate SQL for each category
string countriesSql = GenerateCountriesSql(countriesPath);
string statesSql = GenerateStatesSql(statesPath);
string citiesSql = GenerateCitiesSql(citiesPath);

// Save output to SQL files
File.WriteAllText(Path.Combine(basePath, "Insert_Countries.sql"), countriesSql);
File.WriteAllText(Path.Combine(basePath, "Insert_States.sql"), statesSql);
File.WriteAllText(Path.Combine(basePath, "Insert_Cities.sql"), citiesSql);

Console.WriteLine("✅ SQL scripts generated successfully!");
Console.WriteLine($"📂 Files saved in: {basePath}");


// -------------------- Helper Methods --------------------

static string GenerateCountriesSql(string jsonPath)
{
    var json = File.ReadAllText(jsonPath);
    var countries = JsonSerializer.Deserialize<List<Country>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var sb = new StringBuilder("BEGIN TRY\nBEGIN TRANSACTION;\n");

    foreach (var c in countries)
    {
        sb.AppendLine($@"INSERT INTO HR.Country 
                                (CountryName, ShortName, CountryMobileCode, IsActive, CreatedBy, CreatedOn) 
                            VALUES 
                                ('{Escape(c.Name)}', '{Escape(c.Iso2)}', '{Escape(c.Phonecode)}', 1, 25, GETDATE());");
    }
    sb.AppendLine("COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\nROLLBACK TRANSACTION;\nPRINT '❌ Error inserting HR.Country records.';\nEND CATCH;");




    return sb.ToString();
}

static string GenerateStatesSql(string jsonPath)
{
    var states = JsonSerializer.Deserialize<List<State>>(File.ReadAllText(jsonPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var sb = new StringBuilder("BEGIN TRY\nBEGIN TRANSACTION;\n");

    foreach (var group in states.GroupBy(s => s.Country_Code))
    {
        sb.AppendLine($"\n-- ===================== {group.First().Country_Name} ({group.Key}) =====================\n");

        foreach (var s in group)
        {
            if (string.IsNullOrWhiteSpace(s.Country_Code))
            {
                sb.AppendLine($"-- ⚠️ Skipped state '{Escape(s.Name)}' (Missing CountryCode)");
                continue;
            }

            sb.AppendLine($"INSERT INTO HR.State (CountryCode, StateName, IsActive, CreatedBy, CreatedOn) " +
                          $"VALUES ((SELECT CountryCode FROM HR.Country WHERE ShortName='{Escape(s.Country_Code)}'), '{Escape(s.Name)}', 1, 25, GETDATE());");
        }
    }

    sb.AppendLine("COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\nROLLBACK TRANSACTION;\nPRINT '❌ Error inserting HR.State records.';\nEND CATCH;");
    return sb.ToString();
}



static string GenerateCitiesSql(string jsonPath)
{
    var cities = JsonSerializer.Deserialize<List<City>>(File.ReadAllText(jsonPath),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    var sb = new StringBuilder();
    int batchSize = 1000;
    int counter = 0;

    foreach (var c in cities)
    {
        if (string.IsNullOrWhiteSpace(c.CountryCode) || string.IsNullOrWhiteSpace(c.StateName))
        {
            sb.AppendLine($"-- ⚠️ Skipped city '{Escape(c.Name)}' (Missing CountryCode or StateName)");
            continue;
        }

        sb.AppendLine($@"INSERT INTO HR.City 
                            (CountryCode, StateCode, CityName, IsActive, CreatedBy, CreatedOn) 
                        VALUES 
                            ((SELECT TOP 1 CountryCode FROM HR.Country WHERE ShortName = '{Escape(c.CountryCode)}'),
                             (SELECT TOP 1 StateCode FROM HR.State 
                                WHERE StateName = '{Escape(c.StateName)}' 
                                  AND CountryCode = (SELECT TOP 1 CountryCode FROM HR.Country WHERE ShortName = '{Escape(c.CountryCode)}')),
                             N'{Escape(c.Name)}', 1, 25, GETDATE());");

        counter++;

        if (counter % batchSize == 0)
        {
            sb.AppendLine("GO\n");
        }
    }

    return sb.ToString();
}


#region Methods that prevent Duplication Errors
/*
static string GenerateCountriesSql(string jsonPath)
{
    var json = File.ReadAllText(jsonPath);
    var countries = JsonSerializer.Deserialize<List<Country>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var sb = new StringBuilder("BEGIN TRY\nBEGIN TRANSACTION;\n");

    foreach (var c in countries)
    {
        sb.AppendLine($@"
MERGE HR.Country AS target
USING (VALUES ('{Escape(c.Iso2)}', '{Escape(c.Name)}', '{Escape(c.Phonecode)}')) AS source (ShortName, CountryName, CountryMobileCode)
ON target.ShortName = source.ShortName
WHEN MATCHED THEN 
    UPDATE SET CountryName = source.CountryName, CountryMobileCode = source.CountryMobileCode, IsActive = 1, CreatedBy = 25, CreatedOn = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (ShortName, CountryName, CountryMobileCode, IsActive, CreatedBy, CreatedOn)
    VALUES (source.ShortName, source.CountryName, source.CountryMobileCode, 1, 25, GETDATE());
");
    }

    sb.AppendLine("COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\nROLLBACK TRANSACTION;\nPRINT '❌ Error inserting HR.Country records.';\nEND CATCH;");
    return sb.ToString();
}

static string GenerateStatesSql(string jsonPath)
{
    var states = JsonSerializer.Deserialize<List<State>>(File.ReadAllText(jsonPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var sb = new StringBuilder("BEGIN TRY\nBEGIN TRANSACTION;\n");

    foreach (var group in states.GroupBy(s => s.Country_Code))
    {
        sb.AppendLine($"\n-- ===================== {Escape(group.First().Country_Name)} ({Escape(group.Key)}) =====================\n");

        foreach (var s in group)
        {
            if (string.IsNullOrWhiteSpace(s.Country_Code))
            {
                sb.AppendLine($"-- ⚠️ Skipped state '{Escape(s.Name)}' (Missing CountryCode)");
                continue;
            }

            sb.AppendLine($@"
MERGE HR.State AS target
USING (VALUES ('{Escape(s.Name)}', (SELECT CountryCode FROM HR.Country WHERE ShortName = '{Escape(s.Country_Code)}'))) AS source (StateName, CountryCode)
ON target.StateName = source.StateName AND target.CountryCode = source.CountryCode
WHEN MATCHED THEN
    UPDATE SET IsActive = 1, CreatedBy = 25, CreatedOn = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (StateName, CountryCode, IsActive, CreatedBy, CreatedOn)
    VALUES (source.StateName, source.CountryCode, 1, 25, GETDATE());
");
        }
    }

    sb.AppendLine("COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\nROLLBACK TRANSACTION;\nPRINT '❌ Error inserting HR.State records.';\nEND CATCH;");
    return sb.ToString();
}

static string GenerateCitiesSql(string jsonPath)
{
    var cities = JsonSerializer.Deserialize<List<City>>(File.ReadAllText(jsonPath),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    var sb = new StringBuilder("BEGIN TRY\nBEGIN TRANSACTION;\n");
    int batchSize = 1000;
    int counter = 0;

    foreach (var c in cities)
    {
        if (string.IsNullOrWhiteSpace(c.CountryCode) || string.IsNullOrWhiteSpace(c.StateName))
        {
            sb.AppendLine($"-- ⚠️ Skipped city '{Escape(c.Name)}' (Missing CountryCode or StateName)");
            continue;
        }

        sb.AppendLine($@"
IF NOT EXISTS (
    SELECT 1 FROM HR.City 
    WHERE CityName = N'{Escape(c.Name)}' 
      AND StateCode = (SELECT TOP 1 StateCode FROM HR.State WHERE StateName = '{Escape(c.StateName)}' 
                       AND CountryCode = (SELECT TOP 1 CountryCode FROM HR.Country WHERE ShortName = '{Escape(c.CountryCode)}'))
)
BEGIN
    INSERT INTO HR.City 
        (CountryCode, StateCode, CityName, IsActive, CreatedBy, CreatedOn) 
    VALUES 
        (
            (SELECT TOP 1 CountryCode FROM HR.Country WHERE ShortName = '{Escape(c.CountryCode)}'),
            (SELECT TOP 1 StateCode FROM HR.State 
                WHERE StateName = '{Escape(c.StateName)}' 
                  AND CountryCode = (SELECT TOP 1 CountryCode FROM HR.Country WHERE ShortName = '{Escape(c.CountryCode)}')),
            N'{Escape(c.Name)}', 1, 25, GETDATE()
        );
END
");

        counter++;

        if (counter % batchSize == 0)
        {
            sb.AppendLine("COMMIT TRANSACTION;\nGO\nBEGIN TRANSACTION;");
        }
    }

    sb.AppendLine("COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\nROLLBACK TRANSACTION;\nPRINT '❌ Error inserting HR.City records.';\nEND CATCH;");
    return sb.ToString();
}

static string Escape(string? value)
{
    return value?.Replace("'", "''") ?? "";
}
*/
#endregion

static string Escape(string? value)
{
    return value?.Replace("'", "''") ?? "";
}



// -------------------- Models --------------------

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Iso2 { get; set; }
    public string Phonecode { get; set; }
}

public class State
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Iso2 { get; set; }
    public string Iso3166_2 { get; set; }
    public int Country_Id { get; set; }
    public string Country_Code { get; set; }
    public string Country_Name { get; set; }
}


public class City
{
    public int Id { get; set; }
    public string Name { get; set; }

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; }

    [JsonPropertyName("country_name")]
    public string CountryName { get; set; }

    [JsonPropertyName("state_name")]
    public string StateName { get; set; }

    [JsonPropertyName("state_code")]
    public string StateCode { get; set; }
}
