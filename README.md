# TSqlParser
Reference TSqlParser and init SqlScriptAnalyzer. The ParserResults object has the collection of tables and stored procedures invoked. 

```c#
string _sqlText = "INSERT INTO table1
SELECT a, b, c
FROM table2

EXEC pr_some_inner_procedure ";

SqlScriptAnalyzer _analyzer = new SqlScriptAnalyzer();
ParserResults actual = await _analyzer.AnalyzeSqlTextAsync(_sqlText);
Console.WriteLine(actual.ToString());
```


Parsing Result 
  - Tables touched 
    - table1 - INSERT
    - table2 - SELECT
	- Procedures Invoked 
		- pr_some_inner_procedure
