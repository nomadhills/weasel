using System;
using System.IO;

namespace Weasel.Postgresql.Functions
{
    public class FunctionDelta : ISchemaObjectDelta
    {
        public Function Expected { get; set; }
        public Function Actual { get; set; }

        public FunctionDelta(Function expected, Function actual)
        {
            Expected = expected;
            Actual = actual;
            
            SchemaObject = expected;


            if (Expected.IsRemoved)
            {
                Difference = Actual == null ? SchemaPatchDifference.None : SchemaPatchDifference.Update;
            }
            else if (Actual == null)
            {
                Difference = SchemaPatchDifference.Create;
            }
            else if (!Expected.Body().CanonicizeSql().Equals(Actual.Body().CanonicizeSql(), StringComparison.OrdinalIgnoreCase))
            {
                Difference = SchemaPatchDifference.Update;
            }
            else
            {
                Difference = SchemaPatchDifference.None;
            }
        }

        public ISchemaObject SchemaObject { get; }
        public SchemaPatchDifference Difference { get; }
        public void WriteUpdate(DdlRules rules, TextWriter writer)
        {
            if (Expected.IsRemoved)
            {
                foreach (var drop in Actual.DropStatements())
                {
                    var sql = drop;
                    if (!sql.EndsWith("cascade", StringComparison.OrdinalIgnoreCase))
                    {
                        sql = sql.TrimEnd(';') + " cascade;";
                    }

                    writer.WriteLine(sql);
                }
            }
            else
            {
                foreach (var drop in Actual.DropStatements())
                {
                    var sql = drop;
                    if (!sql.EndsWith("cascade", StringComparison.OrdinalIgnoreCase))
                    {
                        sql = sql.TrimEnd(';') + " cascade;";
                    }

                    writer.WriteLine(sql);
                }  
                
                Expected.WriteCreateStatement(rules, writer);
            }
        }

        public void WriteRollback(DdlRules rules, TextWriter writer)
        {
            throw new NotImplementedException();
        }

        public void WriteRestorationOfPreviousState(DdlRules rules, TextWriter writer)
        {
            Actual.WriteCreateStatement(rules, writer);
        }


        public void WritePatch(SchemaPatch patch)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Expected.Identifier.QualifiedName + " Diff";
        }
    }
}
