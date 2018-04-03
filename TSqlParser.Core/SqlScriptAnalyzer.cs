using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
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
        public SqlScriptAnalyzer()
        {

        }


        /// <summary>
        /// Analyzes the SQL text asynchronously.
        /// </summary>
        /// <param name="inputScript">The input script.</param>
        /// <returns></returns>
        public async Task<ParserResults> AnalyzeSqlTextAsync(string inputScript)
        {
            ParserResults results = new ParserResults();

            TSql120Parser _parser = new TSql120Parser(true);
            Sql120ScriptGenerator _scriptGen;

            SqlScriptGeneratorOptions options = new SqlScriptGeneratorOptions();

            options.SqlVersion = SqlVersion.Sql120;
            options.KeywordCasing = KeywordCasing.Uppercase;

            _scriptGen = new Sql120ScriptGenerator(options);

            TSqlFragment fragment;
            IList<ParseError> errors;
            using (StringReader sr = new StringReader(inputScript))
            {
                fragment = _parser.Parse(sr, out errors);
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
                            if (statement is CreateProcedureStatement createProcedureStatement)
                                AnalyzeCreateProcedureStatement(createProcedureStatement, ref results);
                            else if (statement is BeginEndBlockStatement beginEndBlockStatement)
                                AnalyzeBeginEndBlockStatement(beginEndBlockStatement, ref results);
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

            foreach (TSqlStatement statement in createProcedureStatement.StatementList.Statements)
            {
                if (statement is BeginEndBlockStatement beginEndBlockStatement)
                    AnalyzeBeginEndBlockStatement(beginEndBlockStatement, ref results);
            }

        }

        private void AnalyzeBeginEndBlockStatement(BeginEndBlockStatement beginEndBlockStatement, ref ParserResults results)
        {
            if (beginEndBlockStatement.StatementList.Statements.Count == 0)
                return;

            foreach (TSqlStatement statement in beginEndBlockStatement.StatementList.Statements)
            {
                if (statement is UpdateStatement updateStatement)
                    AnalyzeUpdateStatement(updateStatement, ref results);
                else if (statement is InsertStatement insertStatement)
                    AnalyzeInsertStatement(insertStatement, ref results);
                else if (statement is SelectStatement selectStatement)
                    AnalyzeSelectStatement(selectStatement, ref results);
                else if (statement is MergeStatement mergeStatement)
                    AnalyzeMergeStatement(mergeStatement, ref results);
                else if (statement is DeleteStatement deleteStatement)
                    AnalyzeDeleteStatement(deleteStatement, ref results);
            }
        }

        private void AnalyzeUpdateStatement(UpdateStatement updateStatement, ref ParserResults results)
        {
            if (updateStatement.UpdateSpecification.Target is NamedTableReference updateNamedTableReference)
            {
                foreach (Identifier tableNameIdentifier in updateNamedTableReference.SchemaObject.Identifiers)
                {
                    string tableName = tableNameIdentifier.Value;

                    if (!results.TableParsingResults.Any(x => x.TableName == tableName && x.OperationType == SqlOperationType.UPDATE))
                    {
                        results.TableParsingResults.Add(new TableParsingResult()
                        {
                            TableName = tableName,
                            OperationType = SqlOperationType.UPDATE
                        }); // table names to be updated
                    }
                }
            }
        }

        private void AnalyzeInsertStatement(InsertStatement insertStatement, ref ParserResults results)
        {
            if (insertStatement.InsertSpecification.Target is NamedTableReference insertNamedTableReference)
            {
                foreach (Identifier tableNameIdentifier in insertNamedTableReference.SchemaObject.Identifiers)
                {
                    string tableName = tableNameIdentifier.Value;
                    if (!results.TableParsingResults.Any(x => x.TableName == tableName && x.OperationType == SqlOperationType.UPDATE))
                    {
                        results.TableParsingResults.Add(new TableParsingResult()
                        {
                            TableName = tableName,
                            OperationType = SqlOperationType.INSERT
                        });
                    }
                }
            }
        }

        private void AnalyzeSelectStatement(SelectStatement selectStatement, ref ParserResults results)
        {

        }

        private void AnalyzeMergeStatement(MergeStatement mergeStatement, ref ParserResults results)
        {

        }

        private void AnalyzeDeleteStatement(DeleteStatement deleteStatement, ref ParserResults results)
        {

        }

        private void ExtractTablesUsedInFromClause(List<string> selectTables, FromClause fromClause)
        {
            foreach (QualifiedJoin tableJoinReference in fromClause.TableReferences)
            {
                QualifiedJoin joinReference = tableJoinReference;
                do
                {
                    if (joinReference.SecondTableReference is NamedTableReference namedTableReference)
                    {
                        foreach (Identifier tableNameIdentifier in namedTableReference.SchemaObject.Identifiers)
                        {
                            if (!selectTables.Exists(x => x == tableNameIdentifier.Value.Trim().ToLower()))
                            {
                                selectTables.Add(tableNameIdentifier.Value.Trim().ToLower());
                            }
                        }
                    }

                    if (joinReference.FirstTableReference is NamedTableReference namedTableReference2)
                    {
                        foreach (Identifier tableNameIdentifier in namedTableReference2.SchemaObject.Identifiers)
                        {
                            if (!selectTables.Exists(x => x == tableNameIdentifier.Value.Trim().ToLower()))
                            {
                                selectTables.Add(tableNameIdentifier.Value.Trim().ToLower());
                            }
                        }
                    }

                    joinReference = joinReference.FirstTableReference as QualifiedJoin;
                }
                while (joinReference != null);
            }
        }

    }

    /// <summary>
    /// root object that represents the results of parsing SQL text
    /// </summary>
    public class ParserResults
    {
        
        /// <summary>
        /// Gets or sets the name of the stored procedure referenced in SQL text.
        /// </summary>
        /// <value>
        /// The name of the procedure.
        /// </value>
        public string ProcedureName { get; set; }


        /// <summary>
        /// Gets or sets the names of stored procedures invoked in SQL text.
        /// </summary>
        /// <value>
        /// The procedures invoked.
        /// </value>
        public List<string> ProceduresInvoked { get; set; }

        /// <summary>
        /// Gets or sets the table parsing results.
        /// </summary>
        /// <value>
        /// The table parsing results.
        /// </value>
        public List<TableParsingResult> TableParsingResults { get; set; } = new List<TableParsingResult>();

        /// <summary>
        /// Gets a value indicating whether this instance has parsing exception.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has parsing exception; otherwise, <c>false</c>.
        /// </value>
        public bool HasParsingException
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ParsingExceptionDetail);
            }
        }

        /// <summary>
        /// Gets or sets the parsing exception detail.
        /// </summary>
        /// <value>
        /// The parsing exception detail.
        /// </value>
        public string ParsingExceptionDetail { get; set; }


    }

    /// <summary>
    /// object model that represents the result of parsing SQL text of tables
    /// </summary>
    public class TableParsingResult
    {
        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        /// <value>
        /// The name of the table.
        /// </value>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the schema.
        /// </summary>
        /// <value>
        /// The schema.
        /// </value>
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the type of SQL operation on the table.
        /// </summary>
        /// <value>
        /// The type of the operation.
        /// </value>
        public SqlOperationType OperationType { get; set; }

        /// <summary>
        /// Gets or sets the column parsing results.
        /// </summary>
        /// <value>
        /// The column parsing results.
        /// </value>
        public List<ColumnParsingResult> ColumnParsingResults { get; set; } = new List<ColumnParsingResult>();
    }

    /// <summary>
    /// object model that represents the result of parsing SQL text for columns in a table
    /// </summary>
    public class ColumnParsingResult
    {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        /// <value>
        /// The name of the column.
        /// </value>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the type of the operation.
        /// </summary>
        /// <value>
        /// The type of the operation.
        /// </value>
        public SqlOperationType OperationType { get; set; }
    }

    /// <summary>
    /// Sql Operattion Type
    /// </summary>
    public enum SqlOperationType
    {
        SELECT,
        INSERT,
        UPDATE,
        DELETE
    }
}
