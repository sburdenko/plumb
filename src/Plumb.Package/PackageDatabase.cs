using Microsoft.Data.Sqlite;
using Plumb.Core.Model;

namespace Plumb.Package;

/// <summary>
/// The <c>model.sqlite</c> file: an <c>elements</c> and a <c>properties</c> table. Row order is insertion order.
/// </summary>
internal static class PackageDatabase
{
    private const string Schema = """
        CREATE TABLE elements (
          global_id        TEXT PRIMARY KEY,
          ifc_type         TEXT NOT NULL,
          name             TEXT,
          parent_global_id TEXT,
          storey_global_id TEXT
        );

        CREATE TABLE properties (
          global_id  TEXT NOT NULL,
          pset       TEXT NOT NULL,
          name       TEXT NOT NULL,
          value      TEXT,
          unit       TEXT,
          FOREIGN KEY (global_id) REFERENCES elements(global_id)
        );

        CREATE INDEX ix_properties_global_id ON properties(global_id);
        """;

    /// <param name="progress">Receives 0..100 as rows are written.</param>
    public static void Write(string path, IfcModelData model, IProgress<int> progress, CancellationToken cancellationToken)
    {
        using var connection = Open(path, SqliteOpenMode.ReadWriteCreate);
        using var transaction = connection.BeginTransaction();
        var rows = new RowProgress(model.Elements.Count + model.Properties.Count, progress, cancellationToken);

        Execute(connection, transaction, Schema);
        InsertElements(connection, transaction, model.Elements, rows);
        InsertProperties(connection, transaction, model.Properties, rows);

        transaction.Commit();
    }

    public static (IReadOnlyList<ElementRecord> Elements, IReadOnlyList<PropertyRecord> Properties) Read(
        string path,
        CancellationToken cancellationToken)
    {
        using var connection = Open(path, SqliteOpenMode.ReadOnly);

        var elements = Query(
            connection,
            "SELECT global_id, ifc_type, name, parent_global_id, storey_global_id FROM elements ORDER BY rowid",
            row => new ElementRecord(row.GetString(0), row.GetString(1), Text(row, 2), Text(row, 3), Text(row, 4)),
            cancellationToken);

        var properties = Query(
            connection,
            "SELECT global_id, pset, name, value, unit FROM properties ORDER BY rowid",
            row => new PropertyRecord(row.GetString(0), row.GetString(1), row.GetString(2), Text(row, 3), Text(row, 4)),
            cancellationToken);

        return (elements, properties);
    }

    // Pooling keeps the file open after Dispose, which would block moving or deleting the package folder.
    private static SqliteConnection Open(string path, SqliteOpenMode mode)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = false,
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static void InsertElements(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<ElementRecord> elements,
        RowProgress rows)
    {
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO elements (global_id, ifc_type, name, parent_global_id, storey_global_id)
            VALUES ($id, $type, $name, $parent, $storey)
            """;
        var id = insert.Parameters.Add("$id", SqliteType.Text);
        var type = insert.Parameters.Add("$type", SqliteType.Text);
        var name = insert.Parameters.Add("$name", SqliteType.Text);
        var parent = insert.Parameters.Add("$parent", SqliteType.Text);
        var storey = insert.Parameters.Add("$storey", SqliteType.Text);

        foreach (var element in elements)
        {
            rows.Next();
            id.Value = element.GlobalId;
            type.Value = element.IfcType;
            name.Value = OrNull(element.Name);
            parent.Value = OrNull(element.ParentGlobalId);
            storey.Value = OrNull(element.StoreyGlobalId);
            insert.ExecuteNonQuery();
        }
    }

    private static void InsertProperties(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<PropertyRecord> properties,
        RowProgress rows)
    {
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO properties (global_id, pset, name, value, unit)
            VALUES ($id, $pset, $name, $value, $unit)
            """;
        var id = insert.Parameters.Add("$id", SqliteType.Text);
        var pset = insert.Parameters.Add("$pset", SqliteType.Text);
        var name = insert.Parameters.Add("$name", SqliteType.Text);
        var value = insert.Parameters.Add("$value", SqliteType.Text);
        var unit = insert.Parameters.Add("$unit", SqliteType.Text);

        foreach (var property in properties)
        {
            rows.Next();
            id.Value = property.GlobalId;
            pset.Value = property.Pset;
            name.Value = property.Name;
            value.Value = OrNull(property.Value);
            unit.Value = OrNull(property.Unit);
            insert.ExecuteNonQuery();
        }
    }

    private static List<T> Query<T>(
        SqliteConnection connection,
        string sql,
        Func<SqliteDataReader, T> map,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();

        var rows = new List<T>();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(map(reader));
        }

        return rows;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Counts written rows, reports each new whole percent and checks for cancellation before every row.
    /// </summary>
    private sealed class RowProgress(int total, IProgress<int> progress, CancellationToken cancellationToken)
    {
        private int _written;
        private int _lastPercent = -1;

        public void Next()
        {
            cancellationToken.ThrowIfCancellationRequested();
            var percent = total == 0 ? 100 : _written * 100 / total;
            if (percent != _lastPercent)
            {
                _lastPercent = percent;
                progress.Report(percent);
            }

            _written++;
        }
    }

    private static object OrNull(string? value) => value ?? (object)DBNull.Value;

    private static string? Text(SqliteDataReader row, int ordinal) => row.IsDBNull(ordinal) ? null : row.GetString(ordinal);
}
