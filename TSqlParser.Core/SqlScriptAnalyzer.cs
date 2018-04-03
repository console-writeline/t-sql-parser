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
                var statmentList = _parser.ParseStatementList(sr, out errors);
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

                }
            }

            return await Task.FromResult(results);
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
        public List<TableParsingResult> TableParsingResults { get; set; }

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
        public List<ColumnParsingResult> ColumnParsingResults { get; set; }
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
