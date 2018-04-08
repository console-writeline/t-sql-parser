using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSqlParser.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;

namespace TSqlParser.Core.Tests
{
    [TestClass()]
    public class SqlScriptAnalyzerTests
    {
        SqlScriptAnalyzer _analyzer;
        string _sqlText;

        [TestInitialize]
        public void Init()
        {
            _analyzer = new SqlScriptAnalyzer();
            //_sqlText = File.ReadAllText(ConfigurationManager.AppSettings["sql-text-input-file"]);
        }

        [TestMethod()]
        public void SqlScriptAnalyzerTest()
        {
            var obj = new SqlScriptAnalyzer();
            Assert.IsNotNull(obj);
        }

        [TestMethod()]
        public void AnalyzeSqlTextAsyncTest()
        {
            //TODO:
            var actual = _analyzer.AnalyzeSqlTextAsync(_sqlText).Result;
            var expected = new ParserResults();
            Assert.IsNotNull(actual);
        }

        [TestMethod()]
        public void AnalyzeDeclareSelectVariableStatementTest()
        {
            _sqlText = @"DECLARE @someVariable INT
                         SELECT @someVariable = some_column_int FROM table1 t1 INNER JOIN table2 t2 ON t1.id = t2.t1id                        
                        ";
            var actual = _analyzer.AnalyzeSqlTextAsync(_sqlText).Result;
            var expected = new ParserResults()
            {
                 TableParsingResults = new List<TableParsingResult>()
                 {
                     new TableParsingResult()
                     {
                          TableName = "table2",
                          Alias = "t2",
                          OperationType = SqlOperationType.SELECT
                     },
                     new TableParsingResult()
                     {
                         TableName = "table1",
                          Alias = "t1",
                          OperationType = SqlOperationType.SELECT
                     }
                 }
            };

            Assert.IsFalse(actual.HasParsingException);
            Assert.AreEqual<int>(actual.TableParsingResults.Count, expected.TableParsingResults.Count);
            Assert.AreEqual<string>(expected.TableParsingResults[0].TableName, actual.TableParsingResults[0].TableName);
        }

        [TestMethod()]
        public void AnalyzeDeclareSetVariableStatementTest()
        {
            _sqlText = @"DECLARE @someVariable INT = (SELECT some_column_int FROM table1 t1 INNER JOIN table2 t2 ON t1.id = t2.t1id);";

            var actual = _analyzer.AnalyzeSqlTextAsync(_sqlText).Result;
            var expected = new ParserResults()
            {
                TableParsingResults = new List<TableParsingResult>()
                 {
                     new TableParsingResult()
                     {
                          TableName = "table2",
                          Alias = "t2",
                          OperationType = SqlOperationType.SELECT
                     },
                     new TableParsingResult()
                     {
                         TableName = "table1",
                          Alias = "t1",
                          OperationType = SqlOperationType.SELECT
                     }
                 }
            };

            Assert.IsFalse(actual.HasParsingException);
            Assert.AreEqual<int>(actual.TableParsingResults.Count, expected.TableParsingResults.Count);
            Assert.AreEqual<string>(expected.TableParsingResults[0].TableName, actual.TableParsingResults[0].TableName);
        }
    }
}