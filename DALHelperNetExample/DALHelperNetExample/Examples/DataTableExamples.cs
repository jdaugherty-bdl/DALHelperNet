using DALHelperNet;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNetExample.Models;
using DALHelperNetExample.Models.Basic;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample.Examples
{
    public class DataTableExamples
    {
        /// <summary>
        /// Runs all DALHelperNet examples that return a DataTable.
        /// </summary>
        /// <returns>True/false as to whether or not all runs were successful.</returns>
        public static bool RunAllDataTableExamples()
        {
            DataTable commonDataTable; // this is the standard return type for the following functions

            // get a table without passing any parameters
            commonDataTable = GetDataTableNoParameters();

            // get a table without passing any parameters and using the DataTable attribute of a class to get the table name
            commonDataTable = GetDataTableNoParametersUsingDataTableAttribute();

            // get a table with passing parameters
            commonDataTable = GetDataTableWithParameters(0, "string value");

            // simulate a previously opened database connection
            using (var conn = DALHelper.GetConnectionFromString(ExampleConnectionStringTypes.FirstApplicationDatabase))
            {
                // get a table with passing parameters using a previously opened connection
                commonDataTable = GetDataTableWithParametersExistingConnection(conn, 0, "string value");

                // simulate a previously begun transaction on a previously opened database connection
                using (var sqlTransaction = conn.BeginTransaction())
                {
                    // get a table with passing parameters using a previously opened connection and transaction
                    commonDataTable = GetDataTableWithParametersExistingConnectionTransaction(conn, sqlTransaction, 0, "string value");

                    // simulate a transaction commit
                    sqlTransaction.Commit();
                }
            }

            return true;
        }

        /// <summary>
        /// This will get a DataTable using the example query. It uses no parameters.
        /// </summary>
        /// <returns>A DataTable filled with the selection provided.</returns>
        public static DataTable GetDataTableNoParameters()
        {
            // get data query with an ORDER BY statement
            var tableQuery = @"SELECT * FROM example_table
                WHERE column1 > 0
                AND column2 = 'string value'
                ORDER BY column3;";

            // get the DataTable with no parameters
            var resultingTable = DALHelper.GetDataTable(ExampleConnectionStringTypes.FirstApplicationDatabase, tableQuery);

            return resultingTable;
        }

        /// <summary>
        /// This will get a DataTable using the example query. It uses the DALTable attribute on one of the example classes to determine which table to use in the query.
        /// If no attribute is attached to the class, the table name will be blank.
        /// </summary>
        /// <returns>A DataTable filled with the selection provided.</returns>
        public static DataTable GetDataTableNoParametersUsingDataTableAttribute()
        {
            // get data query using the DALTable attribute on the ExampleBasicObject class as a table name
            var tableQuery = $@"SELECT * FROM {typeof(ExampleBasicObject).GetCustomAttribute<DALTable>()?.TableName}
                WHERE column1 > 0
                AND column2 = 'string value'
                ORDER BY column3;";

            // get the DataTable with no parameters
            var resultingTable = DALHelper.GetDataTable(ExampleConnectionStringTypes.FirstApplicationDatabase, tableQuery);

            return resultingTable;
        }

        public static DataTable GetDataTableWithParameters(int Value1, string Value2)
        {
            var tableQuery = @"SELECT * FROM example_table
                WHERE column1 > @value_one
                AND column2 = @value_two
                ORDER BY column3;";

            var resultingTable = DALHelper.GetDataTable(ExampleConnectionStringTypes.FirstApplicationDatabase, tableQuery, new Dictionary<string, object>
            {
                { "@value_one", Value1 },
                { "@value_two", Value2 }
            });

            return resultingTable;
        }

        public static DataTable GetDataTableWithParametersExistingConnection(MySqlConnection ExistingConnection, int Value1, string Value2)
        {
            var tableQuery = @"SELECT * FROM example_table
                WHERE column1 > @value_one
                AND column2 = @value_two
                ORDER BY column3;";

            var resultingTable = DALHelper.GetDataTable(ExistingConnection, tableQuery, new Dictionary<string, object>
            {
                { "@value_one", Value1 },
                { "@value_two", Value2 }
            });

            return resultingTable;
        }

        public static DataTable GetDataTableWithParametersExistingConnectionTransaction(MySqlConnection ExistingConnection, MySqlTransaction ExistingTransaction, int Value1, string Value2)
        {
            var tableQuery = @"SELECT * FROM example_table
                WHERE column1 > @value_one
                AND column2 = @value_two
                ORDER BY column3;";

            var resultingTable = DALHelper.GetDataTable(ExistingConnection, tableQuery, new Dictionary<string, object>
            {
                { "@value_one", Value1 },
                { "@value_two", Value2 }
            }, SqlTransaction: ExistingTransaction);

            return resultingTable;
        }
    }
}
