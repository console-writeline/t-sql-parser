using Microsoft.SqlServer.TransactSql.ScriptDom;
using t = Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlParser.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlScriptAnalyzer
    {
        private SqlVersion _sqlVersion;
        private t.TSqlParser _sqlParser;

        public SqlScriptAnalyzer(SqlVersion sqlVersion = SqlVersion.Sql120)
        {
            _sqlVersion = sqlVersion;
        }
        
        private t.TSqlParser CreateSqlParser()
        {
            switch(_sqlVersion)
            {
                case SqlVersion.Sql80:
                    return new t.TSql80Parser(true);
                case SqlVersion.Sql90:
                    return new t.TSql90Parser(true);
                case SqlVersion.Sql100:
                    return new t.TSql100Parser(true);
                case SqlVersion.Sql110:
                    return new t.TSql110Parser(true);                
                case SqlVersion.Sql130:
                    return new t.TSql130Parser(true);
                case SqlVersion.Sql140:
                    return new t.TSql140Parser(true);
                default:
                case SqlVersion.Sql120:
                    return new t.TSql120Parser(true);
            }
        }

        /// <summary>
        /// Analyzes the SQL text asynchronously.
        /// </summary>
        /// <param name="inputScript">The input script.</param>
        /// <returns></returns>
        public async Task<ParserResults> AnalyzeSqlTextAsync(string inputScript)
        {
            ParserResults results = new ParserResults();

            _sqlParser = CreateSqlParser();
            Sql120ScriptGenerator _scriptGen;

            SqlScriptGeneratorOptions options = new SqlScriptGeneratorOptions();

            options.SqlVersion = _sqlVersion;
            options.KeywordCasing = KeywordCasing.Uppercase;

            _scriptGen = new Sql120ScriptGenerator(options);

            TSqlFragment fragment;
            IList<ParseError> errors;
            using (StringReader sr = new StringReader(inputScript))
            {
                fragment = _sqlParser.Parse(sr, out errors);
            }


            if (errors.Count > 0)
            {
                results.ParsingExceptionDetail = string.Join(",", errors.Select(x => x.Message));
            }
            else
            {
                TSqlScript sqlScript = fragment as TSqlScript;
                if (sqlScript != null)
                {
                    foreach (TSqlBatch batch in sqlScript.Batches)
                    {
                        foreach (TSqlStatement statement in batch.Statements)
                        {
                            AnalyzeTsqlStatement(results, statement);
                        }
                    }
                }
            }

            return await Task.FromResult(results);
        }

        private void AnalyzeCreateProcedureStatement(CreateProcedureStatement createProcedureStatement, ref ParserResults results)
        {
            //((BeginEndBlockStatement)((CreateProcedureStatement)statement).StatementList.Statements[0]).StatementList.Statements
            if (createProcedureStatement.StatementList.Statements.Count == 0)
                return;

            AnalyzeTsqlStatementList(createProcedureStatement.StatementList, ref results);
        }

        private void AnalyzeBeginEndBlockStatement(BeginEndBlockStatement beginEndBlockStatement, ref ParserResults results)
        {
            if (beginEndBlockStatement.StatementList.Statements.Count == 0)
                return;

            AnalyzeTsqlStatementList(beginEndBlockStatement.StatementList, ref results);
        }

        private void AnalyzeTryCatchStatement(TryCatchStatement tryCatchStatement, ref ParserResults results)
        {
            if (tryCatchStatement.TryStatements.Statements.Count == 0)
                return;

            AnalyzeTsqlStatementList(tryCatchStatement.TryStatements, ref results);
            AnalyzeTsqlStatementList(tryCatchStatement.CatchStatements, ref results);

        }

        private void AnalyzeTsqlStatementList(StatementList statementList, ref ParserResults results)
        {
            foreach (TSqlStatement statement in statementList.Statements)
            {                
                results = AnalyzeTsqlStatement(results, statement);
            }
        }

        private ParserResults AnalyzeTsqlStatement(ParserResults results, TSqlStatement statement)
        {
            if (statement is CreateProcedureStatement createProcedureStatement)
                AnalyzeCreateProcedureStatement(createProcedureStatement, ref results); // recursion path
            if (statement is BeginEndBlockStatement beginEndBlockStatement)
                AnalyzeBeginEndBlockStatement(beginEndBlockStatement, ref results); // recursion path
            else if (statement is UpdateStatement updateStatement)
                AnalyzeUpdateStatement(updateStatement, ref results);
            else if (statement is InsertStatement insertStatement)
                AnalyzeInsertStatement(insertStatement, ref results);
            else if (statement is SelectStatement selectStatement)
                AnalyzeSelectStatement(selectStatement, ref results);
            else if (statement is MergeStatement mergeStatement)
                AnalyzeMergeStatement(mergeStatement, ref results);
            else if (statement is DeleteStatement deleteStatement)
                AnalyzeDeleteStatement(deleteStatement, ref results);
            else if (statement is TryCatchStatement tryCatchStatement)
                AnalyzeTryCatchStatement(tryCatchStatement, ref results); // recursion path
            else if (statement is WhileStatement whileStatement)
                AnalyzeWhileStatement(whileStatement, ref results);
            else if (statement is ExecuteStatement executeStatement)
                AnalyzeExecuteStatement(executeStatement, ref results);
            else if (statement is IfStatement ifStatement)
            {

            }
            else
            {
                Debug.WriteLine($"found statement type (not analyzed): {statement.GetType().FullName}");
            }
            return results;
        }

        private void AnalyzeExecuteStatement(ExecuteStatement executeStatement, ref ParserResults results)
        {
            if(executeStatement.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference executableProcedureReference)
            {
                //executableProcedureReference.ProcedureReference.ProcedureReference.Name.Identifiers
                string schema = "";
                string procedureName = "";
                

                if (executableProcedureReference.ProcedureReference.ProcedureReference.Name.Identifiers.Count == 1)
                {
                    procedureName = executableProcedureReference.ProcedureReference.ProcedureReference.Name.Identifiers[0].Value;
                }
                else if (executableProcedureReference.ProcedureReference.ProcedureReference.Name.Identifiers.Count == 2)
                {
                    schema = executableProcedureReference.ProcedureReference.ProcedureReference.Name.Identifiers[0].Value;
                    procedureName = executableProcedureReference.ProcedureReference.ProcedureReference.Name.Identifiers[1].Value;
                }

                string pName = (string.IsNullOrWhiteSpace(schema) ? procedureName : $"{schema}.{procedureName}").ToLower();

                if (!results.ProceduresInvoked.Any(x => x == pName))
                    results.ProceduresInvoked.Add(pName);
            }
            else
            {
                Debug.WriteLine($"ExecutableEntity type (not analyzed): {executeStatement.ExecuteSpecification.ExecutableEntity.GetType().FullName}");
            }
        }

        private void AnalyzeWhileStatement(WhileStatement whileStatement, ref ParserResults results)
        {
            AnalyzeTsqlStatement(results, whileStatement.Statement);
        }

        private void AnalyzeUpdateStatement(UpdateStatement updateStatement, ref ParserResults results)
        {
            Dictionary<string, List<TableParsingResult>> cteModel = new Dictionary<string, List<TableParsingResult>>();
            if(updateStatement.WithCtesAndXmlNamespaces?.CommonTableExpressions.Count > 0)
            {
                foreach(CommonTableExpression cte in updateStatement.WithCtesAndXmlNamespaces.CommonTableExpressions)
                {
                    AnalyzeCommonTableExpression(cteModel, cte);
                }
            }

            if (updateStatement.UpdateSpecification.Target is NamedTableReference updateNamedTableReference)
            {
                string tableName = ExtraceTableNameFromNamedTableRefernce(updateNamedTableReference, out string alias);
                if (updateStatement.UpdateSpecification.FromClause == null)
                {
                    results.AddIfNotExists(tableName, SqlOperationType.UPDATE, alias);
                }
                else
                {
                    var items = ExtractTablesUsedInFromClause(updateStatement.UpdateSpecification.FromClause);
                    if (cteModel.Count > 0)
                    {
                        foreach (var cte in cteModel)
                        {
                            var item = items.Find(x => x.TableName == cte.Key);
                            if (item != null)
                            {
                                items.Remove(item);
                                foreach (var table in cte.Value)
                                    items.AddIfNotExists(table.TableName, table.OperationType, table.Alias);
                            }
                        }
                    }
                    results.AddIfNotExists(items);

                    var result = items.Find(x => x.Alias == tableName);
                    if (result != null)
                        results.AddIfNotExists(result.TableName, SqlOperationType.UPDATE, tableName);
                }

                foreach(var setClause in updateStatement.UpdateSpecification.SetClauses)
                {
                    if(setClause is AssignmentSetClause assignmentSetClause && assignmentSetClause.NewValue is ParenthesisExpression parenthesisExpression && parenthesisExpression.Expression is FunctionCall functionCall)
                    {
                        //foreach(var item in parenthesisExpression.Expression)
                        foreach(var item in functionCall.Parameters)
                        {
                            if (item is ScalarSubquery scalarSubquery && scalarSubquery.QueryExpression is QuerySpecification querySpecification)
                            {
                                var items = ExtractTablesUsedInFromClause(querySpecification.FromClause);
                                results.AddIfNotExists(items);
                            }

                        }
                    }
                }

                //((Microsoft.SqlServer.TransactSql.ScriptDom.ParenthesisExpression)((Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentSetClause)(new System.Collections.Generic.Mscorlib_CollectionDebugView<Microsoft.SqlServer.TransactSql.ScriptDom.SetClause>(updateStatement.UpdateSpecification.SetClauses).Items[0])).NewValue).Expression
            }            
        }

        public void ExtractTablesUsedInAssignmentClause()
        {
            //((Microsoft.SqlServer.TransactSql.ScriptDom.ParenthesisExpression)((Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentSetClause)(new System.Collections.Generic.Mscorlib_CollectionDebugView<Microsoft.SqlServer.TransactSql.ScriptDom.SetClause>(updateStatement.UpdateSpecification.SetClauses).Items[0])).NewValue).Expression
        }

        private void AnalyzeInsertStatement(InsertStatement insertStatement, ref ParserResults results)
        {
            Dictionary<string, List<TableParsingResult>> cteModel = new Dictionary<string, List<TableParsingResult>>();
            if (insertStatement.WithCtesAndXmlNamespaces?.CommonTableExpressions.Count > 0)
            {
                foreach (CommonTableExpression cte in insertStatement.WithCtesAndXmlNamespaces.CommonTableExpressions)
                {
                    AnalyzeCommonTableExpression(cteModel, cte);
                }
            }
            if (insertStatement.InsertSpecification.Target is NamedTableReference insertNamedTableReference)
            {
                string tableName = ExtraceTableNameFromNamedTableRefernce(insertNamedTableReference, out string alias);
                results.AddIfNotExists(tableName, SqlOperationType.INSERT, alias);
                AnalyzeInsertSourceStatement(insertStatement, results, cteModel);
            }
            else if(insertStatement.InsertSpecification.Target is VariableTableReference variableTableReference)
            {
                results.AddIfNotExists(variableTableReference.Variable.Name, SqlOperationType.INSERT, null);
                AnalyzeInsertSourceStatement(insertStatement, results, cteModel);
            }
        }

        private void AnalyzeInsertSourceStatement(InsertStatement insertStatement, ParserResults results, Dictionary<string, List<TableParsingResult>> cteModel)
        {
            if (insertStatement.InsertSpecification.InsertSource != null && insertStatement.InsertSpecification.InsertSource is SelectInsertSource selectInsertSource && selectInsertSource.Select is QuerySpecification selectQuerySpecification) //selectinsertsource
            {
                var items = ExtractTablesUsedInFromClause(selectQuerySpecification.FromClause);
                if (cteModel.Count > 0)
                {
                    foreach (var cte in cteModel)
                    {
                        var item = items.Find(x => x.TableName == cte.Key);
                        if (item != null)
                        {
                            items.Remove(item);
                            foreach (var table in cte.Value)
                                items.AddIfNotExists(table.TableName, table.OperationType, table.Alias);
                        }
                    }
                }
                results.AddIfNotExists(items);
            }
        }

        private void AnalyzeCommonTableExpression(Dictionary<string, List<TableParsingResult>> cteModel, CommonTableExpression cte)
        {
            string cteName = cte.ExpressionName.Value;
            if (cte.QueryExpression is QuerySpecification querySpecification)
            {
                var items = ExtractTablesUsedInFromClause(querySpecification.FromClause);

                // flatten out self refrencing ctes
                // ;with cte1 as (),
                // cte2 as (select from cte1 inner join users)
                foreach (var cte1 in cteModel) 
                {
                    var item = items.Find(x => x.TableName == cte1.Key);
                    if (item != null)
                    {
                        items.Remove(item);
                        foreach (var table in cte1.Value)
                            items.AddIfNotExists(table.TableName, table.OperationType, table.Alias);
                    }
                }
                cteModel.Add(cteName, items);
            }
        }

        private void AnalyzeSelectStatement(SelectStatement selectStatement, ref ParserResults results)
        {
            if(selectStatement.QueryExpression is QuerySpecification selectQuerySpecification)
            {
                var result = ExtractTablesUsedInFromClause(selectQuerySpecification.FromClause);
                results.AddIfNotExists(result);
            }
        }

        private void AnalyzeMergeStatement(MergeStatement mergeStatement, ref ParserResults results)
        {
            if (mergeStatement.MergeSpecification.Target is NamedTableReference mergeNamedTableReference)
            {
                string tableName = ExtraceTableNameFromNamedTableRefernce(mergeNamedTableReference, out string alias);
                results.AddIfNotExists(tableName, SqlOperationType.INSERT, alias);
                results.AddIfNotExists(tableName, SqlOperationType.UPDATE, alias);
                
                if (mergeStatement.MergeSpecification.TableReference is QueryDerivedTable mergeQueryDerivedTable && mergeQueryDerivedTable.QueryExpression is QuerySpecification mergeQuerySpecification)
                {
                    var result = ExtractTablesUsedInFromClause(mergeQuerySpecification.FromClause);
                    results.AddIfNotExists(result);
                }
            }
        }

        private void AnalyzeDeleteStatement(DeleteStatement deleteStatement, ref ParserResults results)
        {
            if (deleteStatement.DeleteSpecification.Target is NamedTableReference deleteNamedTableReference)
            {
                string tableName = ExtraceTableNameFromNamedTableRefernce(deleteNamedTableReference, out string alias);
                results.AddIfNotExists(tableName, SqlOperationType.DELETE, alias);
            }
        }

        private List<TableParsingResult> ExtractTablesUsedInFromClause(FromClause fromClause)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            if (fromClause == null)
                return result;
            
            foreach (TableReference tableReference in fromClause.TableReferences)
            {
                if(tableReference is JoinTableReference joinTableReference) //Qualified or UnqualifiedJoin
                {
                    //JoinTableReference joinTableReference = unqualifiedJoin;
                    
                    do
                    {
                        if (joinTableReference.SecondTableReference is NamedTableReference namedTableReference)
                        {
                            string tableName = ExtraceTableNameFromNamedTableRefernce(namedTableReference, out string alias);
                            result.AddIfNotExists(tableName, SqlOperationType.SELECT, alias);
                        }

                        if (joinTableReference.FirstTableReference is NamedTableReference namedTableReference2)
                        {
                            string tableName = ExtraceTableNameFromNamedTableRefernce(namedTableReference2, out string alias);
                            result.AddIfNotExists(tableName, SqlOperationType.SELECT, alias);
                        }

                        if(joinTableReference.SecondTableReference is QueryDerivedTable queryDerivedTable && queryDerivedTable.QueryExpression is QuerySpecification querySpecification)
                        {
                            string alias = null;
                            if (queryDerivedTable.Alias != null)
                                alias = queryDerivedTable.Alias.Value;

                            var items = ExtractTablesUsedInFromClause(querySpecification.FromClause); // recursion path
                            if (!string.IsNullOrWhiteSpace(alias))
                                items.ForEach(x => x.Alias = alias);

                            result.AddRange(items);
                        }

                        joinTableReference = joinTableReference.FirstTableReference as JoinTableReference;
                    }
                    while (joinTableReference != null);
                }
                else if(tableReference is NamedTableReference namedTableReference)
                {
                    string tableName = ExtraceTableNameFromNamedTableRefernce(namedTableReference, out string alias);
                    result.AddIfNotExists(tableName, SqlOperationType.SELECT, alias);
                }
                else if(tableReference is QueryDerivedTable queryDerivedTable && queryDerivedTable.QueryExpression is QuerySpecification querySpecification)
                {
                    string alias = null;
                    if(queryDerivedTable.Alias != null)
                        alias = queryDerivedTable.Alias.Value;

                    var items = ExtractTablesUsedInFromClause(querySpecification.FromClause); // recursion path
                    if (!string.IsNullOrWhiteSpace(alias))
                        items.ForEach(x => x.Alias = alias);

                    result.AddRange(items);
                }
                else
                {
                    Debug.WriteLine($"found TableReference type (not analyzed): {tableReference.GetType().FullName}");
                }
            }

            return result;
        }

        private string ExtraceTableNameFromNamedTableRefernce(NamedTableReference namedTableReference, out string alias)
        {
            string schema = "";
            string tableName = "";
            alias = null;

            if (namedTableReference.Alias != null)
                alias = namedTableReference.Alias.Value;

            if (namedTableReference.SchemaObject.Identifiers.Count == 1)
            {
                tableName = namedTableReference.SchemaObject.Identifiers[0].Value;
            }
            else if (namedTableReference.SchemaObject.Identifiers.Count == 2)
            {
                schema = namedTableReference.SchemaObject.Identifiers[0].Value;
                tableName = namedTableReference.SchemaObject.Identifiers[1].Value;
            }
            else
            {
                throw new NotImplementedException();
            }

            return string.IsNullOrWhiteSpace(schema) ? tableName : $"{schema}.{tableName}";
        }
    }
}
