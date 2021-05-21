using DALHelperNet.Extensions;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.InternalClasses.Helpers.DataTransfer;
using DALHelperNet.Models;
using DALHelperNet.Models.Properties;
using MoreLinq;
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

        internal static WritableTableDefinition<T> GetWritableTableObject<T>(Enum ConnectionStringType)
        {
            return GetWritableTableObject<T>(ConnectionStringType: ConnectionStringType);
        }

        internal static WritableTableDefinition<T> GetWritableTableObject<T>(MySqlConnection ExistingConnection)
        {
            return GetWritableTableObject<T>(ExistingConnection: ExistingConnection);
        }

        internal static WritableTableDefinition<T> GetDalModelTableObject<T>(Enum ConnectionStringType)
        {
            return GetWritableTableObject<T>(ConnectionStringType: ConnectionStringType, AddStandardTriggers: true);
        }

        internal static WritableTableDefinition<T> GetDalModelTableObject<T>(MySqlConnection ExistingConnection)
        {
            return GetWritableTableObject<T>(ExistingConnection: ExistingConnection, AddStandardTriggers: true);
        }

        private static WritableTableDefinition<T> GetWritableTableObject<T>(Enum ConnectionStringType = null, MySqlConnection ExistingConnection = null, bool AddStandardTriggers = false)
        {
            var tableDef = new WritableTableDefinition<T>
            {
                DatabaseName = ExistingConnection?.Database ?? DALHelper.GetConnectionBuilderFromConnectionType(ConnectionStringType)?.Database
            };

            if (AddStandardTriggers)
                tableDef
                    .SetTrigger(TriggerTypes.BeforeUpdate, "set NEW.last_updated = CURRENT_TIMESTAMP;")
                    .SetTrigger(TriggerTypes.BeforeInsert, "set new.InternalId = IFNULL(new.InternalId, uuid());\r\nset NEW.last_updated = CURRENT_TIMESTAMP;");

            return tableDef;
        }

        internal static bool CreateTable<T>(Enum ConnectionStringType, bool TruncateIfExists = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromString(ConnectionStringType))
            {
                return CreateTable<T>(conn, null, TruncateIfExists: TruncateIfExists);
            }
        }

        internal static bool CreateTable<T>(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null, bool TruncateIfExists = false, bool DropIfExists = false)
        {
            if (TruncateIfExists && DropIfExists)
                throw new ArgumentException("Cannot both truncate and drop table on create.");

            var createdTable = GetDalModelTableObject<T>(ExistingConnection: ExistingConnection);

            var rowsUpdated = DALHelper.DoDatabaseWork<int>(ExistingConnection, createdTable.ToString(), UseTransaction: false); //, SqlTransaction: SqlTransaction);

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
