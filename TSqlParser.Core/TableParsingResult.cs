using System.Collections.Generic;
using System.Diagnostics;

namespace TSqlParser.Core
{
    /// <summary>
    /// object model that represents the result of parsing SQL text of tables
    /// </summary>
    [DebuggerDisplay("Name = {TableName} Alias = {Alias} Operation= {OperationType}")]
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
        /// Gets or sets the alias.
        /// </summary>
        /// <value>
        /// The alias.
        /// </value>
        public string Alias { get; set; }

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
}
