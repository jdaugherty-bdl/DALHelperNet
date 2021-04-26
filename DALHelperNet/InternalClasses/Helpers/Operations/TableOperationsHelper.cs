using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.InternalClasses.Helpers.DataTransfer;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers.Operations
{
    internal class TableOperationsHelper
    {
        internal static bool TruncateTable<T>(Enum ConnectionStringType, string TableName = null, Type ForceType = null)
        {
            return TruncateTable(ConnectionStringType, TableName: TableName, ForceType: ForceType ?? typeof(T));
        }

        internal static bool TruncateTable<T>(Enum ConnectionStringType, string TableName = null)
        {
            return TruncateTable(ConnectionStringType, TableName: TableName, ForceType: typeof(T));
        }

        internal static bool TruncateTable(Enum ConnectionStringType, string TableName = null, Type ForceType = null)
        {
            using (var conn = ConnectionHelper.GetConnectionFromString(ConnectionStringType))
            {
                return TruncateTable(conn, TableName: TableName, ForceType: ForceType, SqlTransaction: null);
            }
        }

        internal static bool TruncateTable<T>(MySqlConnection ExistingConnection, string TableName = null, Type ForceType = null, MySqlTransaction SqlTransaction = null)
        {
            return TruncateTable(ExistingConnection, TableName: TableName, ForceType: ForceType ?? typeof(T), SqlTransaction: SqlTransaction);
        }

        internal static bool TruncateTable<T>(MySqlConnection ExistingConnection, string TableName = null, MySqlTransaction SqlTransaction = null)
        {
            return TruncateTable(ExistingConnection, TableName: TableName, ForceType: typeof(T), SqlTransaction: SqlTransaction);
        }

        internal static bool TruncateTable(MySqlConnection ExistingConnection, string TableName = null, Type ForceType = null, MySqlTransaction SqlTransaction = null)
        {
            var tableName = TableName ?? ForceType.GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError);

            var truncateQuery = $"TRUNCATE {tableName};";

            var rowsUpdated = DatabaseWorkHelper.DoDatabaseWork<int>(ExistingConnection, truncateQuery, UseTransaction: true, SqlTransaction: SqlTransaction);

            var success = rowsUpdated > 0;

            return success;
        }

        internal static bool CreateTable<T>(Enum ConnectionStringType, bool TruncateIfExists = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromString(ConnectionStringType))
            {
                return CreateTable<T>(conn, null, TruncateIfExists: TruncateIfExists);
            }
        }

        internal static bool CreateTable<T>(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null, bool TruncateIfExists = false)
        {
            var tableType = typeof(T);
            var tableName = tableType.GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError);



            var resolvableProperties = tableType.GetProperties().Where(x => x.GetCustomAttribute<DALResolvable>() != null);

            if (resolvableProperties.Count() == 0)
                throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalPropertyAttributeError);

            //TODO: get properties from object, convert to underscore names

            return true;
        }

        internal static bool TableExists(Enum ConnectionStringType, string TableName)
        {
            using (var conn = ConnectionHelper.GetConnectionFromString(ConnectionStringType))
            {
                return TableExists(conn, TableName, null);
            }
        }

        internal static bool TableExists(MySqlConnection ExistingConnection, string TableName, MySqlTransaction SqlTransaction = null)
        {
            // pull the table details from the database
            var existsQuery = @"SELECT TABLE_NAME
                FROM information_schema.tables
                WHERE table_schema = @table_schema
                    AND table_name = @table_name
                LIMIT 1;";

            var tableName = RefinedResultsHelper.GetScalar<string>(ExistingConnection, existsQuery, new Dictionary<string, object>
            {
                ["@table_schema"] = ExistingConnection.Database,
                ["@table_name"] = TableName
            }, SqlTransaction: SqlTransaction);

            return !string.IsNullOrWhiteSpace(tableName);
        }
    }
}
