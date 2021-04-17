using DALHelperNet;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample.Examples
{
    public class DataTableExamples
    {
        public static bool RunAllDataTableExamples()
        {
            DataTable commonDataTable;

            commonDataTable = GetDataTableNoParameters();

            commonDataTable = GetDataTableWithParameters(0, "string value");

            using (var conn = DALHelper.GetConnectionFromString(ExampleConnectionStringTypes.FirstApplicationDatabase))
            {
                commonDataTable = GetDataTableWithParametersExistingConnection(conn, 0, "string value");

                using (var sqlTransaction = conn.BeginTransaction())
                {
                    commonDataTable = GetDataTableWithParametersExistingConnectionTransaction(conn, sqlTransaction, 0, "string value");

                    sqlTransaction.Commit();
                }
            }

            return true;
        }

        public static DataTable GetDataTableNoParameters()
        {
            var tableQuery = @"SELECT * FROM example_table
                WHERE column1 > 0
                AND column2 = 'string value'
                ORDER BY column3;";

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
