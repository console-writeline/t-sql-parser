using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TSqlParser.Core
{
    public static class Extensions
    {
        public static void AddIfNotExists(this ParserResults results, string tableName, SqlOperationType sqlOperationType, string alias)
        {
            results.TableParsingResults.AddIfNotExists(tableName, sqlOperationType, alias);
        }

        public static void AddIfNotExists(this ParserResults results, List<TableParsingResult> items)
        {
            foreach (var item in items)
                results.TableParsingResults.AddIfNotExists(item.TableName, item.OperationType, item.Alias);
        }

        public static void AddIfNotExists(this List<TableParsingResult> list, string tableName, SqlOperationType operationType, string alias)
        {
            if (!list.Any(x => x.TableName == tableName.ToLower() && x.OperationType == operationType))
            {
                list.Add(new TableParsingResult()
                {
                    TableName = tableName.ToLower(),
                    Alias = alias,
                    OperationType = operationType
                });
            }
        }

        public static void AddIfNotExists(this List<TableParsingResult> list, List<TableParsingResult> toAdd)
        {
            toAdd.ForEach(x => list.AddIfNotExists(x.TableName, x.OperationType, x.Alias));
        }

        public static void PrintTSqlStatementBlockToDebugConsole(this TSqlStatement statement)
        {
            StringBuilder sb = new StringBuilder();
            int skipFragments = statement.FirstTokenIndex - 1;
            skipFragments = skipFragments > 0 ? skipFragments : 0;
            int takeFragments = (statement.LastTokenIndex - statement.FirstTokenIndex) + 2;
            takeFragments = skipFragments + takeFragments > statement.ScriptTokenStream.Count ? statement.ScriptTokenStream.Count - skipFragments : takeFragments;
            statement.ScriptTokenStream
                .Skip(skipFragments)
                .Take(takeFragments).ToList().ForEach(x => sb.Append(x.Text));

            Debug.WriteLine($"============= {statement.GetType().FullName} =============");
            Debug.WriteLine(sb.ToString());
        }

        public static void PrintTablesToDebugConsole(this TSqlStatement statement, List<TableParsingResult> items)
        {            
            StringBuilder sb = new StringBuilder();
            Debug.WriteLine($"============= tables added in {statement.GetType().FullName} section =============");
            if (items.Count > 0)
                items.ForEach(x => sb.AppendLine($"Name: {x.TableName} Operation: {x.OperationType} Alias:{x.Alias}"));
            else
                sb.AppendLine("no tables parsed in this section");
            Debug.WriteLine(sb.ToString());
            //Debug.WriteLine($"============= {statement.GetType().FullName} =============");            
        }
    }
}
