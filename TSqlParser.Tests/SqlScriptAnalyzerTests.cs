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
            _sqlText = File.ReadAllText(ConfigurationManager.AppSettings["sql-text-input-file"]);
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
    }
}