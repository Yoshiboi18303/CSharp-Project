using Npgsql;

namespace CSharp_Project.Modules;

public sealed class SqlHandler
{
    #region fields

    public readonly NpgsqlConnection? Connection;

    #endregion
    
    /// <summary>
    /// Initializes an SqlHandler using an NpgsqlConnectionStringBuilder.
    /// </summary>
    /// <param name="connectionStringBuilder">The connection string builder to build</param>
    public SqlHandler(NpgsqlConnectionStringBuilder connectionStringBuilder)
    {
        string connectionString = connectionStringBuilder.ConnectionString;
        Connection = new NpgsqlConnection(connectionString);
        Connection.Open();
    }

    /// <summary>
    /// Initializes an SqlHandler using a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to use</param>
    public SqlHandler(string connectionString)
    {
        Connection = new NpgsqlConnection(connectionString);
        Connection.Open();
    }
    
    /// <summary>
    ///  Executes your command and returns the reader with all the data.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <returns>An NpgsqlDataReader containing all the data.</returns>
    public NpgsqlDataReader DataReaderCommand(NpgsqlCommand command)
    {
        NpgsqlDataReader reader = command.ExecuteReader();
        return reader;
    }

    /// <summary>
    /// Executes your command and returns how many rows were affected.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>An int containing how many rows were affected.</returns>
    public int NonQueryCommand(NpgsqlCommand command)
    {
        int numberOfRowsAffected = command.ExecuteNonQuery();
        return numberOfRowsAffected;
    }

    private void ThrowNonExistentException()
    {
        throw new Exception("Connection doesn't exist.");
    }

    public void OpenConnection()
    {
        if (Connection == null) ThrowNonExistentException();
        Connection!.Open();
    }

    public void CloseConnection()
    {
        if (Connection == null) ThrowNonExistentException();
        Connection!.Close();
    }
}
