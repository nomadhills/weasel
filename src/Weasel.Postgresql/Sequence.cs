using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using Weasel.Core;
using DbCommandBuilder = Weasel.Core.DbCommandBuilder;

namespace Weasel.Postgresql
{
    public class Sequence: ISchemaObject
    {
        public DbObjectName Identifier { get; }

        private readonly long? _startWith;

        public Sequence(string identifier)
        {
            Identifier = DbObjectName.Parse(PostgresqlProvider.Instance, identifier);
        }

        public Sequence(DbObjectName identifier)
        {
            Identifier = identifier;
        }

        public Sequence(DbObjectName identifier, long startWith)
        {
            Identifier = identifier;
            _startWith = startWith;
        }

        public DbObjectName? Owner { get; set; }
        public string OwnerColumn { get; set; } = null!;

        public IEnumerable<DbObjectName> AllNames()
        {
            yield return Identifier;
        }

        public void WriteCreateStatement(Migrator migrator, TextWriter writer)
        {
            writer.WriteLine($"CREATE SEQUENCE {Identifier}{(_startWith.HasValue ? $" START {_startWith.Value}" : string.Empty)};");

            if (Owner != null)
            {
                writer.WriteLine($"ALTER SEQUENCE {Identifier} OWNED BY {Owner}.{OwnerColumn};");
            }
        }

        public void WriteDropStatement(Migrator rules, TextWriter writer)
        {
            writer.WriteLine($"DROP SEQUENCE IF EXISTS {Identifier};");
        }

        public void ConfigureQueryCommand(DbCommandBuilder builder)
        {
            var schemaParam = builder.AddParameter(Identifier.Schema).ParameterName;
            var nameParam = builder.AddParameter(Identifier.Name).ParameterName;
            builder.Append($"select count(*) from information_schema.sequences where sequence_schema = :{schemaParam} and sequence_name = :{nameParam};");
        }

        public async Task<ISchemaObjectDelta> CreateDelta(DbDataReader reader)
        {
            if (!await reader.ReadAsync().ConfigureAwait(false) || (await reader.GetFieldValueAsync<int>(0).ConfigureAwait(false)) == 0)
            {
                return new SchemaObjectDelta(this, SchemaPatchDifference.Create);
            }

            return new SchemaObjectDelta(this, SchemaPatchDifference.None);
        }

        public async Task<ISchemaObjectDelta> FindDelta(NpgsqlConnection conn)
        {
            var builder = new DbCommandBuilder(conn);

            ConfigureQueryCommand(builder);

            using var reader = await builder.ExecuteReaderAsync(conn).ConfigureAwait(false);

            return await CreateDelta(reader).ConfigureAwait(false);
        }
    }
}
