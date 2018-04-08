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
            switch (_sqlVersion)
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

        private void AnalyzeTsqlStatementList(StatementList statementList, ParserResults results)
        {
            foreach (TSqlStatement statement in statementList.Statements)
            {
                results = AnalyzeTsqlStatement(results, statement);
            }
        }

        private ParserResults AnalyzeTsqlStatement(ParserResults results, TSqlStatement statement)
        {
            if (statement is null)
                return results;
            
            if (statement is CreateProcedureStatement createProcedureStatement)
                AnalyzeCreateProcedureStatement(createProcedureStatement, results);
            if (statement is BeginEndBlockStatement beginEndBlockStatement)
                AnalyzeBeginEndBlockStatement(beginEndBlockStatement, results);
            else if (statement is UpdateStatement updateStatement)
                AnalyzeUpdateStatement(updateStatement, results);
            else if (statement is InsertStatement insertStatement)
                AnalyzeInsertStatement(insertStatement, results);
            else if (statement is SelectStatement selectStatement)
                AnalyzeSelectStatement(selectStatement, results);
            else if (statement is MergeStatement mergeStatement)
                AnalyzeMergeStatement(mergeStatement, results);
            else if (statement is DeleteStatement deleteStatement)
                AnalyzeDeleteStatement(deleteStatement, results);
            else if (statement is TryCatchStatement tryCatchStatement)
                AnalyzeTryCatchStatement(tryCatchStatement, results);
            else if (statement is WhileStatement whileStatement)
                AnalyzeWhileStatement(whileStatement, results);
            else if (statement is ExecuteStatement executeStatement)
                AnalyzeExecuteStatement(executeStatement, results);
            else if (statement is IfStatement ifStatement)
                AnalyzeIfStatement(ifStatement, results);
            else if (statement is DeclareVariableStatement declareVariableStatement)
                AnalyzeDeclareVariableStatement(declareVariableStatement, results);
            else
            {
                //TODO
                Debug.WriteLine($"found statement type (not analyzed): {statement.GetType().FullName}");
            }
            return results;
        }

        #region Analyze TSqlStatement methods
        private void AnalyzeCreateProcedureStatement(CreateProcedureStatement createProcedureStatement, ParserResults results)
        {
            //((BeginEndBlockStatement)((CreateProcedureStatement)statement).StatementList.Statements[0]).StatementList.Statements
            if (createProcedureStatement.StatementList.Statements.Count == 0)
                return;

            AnalyzeTsqlStatementList(createProcedureStatement.StatementList, results);
        }

        private void AnalyzeBeginEndBlockStatement(BeginEndBlockStatement beginEndBlockStatement, ParserResults results)
        {
            if (beginEndBlockStatement.StatementList.Statements.Count == 0)
                return;

            AnalyzeTsqlStatementList(beginEndBlockStatement.StatementList, results);
        }

        private void AnalyzeDeclareVariableStatement(DeclareVariableStatement declareVariableStatement, ParserResults results)
        {
            foreach(DeclareVariableElement element in declareVariableStatement.Declarations)
            {
                if(element.Value is ScalarSubquery scalarSubquery)
                {
                    var items = ExtractTablesFromScalarSubQuery(scalarSubquery);
                    results.AddIfNotExists(items);
                }
            }
        }

        private void AnalyzeTryCatchStatement(TryCatchStatement tryCatchStatement, ParserResults results)
        {
            if (tryCatchStatement.TryStatements.Statements.Count == 0)
                return;

            AnalyzeTsqlStatementList(tryCatchStatement.TryStatements, results);
            AnalyzeTsqlStatementList(tryCatchStatement.CatchStatements, results);

        }

        private void AnalyzeExecuteStatement(ExecuteStatement executeStatement, ParserResults results)
        {
            executeStatement.PrintTSqlStatementBlockToDebugConsole();

            if (executeStatement.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference executableProcedureReference)
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
            else if(executeStatement.ExecuteSpecification.ExecutableEntity is ExecutableStringList executableStringList)
            {
                results.HasDynamicSQL = true;
                foreach(ValueExpression valueExpression in executableStringList.Strings)
                {
                    if (valueExpression is StringLiteral stringLiteral)
                        results.DynamicSQLStatements.Add(stringLiteral.Value);
                }
            }
            else
            {
                Debug.WriteLine($"ExecutableEntity type (not analyzed): {executeStatement.ExecuteSpecification.ExecutableEntity.GetType().FullName}");
            }
        }

        private void AnalyzeWhileStatement(WhileStatement whileStatement, ParserResults results)
        {
            AnalyzeTsqlStatement(results, whileStatement.Statement);
        }

        private void AnalyzeUpdateStatement(UpdateStatement updateStatement, ParserResults results)
        {
            updateStatement.PrintTSqlStatementBlockToDebugConsole();
            List<TableParsingResult> temp = new List<TableParsingResult>();

            Dictionary<string, List<TableParsingResult>> cteModel = new Dictionary<string, List<TableParsingResult>>();
            if (updateStatement.WithCtesAndXmlNamespaces?.CommonTableExpressions.Count > 0)
            {
                foreach (CommonTableExpression cte in updateStatement.WithCtesAndXmlNamespaces.CommonTableExpressions)
                {
                    AnalyzeCommonTableExpression(cteModel, cte);
                }
            }

            if (updateStatement.UpdateSpecification.Target is NamedTableReference updateNamedTableReference)
            {
                string tableName = ExtraceTableNameFromNamedTableRefernce(updateNamedTableReference, out string alias);
                if (updateStatement.UpdateSpecification.FromClause == null)
                {
                    temp.AddIfNotExists(tableName, SqlOperationType.UPDATE, alias);
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
                    temp.AddIfNotExists(items);

                    var result = items.Find(x => x.Alias == tableName);
                    if (result != null)
                        results.AddIfNotExists(result.TableName, SqlOperationType.UPDATE, tableName);
                }

                foreach (var setClause in updateStatement.UpdateSpecification.SetClauses)
                {
                    temp.AddIfNotExists(ExtractTablesUsedInAssignmentClause(setClause));
                }
            }

            updateStatement.PrintTablesToDebugConsole(temp);
            results.AddIfNotExists(temp);
        }

        private void AnalyzeInsertStatement(InsertStatement insertStatement, ParserResults results)
        {
            insertStatement.PrintTSqlStatementBlockToDebugConsole();
            List<TableParsingResult> temp = new List<TableParsingResult>();

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
                temp.AddIfNotExists(tableName, SqlOperationType.INSERT, alias);
                var items = ExtractTablesFromInsertStatement(insertStatement, cteModel);
                temp.AddIfNotExists(items);
            }
            else if (insertStatement.InsertSpecification.Target is VariableTableReference variableTableReference)
            {
                temp.AddIfNotExists(variableTableReference.Variable.Name, SqlOperationType.INSERT, null);
                var items = ExtractTablesFromInsertStatement(insertStatement, cteModel);
                temp.AddIfNotExists(items);
            }

            insertStatement.PrintTablesToDebugConsole(temp);
            results.AddIfNotExists(temp);
        }
        
        private void AnalyzeCommonTableExpression(Dictionary<string, List<TableParsingResult>> cteModel, CommonTableExpression cte)
        {
            string cteName = cte.ExpressionName.Value.ToLower();
            if (cte.QueryExpression is QuerySpecification querySpecification)
            {
                var items = ExtractTablesFromQuerySpecification(querySpecification); //ExtractTablesUsedInFromClause(querySpecification.FromClause);

                // flatten out self rencing ctes
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

        private void AnalyzeSelectStatement(SelectStatement selectStatement, ParserResults results)
        {
            selectStatement.PrintTSqlStatementBlockToDebugConsole();
            List<TableParsingResult> temp = new List<TableParsingResult>();
            if (selectStatement.QueryExpression is QuerySpecification selectQuerySpecification)
            {
                var result = ExtractTablesFromQuerySpecification(selectQuerySpecification); // ExtractTablesUsedInFromClause(selectQuerySpecification.FromClause);
                foreach (SelectElement selectElement in selectQuerySpecification.SelectElements)
                {
                    if (selectElement is SelectScalarExpression scalarExpression && scalarExpression.Expression is ScalarSubquery scalarSubquery && scalarSubquery.QueryExpression is QuerySpecification querySpecification)
                    {
                        var items = ExtractTablesFromQuerySpecification(querySpecification);
                        temp.AddIfNotExists(items);
                    }
                }
                temp.AddIfNotExists(result);
            }
            else if(selectStatement.QueryExpression is BinaryQueryExpression binaryQueryExpression)
            {
                var result = ExtractTablesFromBinaryQueryExpression(binaryQueryExpression);
                temp.AddIfNotExists(result);
            }

            selectStatement.PrintTablesToDebugConsole(temp);
            results.AddIfNotExists(temp);
        }

        private void AnalyzeMergeStatement(MergeStatement mergeStatement, ParserResults results)
        {
            mergeStatement.PrintTSqlStatementBlockToDebugConsole();
            List<TableParsingResult> temp = new List<TableParsingResult>();

            if (mergeStatement.MergeSpecification.Target is NamedTableReference mergeNamedTableReference)
            {
                string tableName = ExtraceTableNameFromNamedTableRefernce(mergeNamedTableReference, out string alias);
                temp.AddIfNotExists(tableName, SqlOperationType.INSERT, alias);
                temp.AddIfNotExists(tableName, SqlOperationType.UPDATE, alias);

                if (mergeStatement.MergeSpecification.TableReference is QueryDerivedTable mergeQueryDerivedTable && mergeQueryDerivedTable.QueryExpression is QuerySpecification querySpecification)
                {
                    var result = ExtractTablesFromQuerySpecification(querySpecification); //ExtractTablesUsedInFromClause(mergeQuerySpecification.FromClause);
                    temp.AddIfNotExists(result);
                }
            }

            mergeStatement.PrintTablesToDebugConsole(temp);
            results.AddIfNotExists(temp);
        }

        private void AnalyzeDeleteStatement(DeleteStatement deleteStatement, ParserResults results)
        {
            deleteStatement.PrintTSqlStatementBlockToDebugConsole();

            if (deleteStatement.DeleteSpecification.Target is NamedTableReference deleteNamedTableReference)
            {
                string tableName = ExtraceTableNameFromNamedTableRefernce(deleteNamedTableReference, out string alias);
                if (deleteStatement.DeleteSpecification.FromClause != null)
                {
                    var items = ExtractTablesUsedInFromClause(deleteStatement.DeleteSpecification.FromClause);
                    var match = items.Find(x => x.Alias == tableName);
                    if (match != null)
                    {
                        match.OperationType = SqlOperationType.DELETE;
                    }
                    results.AddIfNotExists(items);
                }
                else
                    results.AddIfNotExists(tableName, SqlOperationType.DELETE, alias);
            }
        }

        private void AnalyzeIfStatement(IfStatement ifStatement, ParserResults results)
        {
            ifStatement.PrintTSqlStatementBlockToDebugConsole();
            if (ifStatement.Predicate is ExistsPredicate existsPredicate)
            {
                var items = ExtractTablesFromScalarSubQuery(existsPredicate.Subquery);
                results.AddIfNotExists(items);
            }

            AnalyzeTsqlStatement(results, ifStatement.ThenStatement);
            AnalyzeTsqlStatement(results, ifStatement.ElseStatement);
        }
        #endregion

        #region Extract Table methods
        private List<TableParsingResult> ExtractTablesFromInsertStatement(InsertStatement insertStatement, Dictionary<string, List<TableParsingResult>> cteModel)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            if (insertStatement.InsertSpecification.InsertSource != null && insertStatement.InsertSpecification.InsertSource is SelectInsertSource selectInsertSource) //selectinsertsource
            {
                if (selectInsertSource.Select is QuerySpecification selectQuerySpecification)
                {
                    var items = ExtractTablesFromQuerySpecification(selectQuerySpecification); //ExtractTablesUsedInFromClause(selectQuerySpecification.FromClause);
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
                    result.AddIfNotExists(items);
                }
                else if(selectInsertSource.Select is BinaryQueryExpression binaryQueryExpression)
                {
                    var items = ExtractTablesFromBinaryQueryExpression(binaryQueryExpression);
                    result.AddIfNotExists(items);
                }                
            }

            return result;
        }

        private List<TableParsingResult> ExtractTablesFromScalarSubQuery(ScalarSubquery scalarSubquery)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            if(scalarSubquery.QueryExpression is QuerySpecification querySpecification)
            {
                var items = ExtractTablesFromQuerySpecification(querySpecification);
                result.AddIfNotExists(items);
            }
            return result;
        }

        private List<TableParsingResult> ExtractTablesFromQuerySpecification(QuerySpecification querySpecification)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            var items = ExtractTablesUsedInFromClause(querySpecification.FromClause);
            result.AddIfNotExists(items);
            foreach (var selectExpression in querySpecification.SelectElements)
            {
                if (selectExpression is SelectScalarExpression selectScalarExpression)
                {
                    if (selectScalarExpression.Expression is ScalarSubquery scalarSubQuery)
                    {
                        var items2 = ExtractTablesFromScalarSubQuery(scalarSubQuery); // recursion path
                        result.AddIfNotExists(items2);
                    }
                    else if (selectScalarExpression.Expression is SearchedCaseExpression searchedCaseExpression)
                    {
                        var items3 = ExtractTableNamesFromSearchedCaseExpression(searchedCaseExpression);
                        result.AddIfNotExists(items3);
                    }
                    else if (selectScalarExpression.Expression is FunctionCall functionCall)
                    {
                        foreach (ScalarExpression item in functionCall.Parameters)
                        {
                            if (item is ScalarSubquery scalarSubquery)
                            {
                                var items4 = ExtractTablesFromScalarSubQuery(scalarSubquery);
                                result.AddIfNotExists(items4);
                            }
                            else
                            {
                                Debug.WriteLine($"ScalarExpression {selectExpression.GetType().FullName} not analyzed");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"SelectScalarExpression {selectExpression.GetType().FullName} not analyzed");
                    }
                }
                else if(selectExpression is SelectSetVariable selectSetVariable)
                {
                    if(selectSetVariable.Expression is SearchedCaseExpression searchedCaseExpression)
                    {
                        var items5 = ExtractTableNamesFromSearchedCaseExpression(searchedCaseExpression);
                        result.AddIfNotExists(items5);
                    }
                }
                else 
                {
                    Debug.WriteLine($"SelectScalarExpression {selectExpression.GetType().FullName} not analyzed");
                }
            }
            return result;
        }

        private List<TableParsingResult> ExtractTableNamesFromSearchedCaseExpression(SearchedCaseExpression searchedCaseExpression)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            foreach (SearchedWhenClause whenClause in searchedCaseExpression.WhenClauses)
            {
                if (whenClause.ThenExpression is BinaryExpression binaryExpression)
                {
                    var items1 = ExtractTablesFromBinaryExpression(binaryExpression);
                    result.AddIfNotExists(items1);
                }
                else
                {
                    Debug.WriteLine($"SearchedWhenClause {whenClause.GetType().FullName} not analyzed");
                }
            }
            return result;
        }

        private List<TableParsingResult> ExtractTablesFromBinaryExpression(BinaryExpression binaryExpression)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            if (binaryExpression.FirstExpression is ScalarSubquery scalarSubQuery1)
            {
                var items1 = ExtractTablesFromScalarSubQuery(scalarSubQuery1);
                result.AddIfNotExists(items1);
            }

            if (binaryExpression.SecondExpression is ScalarSubquery scalarSubQuery2)
            {
                var items2 = ExtractTablesFromScalarSubQuery(scalarSubQuery2);
                result.AddIfNotExists(items2);
            }
            return result;
        }

        private List<TableParsingResult> ExtractTablesUsedInFromClause(FromClause fromClause)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            if (fromClause == null)
                return result;

            foreach (TableReference tableReference in fromClause.TableReferences)
            {
                if (tableReference is JoinTableReference joinTableReference) // can be of type Qualified or UnqualifiedJoin but we care only about the abstract type
                {
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

                        if (joinTableReference.SecondTableReference is QueryDerivedTable queryDerivedTable && queryDerivedTable.QueryExpression is QuerySpecification querySpecification)
                        {
                            string alias = null;
                            if (queryDerivedTable.Alias != null)
                                alias = queryDerivedTable.Alias.Value;

                            var items = ExtractTablesFromQuerySpecification(querySpecification); //ExtractTablesUsedInFromClause(querySpecification.FromClause); // recursion path
                            if (!string.IsNullOrWhiteSpace(alias))
                                items.ForEach(x => x.Alias = alias);

                            result.AddIfNotExists(items);
                        }

                        joinTableReference = joinTableReference.FirstTableReference as JoinTableReference;
                    }
                    while (joinTableReference != null);
                }
                else if (tableReference is NamedTableReference namedTableReference)
                {
                    string tableName = ExtraceTableNameFromNamedTableRefernce(namedTableReference, out string alias);
                    result.AddIfNotExists(tableName, SqlOperationType.SELECT, alias);
                }
                else if (tableReference is QueryDerivedTable queryDerivedTable && queryDerivedTable.QueryExpression is QuerySpecification querySpecification)
                {
                    string alias = null;
                    if (queryDerivedTable.Alias != null)
                        alias = queryDerivedTable.Alias.Value;

                    var items = ExtractTablesFromQuerySpecification(querySpecification); //ExtractTablesUsedInFromClause(querySpecification.FromClause); // recursion path
                    if (!string.IsNullOrWhiteSpace(alias))
                        items.ForEach(x => x.Alias = alias);

                    result.AddIfNotExists(items);
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
                //throw new NotImplementedException();
            }

            return string.IsNullOrWhiteSpace(schema) ? tableName : $"{schema}.{tableName}";
        }

        private List<TableParsingResult> ExtractTablesUsedInAssignmentClause(SetClause setClause)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            if (setClause is AssignmentSetClause assignmentSetClause && assignmentSetClause.NewValue is ParenthesisExpression parenthesisExpression && parenthesisExpression.Expression is FunctionCall functionCall)
            {
                //foreach(var item in parenthesisExpression.Expression)
                foreach (var item in functionCall.Parameters)
                {
                    if (item is ScalarSubquery scalarSubquery && scalarSubquery.QueryExpression is QuerySpecification querySpecification)
                    {
                        var items = ExtractTablesFromQuerySpecification(querySpecification); //ExtractTablesUsedInFromClause(querySpecification.FromClause);
                        result.AddIfNotExists(items);
                    }
                }
            }
            return result;
        }

        private List<TableParsingResult> ExtractTablesFromBinaryQueryExpression(BinaryQueryExpression binaryQueryExpression)
        {
            List<TableParsingResult> result = new List<TableParsingResult>();
            if(binaryQueryExpression.FirstQueryExpression is BinaryQueryExpression binaryQueryExpression2)
            {
                var items = ExtractTablesFromBinaryQueryExpression(binaryQueryExpression2);
                result.AddIfNotExists(items);
            }
            else if (binaryQueryExpression.FirstQueryExpression is QuerySpecification querySpecification1)
            {
                var items = ExtractTablesFromQuerySpecification(querySpecification1);
                result.AddIfNotExists(items);
            }
            if (binaryQueryExpression.SecondQueryExpression is QuerySpecification querySpecification2)
            {
                var items = ExtractTablesFromQuerySpecification(querySpecification2);
                result.AddIfNotExists(items);
            }
            return result;
        }
        #endregion

        
    }
}
