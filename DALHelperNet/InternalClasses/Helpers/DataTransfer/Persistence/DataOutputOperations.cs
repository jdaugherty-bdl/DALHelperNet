using DALHelperNet.Helpers.Persistence;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.InternalClasses.Helpers.Operations;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers.DataTransfer.Persistence
{
    internal class DataOutputOperations
    {
		/// <summary>
		/// Factory to retrieve a new bulk table writer instance. Caller can then run bulk inserts as per BulkTableWriter class.
		/// </summary>
		/// <param name="ConfigConnectionString">The DALHelper connection string type</param>
		/// <param name="InsertQuery">The full SQL query to be run on each row insert</param>
		/// <param name="UseTransaction">Indicate whether to write all the data in a single transaction</param>
		/// <param name="ThrowException">Indicate whether to throw exception or record and proceed</param>
		/// <returns>An object to add data to and write that data to the database</returns>
		internal static BulkTableWriter<T> GetBulkTableWriter<T>(Enum ConfigConnectionString, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
		{
			return new BulkTableWriter<T>(ConfigConnectionString, InsertQuery: InsertQuery, UseTransaction: UseTransaction || SqlTransaction != null, ThrowException: ThrowException, SqlTransaction: SqlTransaction);
		}

		internal static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection ExistingConnection, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
		{
			return new BulkTableWriter<T>(ExistingConnection, InsertQuery: InsertQuery, UseTransaction: UseTransaction || SqlTransaction != null, ThrowException: ThrowException, SqlTransaction: SqlTransaction);
		}

		internal static int BulkTableWrite<T>(Enum ConfigConnectionString, T SourceData, string TableName = null, Type ForceType = null)
		{
			/*
			var rowsUpdated = GetBulkTableWriter<T>(ConfigConnectionString)
				.SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(NoDalTableAttributeError))
				.SetSourceData(SourceData)
				.UseTransaction(true)
				.Write();

			return rowsUpdated;
			*/
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString))
            {
				return BulkTableWrite<T>(conn, SourceData, TableName, null, ForceType);
            }
		}

		internal static int BulkTableWrite<T>(Enum ConfigConnectionString, IEnumerable<T> SourceData, string TableName = null, Type ForceType = null)
		{
			/*
			var rowsUpdated = GetBulkTableWriter<T>(ConfigConnectionString)
				.SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(NoDalTableAttributeError))
				.SetSourceData(SourceData)
				.UseTransaction(true)
				.Write();

			return rowsUpdated;
			*/
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString))
            {
				return BulkTableWrite<T>(conn, SourceData, TableName, null, ForceType);
            }
		}

		//TODO: make each BulkTableWrite below chain up to a single BulkTableWrite that then calls GetBulkTableWriter
		internal static int BulkTableWrite<T>(MySqlConnection ExistingConnection, T SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null)
		{
			var rowsUpdated = GetBulkTableWriter<T>(ExistingConnection)
				.SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError))
				.SetSourceData(SourceData)
				.UseTransaction(true)
				.SetTransaction(SqlTransaction)
				.Write();

			return rowsUpdated;
		}

		internal static int BulkTableWrite<T>(MySqlConnection ExistingConnection, IEnumerable<T> SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null)
		{
			var rowsUpdated = GetBulkTableWriter<T>(ExistingConnection)
				.SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError))
				.SetSourceData(SourceData)
				.UseTransaction(true)
				.SetTransaction(SqlTransaction)
				.Write();

			return rowsUpdated;
		}
	}
}
