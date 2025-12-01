using System.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

var app = builder.Build();

app.MapGet("/", () => Results.Ok("Music API is running."));

app.MapGet("/api/songs", async (IDbConnection db) =>
{
    using var cmd = db.CreateCommand();
    cmd.CommandText = "SELECT id, title, artist, is_national, lyrics, created_at FROM songs ORDER BY id";

    if (db is NpgsqlConnection npg && npg.State != System.Data.ConnectionState.Open)
        await npg.OpenAsync();

    var list = new List<Song>();

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new Song
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Artist = reader.GetString(2),
            IsNational = reader.GetBoolean(3),
            Lyrics = reader.GetString(4),
            CreatedAt = reader.GetDateTime(5)
        });
    }

    return Results.Ok(list);
});

app.MapPost("/api/songs", async (IDbConnection db, SongCreateDto dto) =>
{
    using var cmd = db.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO songs (title, artist, is_national, lyrics)
        VALUES (@title, @artist, @is_national, @lyrics)
        RETURNING id, created_at;
    ";

    var pTitle = cmd.CreateParameter();
    pTitle.ParameterName = "title";
    pTitle.Value = dto.Title;
    cmd.Parameters.Add(pTitle);

    var pArtist = cmd.CreateParameter();
    pArtist.ParameterName = "artist";
    pArtist.Value = dto.Artist;
    cmd.Parameters.Add(pArtist);

    var pNat = cmd.CreateParameter();
    pNat.ParameterName = "is_national";
    pNat.Value = dto.IsNational;
    cmd.Parameters.Add(pNat);

    var pLyrics = cmd.CreateParameter();
    pLyrics.ParameterName = "lyrics";
    pLyrics.Value = dto.Lyrics;
    cmd.Parameters.Add(pLyrics);

    if (db is NpgsqlConnection npg && npg.State != System.Data.ConnectionState.Open)
        await npg.OpenAsync();

    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var createdAt = reader.GetDateTime(1);

        var song = new Song
        {
            Id = id,
            Title = dto.Title,
            Artist = dto.Artist,
            IsNational = dto.IsNational,
            Lyrics = dto.Lyrics,
            CreatedAt = createdAt
        };

        return Results.Created($"/api/songs/{id}", song);
    }

    return Results.Problem("Erro ao inserir música.");
});

app.MapDelete("/api/songs/{id:int}", async (IDbConnection db, int id) =>
{
    using var cmd = db.CreateCommand();
    cmd.CommandText = "DELETE FROM songs WHERE id = @id";

    var pId = cmd.CreateParameter();
    pId.ParameterName = "id";
    pId.Value = id;
    cmd.Parameters.Add(pId);

    if (db is NpgsqlConnection npg && npg.State != System.Data.ConnectionState.Open)
        await npg.OpenAsync();

    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.NoContent() : Results.NotFound();
});

app.Run();

public class Song
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public bool IsNational { get; set; }
    public string Lyrics { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public record SongCreateDto(string Title, string Artist, bool IsNational, string Lyrics);
