namespace TSqlParser.Core
{
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
}
