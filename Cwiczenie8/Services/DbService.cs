namespace Cwiczenie8.Services;

using Cwiczenie8.DTOs;
using System.Data;
using Cwiczenie8.Exceptions;
using Microsoft.Data.SqlClient;

public interface IDbService
{
    Task<IEnumerable<TripDetailsGetDto>> GetAllTripsAsync();
    Task<IEnumerable<TripDetailsGetDto>> GetClientTripsAsync(int clientId);
    Task<int> AddClientAsync(ClientCreateDto dto);
    Task RegisterClientToTripAsync(int clientId, int tripId);
    Task UnregisterClientFromTripAsync(int clientId, int tripId);
}


public class DbService(IConfiguration configuration) : IDbService
{
    public async Task<IEnumerable<TripDetailsGetDto>> GetAllTripsAsync()
    {
        var trips = new List<TripDetailsGetDto>();

        await using var connection = await GetConnectionAsync();

        var sql = """
                  SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople, C.Name AS CountryName
                  FROM Trip T
                  JOIN Country_Trip CT ON T.IdTrip = CT.IdTrip
                  JOIN Country C ON CT.IdCountry = C.IdCountry
                  ORDER BY T.IdTrip;
                  """;

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        TripDetailsGetDto? currentTrip = null;
        int? lastTripId = null;

        while (await reader.ReadAsync())
        {
            var tripId = reader.GetInt32(0);

            if (lastTripId != tripId)
            {
                currentTrip = new TripDetailsGetDto
                {
                    TripId = tripId,
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<string>()
                };
                trips.Add(currentTrip);
                lastTripId = tripId;
            }

            currentTrip?.Countries.Add(reader.GetString(6));
        }

        return trips;
    }
    private async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    public async Task<IEnumerable<TripDetailsGetDto>> GetClientTripsAsync(int clientId)
    {
        var trips = new List<TripDetailsGetDto>();

        await using var connection = await GetConnectionAsync();

        // Check if client exists
        var checkClientSql = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
        await using (var checkCmd = new SqlCommand(checkClientSql, connection))
        {
            checkCmd.Parameters.AddWithValue("@IdClient", clientId);
            var exists = await checkCmd.ExecuteScalarAsync();
            if (exists == null)
                throw new NotFoundException($"Klient {clientId} nie istnieje.");
        }

        var sql = """
                  SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople, C.Name AS CountryName
                  FROM Client_Trip CT
                  JOIN Trip T ON CT.IdTrip = T.IdTrip
                  JOIN Country_Trip CTR ON T.IdTrip = CTR.IdTrip
                  JOIN Country C ON CTR.IdCountry = C.IdCountry
                  WHERE CT.IdClient = @IdClient
                  ORDER BY T.IdTrip;
                  """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@IdClient", clientId);

        var reader = await cmd.ExecuteReaderAsync();

        TripDetailsGetDto? currentTrip = null;
        int? lastTripId = null;

        while (await reader.ReadAsync())
        {
            var tripId = reader.GetInt32(0);

            if (lastTripId != tripId)
            {
                currentTrip = new TripDetailsGetDto
                {
                    TripId = tripId,
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<string>()
                };
                trips.Add(currentTrip);
                lastTripId = tripId;
            }

            currentTrip?.Countries.Add(reader.GetString(6));
        }

        return trips;
    }

    public async Task<int> AddClientAsync(ClientCreateDto dto)
    {
        await using var connection = await GetConnectionAsync();

        var sql = """
                  INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                  OUTPUT INSERTED.IdClient
                  VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
                  """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
        cmd.Parameters.AddWithValue("@LastName", dto.LastName);
        cmd.Parameters.AddWithValue("@Email", dto.Email);
        cmd.Parameters.AddWithValue("@Telephone", (object?)dto.Telephone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Pesel", (object?)dto.Pesel ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task RegisterClientToTripAsync(int clientId, int tripId)
    {
        await using var connection = await GetConnectionAsync();
        
        var checkSql = """
                       SELECT COUNT(*) 
                       FROM Client 
                       WHERE IdClient = @ClientId;
                       SELECT COUNT(*)
                       FROM Trip
                       WHERE IdTrip = @TripId;
                       """;

        await using var cmdCheck = new SqlCommand(checkSql, connection);
        cmdCheck.Parameters.AddWithValue("@ClientId", clientId);
        cmdCheck.Parameters.AddWithValue("@TripId", tripId);

        using var reader = await cmdCheck.ExecuteReaderAsync();
        await reader.ReadAsync();
        if (reader.GetInt32(0) == 0)
            throw new NotFoundException($"Klient {clientId} nie istnieje.");
        await reader.NextResultAsync();
        await reader.ReadAsync();
        if (reader.GetInt32(0) == 0)
            throw new NotFoundException($"Wycieczka {tripId} nie istnieje.");
        await reader.CloseAsync();
        
        var participantsSql = """
                              SELECT COUNT(*) 
                              FROM Client_Trip
                              WHERE IdTrip = @TripId;
                              SELECT MaxPeople 
                              FROM Trip 
                              WHERE IdTrip = @TripId;
                              """;

        await using var cmdMax = new SqlCommand(participantsSql, connection);
        cmdMax.Parameters.AddWithValue("@TripId", tripId);
        using var readerMax = await cmdMax.ExecuteReaderAsync();

        await readerMax.ReadAsync();
        var current = readerMax.GetInt32(0);
        await readerMax.NextResultAsync();
        await readerMax.ReadAsync();
        var max = readerMax.GetInt32(0);

        if (current >= max)
            throw new InvalidOperationException("Blad.");

        await readerMax.CloseAsync();
        
        var insertSql = """
                        INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                        VALUES (@ClientId, @TripId, @Now);
                        """;

        await using var cmdInsert = new SqlCommand(insertSql, connection);
        cmdInsert.Parameters.AddWithValue("@ClientId", clientId);
        cmdInsert.Parameters.AddWithValue("@TripId", tripId);
        cmdInsert.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("yyyyMMdd"));
        await cmdInsert.ExecuteNonQueryAsync();
    }

    public async Task UnregisterClientFromTripAsync(int clientId, int tripId)
    {
        await using var connection = await GetConnectionAsync();

        var checkSql = """
                       SELECT 1 
                       FROM Client_Trip 
                       WHERE IdClient = @ClientId AND IdTrip = @TripId;
                       """;

        await using var checkCmd = new SqlCommand(checkSql, connection);
        checkCmd.Parameters.AddWithValue("@ClientId", clientId);
        checkCmd.Parameters.AddWithValue("@TripId", tripId);

        var exists = await checkCmd.ExecuteScalarAsync();
        if (exists == null)
            throw new NotFoundException("nie znaleziono rejestracji.");

        var deleteSql = """
                        DELETE FROM Client_Trip 
                        WHERE IdClient = @ClientId AND IdTrip = @TripId;
                        """;

        await using var deleteCmd = new SqlCommand(deleteSql, connection);
        deleteCmd.Parameters.AddWithValue("@ClientId", clientId);
        deleteCmd.Parameters.AddWithValue("@TripId", tripId);

        await deleteCmd.ExecuteNonQueryAsync();
    }
}