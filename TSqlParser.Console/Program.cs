using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSqlParser.Core;

namespace TSqlParser.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            SqlScriptAnalyzer _analyzer;
            string _sqlText;

            _analyzer = new SqlScriptAnalyzer();
            _sqlText = File.ReadAllText(ConfigurationManager.AppSettings["sql-text-input-file"]);

            var actual = _analyzer.AnalyzeSqlTextAsync(_sqlText).Result;

            System.Console.WriteLine(actual.ToString());
            System.Console.WriteLine("\r\nPress any key to exit ..");
            System.Console.ReadLine();
        }
    }
}
