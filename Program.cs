using System.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["DefaultConnection"] 
    ?? throw new Exception("DefaultConnection env var missing");

builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

var app = builder.Build();

app.MapGet("/", () => "Music API is running.");

app.MapGet("/api/songs", async (IDbConnection db) =>
{
    using var cmd = db.CreateCommand();
    cmd.CommandText = "SELECT id,title,artist,is_national,lyrics,created_at FROM songs ORDER BY id";

    if (db is NpgsqlConnection c && c.State != ConnectionState.Open)
        await c.OpenAsync();

    var list = new List<Song>();
    using var r = await cmd.ExecuteReaderAsync();
    while(await r.ReadAsync()){
        list.Add(new Song{
            Id=r.GetInt32(0),
            Title=r.GetString(1),
            Artist=r.GetString(2),
            IsNational=r.GetBoolean(3),
            Lyrics=r.GetString(4),
            CreatedAt=r.GetDateTime(5)
        });
    }
    return Results.Ok(list);
});

app.MapPost("/api/songs", async (IDbConnection db, SongCreate dto) =>
{
    using var cmd = db.CreateCommand();
    cmd.CommandText=@"
    INSERT INTO songs (title,artist,is_national,lyrics)
    VALUES (@t,@a,@n,@l)
    RETURNING id,created_at;
    ";
    var p=cmd.CreateParameter(); p.ParameterName="t"; p.Value=dto.Title; cmd.Parameters.Add(p);
    p=cmd.CreateParameter(); p.ParameterName="a"; p.Value=dto.Artist; cmd.Parameters.Add(p);
    p=cmd.CreateParameter(); p.ParameterName="n"; p.Value=dto.IsNational; cmd.Parameters.Add(p);
    p=cmd.CreateParameter(); p.ParameterName="l"; p.Value=dto.Lyrics; cmd.Parameters.Add(p);

    if (db is NpgsqlConnection c && c.State != ConnectionState.Open)
        await c.OpenAsync();

    using var r=await cmd.ExecuteReaderAsync();
    if(await r.ReadAsync()){
        return Results.Created($"/api/songs/{r.GetInt32(0)}", new {
            Id=r.GetInt32(0),
            dto.Title,
            dto.Artist,
            dto.IsNational,
            dto.Lyrics,
            CreatedAt=r.GetDateTime(1)
        });
    }
    return Results.Problem("Insert failed");
});

app.MapDelete("/api/songs/{id:int}", async (IDbConnection db, int id)=>
{
    using var cmd=db.CreateCommand();
    cmd.CommandText="DELETE FROM songs WHERE id=@id";
    var p=cmd.CreateParameter(); p.ParameterName="id"; p.Value=id; cmd.Parameters.Add(p);

    if (db is NpgsqlConnection c && c.State != ConnectionState.Open)
        await c.OpenAsync();

    return await cmd.ExecuteNonQueryAsync()>0 ? Results.NoContent() : Results.NotFound();
});

app.Run();

public class Song{
    public int Id{get;set;}
    public string Title{get;set;}=""
    public string Artist{get;set;}=""
    public bool IsNational{get;set;}
    public string Lyrics{get;set;}=""
    public DateTime CreatedAt{get;set;}
}

public record SongCreate(string Title,string Artist,bool IsNational,string Lyrics);
