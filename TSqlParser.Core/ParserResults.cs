using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TSqlParser.Core
{
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
        public List<string> ProceduresInvoked { get; set; } = new List<string>();

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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Parsing Result ::");

            if(this.TableParsingResults.Count > 0)
                sb.AppendLine($"\tTables touched ::");

            foreach (var table in this.TableParsingResults.OrderBy(x=>x.TableName))
            {
                sb.AppendLine($"\t\t{table.TableName} - {table.OperationType}");
            }

            if (this.ProceduresInvoked.Count > 0)
                sb.AppendLine($"\r\n\tProcedures Invoked ::");

            foreach (var proc in this.ProceduresInvoked)
            {
                sb.AppendLine($"\t\t{proc}");
            }

            return sb.ToString();
        }
    }
}
