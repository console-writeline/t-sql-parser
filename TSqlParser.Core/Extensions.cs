using System.Collections.Generic;
using System.Linq;

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
    }
}
