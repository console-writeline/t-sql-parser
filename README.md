# TSqlParser
## Why another parser ?
The goal of this project is to be able analyze T-Sql statements (similar to sp_depends someprocedurename) to get a list of tables touched with the operation type (UPDATE, INSERT, SELECT or DELETE) without having a connection to the database. If you have connection to the database, then definitely first review if sp_depends satisfies your requirements. 

## How is this different ?
sp_depends does not distinguish between updates and inserts and does not report on data being deleted from tables. And obviously, sp_depends works only on database objects - ie sql stored procedures. If you are running EF framework, that logs SQL statements that you want to analyze, sp_depends will not help. This TSqlParser is also able to find temp tables and table variables used within the SQL text that needs to be analyzed. 

## Cool, how can I use it ?
Reference TSqlParser.Core.dll and init SqlScriptAnalyzer. The ParserResults object has the collection of tables and stored procedures invoked. 

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

code on github @ <https://github.com/console-writeline/t-sql-parser>
nuget package @ <https://www.nuget.org/packages/TSqlParser>
