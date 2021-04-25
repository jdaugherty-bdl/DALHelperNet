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
            using (var conn = ConnectionHelper.GetConnectionFromString(ConnectionStringType))
            {
                return TruncateTable<T>(conn, TableName, ForceType, null);
            }
        }

        internal static bool TruncateTable<T>(MySqlConnection ExistingConnection, string TableName = null, Type ForceType = null, MySqlTransaction SqlTransaction = null)
        {
            var tableName = TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError);

            var truncateQuery = $"TRUNCATE {tableName};";

            var rowsUpdated = DatabaseWorkHelper.DoDatabaseWork<int>(ExistingConnection, truncateQuery, UseTransaction: true, SqlTransaction: SqlTransaction);

            var success = rowsUpdated > 0;

            return success;
        }

        internal static bool CreateTable<T>(Enum ConnectionStringType)
        {
            using (var conn = ConnectionHelper.GetConnectionFromString(ConnectionStringType))
            {
                return CreateTable<T>(conn, null);
            }
        }

        internal static bool CreateTable<T>(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null)
        {
            var tableType = typeof(T);

            var tableName = tableType.GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError);

            var resolvableProperties = tableType.GetProperties().Where(x => x.GetCustomAttribute<DALResolvable>() != null);

            if (resolvableProperties.Count() == 0)
                throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalPropertyAttributeError);

            //TODO: get properties from object, convert to underscore names

            return true;
        }
    }
}
