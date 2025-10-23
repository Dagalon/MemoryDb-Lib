using SqliteDB_Memory_Lib;

namespace SqliteDb_Memory_Tests;

public class ExecuteQueries
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void T_Create_Table()
    {
        const string idDataBase = "TEST_DB";
        
        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection();

        var idTable = "TABLE_PERSONAL_DATA";
        
        //  create a database
        var checkDataBase = SqLiteLiteTools.CreateDatabase(conn, idDataBase, null);
        Assert.That(checkDataBase == EnumsSqliteMemory.Output.SUCCESS);
        var listDataBases = SqLiteLiteTools.GetListDataBase(conn);
        
        // data of the table
        var headers = new List<string> { "ID", "NAME", "FIRST_NAME", "AGE", "JOB" };
        var data = new object[,] { { 1, "Juan", "Garcia", 25, "Programmer" }, 
            { 2, "Pedro", "Moreno", 45, "Engineering" },
            {3, "Maria", "Lopez", 32, "Electricity"}
        };
        
        var checkTable = SqLiteLiteTools.CreateTable(conn, idDataBase, idTable, headers, data);
        Assert.That(checkTable == EnumsSqliteMemory.Output.SUCCESS);
        
        // execute query
        var qry = @"SELECT * FROM TABLE_PERSONAL_DATA";
        var results = SqLiteLiteTools.Select(conn, qry);
        Assert.That(checkTable == EnumsSqliteMemory.Output.SUCCESS);
    }
    
    [Test]
    public void T_Insert_Data()
    {
        const string idDataBase = "TEST_DB";
        
        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection();

        var idTable = "TABLE_PERSONAL_DATA";
        
        //  create a database
        var checkDataBase = SqLiteLiteTools.CreateDatabase(conn, idDataBase, null);
        Assert.That(checkDataBase == EnumsSqliteMemory.Output.SUCCESS);
        
        // data of the table
        var headers = new List<string> { "ID", "NAME", "FIRST_NAME", "AGE", "JOB" };
        var checkTable = SqLiteLiteTools.CreateTable(conn, idDataBase, idTable, headers, null);
        Assert.That(checkTable == EnumsSqliteMemory.Output.SUCCESS);
        
        var data = new object[,] {
            { 1, "Juan", "Garcia", 25, "Programmer" },
            { 2, "Pedro", "Moreno", 45, "Engineer" },
            { 3, "Maria", "Lopez", 32, "Electrician" },
            { 4, "Luis", "Martinez", 28, "Designer" },
            { 5, "Ana", "Sanchez", 41, "Teacher" },
            { 6, "Jorge", "Fernandez", 35, "Architect" },
            { 7, "Lucia", "Ramirez", 30, "Nurse" },
            { 8, "Diego", "Gomez", 27, "Developer" },
            { 9, "Sofia", "Torres", 29, "Analyst" },
            { 10, "Raul", "Dominguez", 39, "Technician" },
            { 11, "Carla", "Ruiz", 33, "Lawyer" },
            { 12, "Hector", "Jimenez", 42, "Manager" },
            { 13, "Isabel", "Castro", 26, "Data Scientist" },
            { 14, "Felipe", "Vega", 31, "Consultant" },
            { 15, "Marta", "Cruz", 37, "Economist" },
            { 16, "Andres", "Navarro", 44, "Project Manager" },
            { 17, "Patricia", "Rojas", 23, "Marketing" },
            { 18, "Daniel", "Herrera", 38, "HR Specialist" },
            { 19, "Elena", "Flores", 34, "Chemist" },
            { 20, "Ruben", "Mendoza", 29, "Accountant" },
            { 21, "Gabriela", "Iglesias", 36, "Pharmacist" },
            { 22, "Tomas", "Peña", 47, "Sales Manager" },
            { 23, "Laura", "Campos", 31, "Administrator" },
            { 24, "Oscar", "Silva", 40, "Physicist" },
            { 25, "Natalia", "Ortega", 27, "Graphic Designer" },
            { 26, "Sergio", "Carrillo", 43, "Electrician" },
            { 27, "Beatriz", "Molina", 28, "Programmer" },
            { 28, "Eduardo", "Suarez", 39, "Engineer" },
            { 29, "Clara", "Delgado", 24, "Nurse" },
            { 30, "Hugo", "Gil", 46, "Teacher" },
            { 31, "Noelia", "Vargas", 33, "Lawyer" },
            { 32, "Pablo", "Santos", 37, "Analyst" },
            { 33, "Rocio", "Blanco", 25, "Developer" },
            { 34, "Ignacio", "Acosta", 35, "Economist" },
            { 35, "Paula", "León", 29, "Consultant" },
            { 36, "Javier", "Ramos", 48, "Architect" },
            { 37, "Monica", "Cabrera", 26, "Marketing" },
            { 38, "Esteban", "Lara", 32, "Technician" },
            { 39, "Susana", "Reyes", 41, "Manager" },
            { 40, "Victor", "Paredes", 30, "HR Specialist" },
            { 41, "Camila", "Nieto", 27, "Accountant" },
            { 42, "Martin", "Fuentes", 36, "Engineer" },
            { 43, "Patricio", "Salas", 44, "Project Manager" },
            { 44, "Eva", "Benitez", 28, "Data Scientist" },
            { 45, "Fernando", "Bravo", 39, "Programmer" },
            { 46, "Irene", "Rico", 31, "Teacher" },
            { 47, "Nicolas", "Solano", 33, "Analyst" },
            { 48, "Carmen", "Luque", 29, "Developer" },
            { 49, "Alberto", "Estevez", 40, "Manager" },
            { 50, "Julia", "Prieto", 26, "Consultant" }
        };
        
        var checkInsert = SqLiteLiteTools.Insert(conn, idDataBase, idTable, headers, data);
        Assert.That(checkInsert == EnumsSqliteMemory.Output.SUCCESS);
        
        var qry = @"SELECT * FROM TEST_DB.TABLE_PERSONAL_DATA";
        var results = SqLiteLiteTools.Select(conn, qry);

        Assert.That(results.Count, Is.EqualTo(50));
        
    }
}