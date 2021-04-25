using DALHelperNet.Helpers.Persistence;
using DALHelperNet.Interfaces;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.InternalClasses.Helpers;
using DALHelperNet.InternalClasses.Helpers.DataTransfer;
using DALHelperNet.InternalClasses.Helpers.DataTransfer.Persistence;
using DALHelperNet.InternalClasses.Helpers.Operations;
using DALHelperNet.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet
{
	public static class DALHelper
	{
		// caches the last execution error encountered
		public static string LastExecutionError 
			=> DatabaseWorkHelper.LastExecutionError;

		// convenience function to check if there's an error cached
		public static bool HasError 
			=> DatabaseWorkHelper.HasError;

		/// <summary>
		/// Gets a MySQL connection builder that is then used to establish a connection to the database
		/// </summary>
		/// <param name="ConfigConnectionString">A properly formatted database connection string</param>
		/// <returns>A connection string builder that can be used to establish connections</returns>
		public static MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum ConfigConnectionString) 
			=> ConnectionHelper.GetConnectionBuilderFromConnectionType(ConfigConnectionString);

		public static MySqlConnection GetConnectionFromString(Enum ConfigConnectionString, bool AllowUserVariables = false) 
			=> ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables);

		/// <summary>
		/// Use the MySql built in function to get the ID of the last row inserted.
		/// </summary>
		/// <param name="ConfigConnectionString">The connection type to use when getting the last ID.</param>
		/// <returns>A string representation of the ID.</returns>
		public static string GetLastInsertId(Enum ConfigConnectionString) 
			=> RowIdentityHelper.GetLastInsertId(ConfigConnectionString);

		/// <summary>
		/// Converts an InternalId to an autonumbered row ID.
		/// </summary>
		/// <param name="ConfigConnectionString">The connection type to use.</param>
		/// <param name="Table">Table name to use for the conversion.</param>
		/// <param name="InternalId">The GUID of the InternalId to convert.</param>
		/// <returns>ID of the row matching the InternalId.</returns>
		public static string GetIdFromInternalId(Enum ConfigConnectionString, string Table, string InternalId) 
			=> RowIdentityHelper.GetIdFromInternalId(ConfigConnectionString, Table, InternalId);

		/// <summary>
		/// Query the database to get a single value
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to retrieve the value requested</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
		/// <param name="AllowUserVariables"></param>
		/// <param name="UseTransaction"></param>
		/// <param name="SqlTransaction"></param>
		/// <returns>Single value of type T</returns>
		public static T GetScalar<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
			=> RefinedResultsHelper.GetScalar<T>(ConfigConnectionString, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

		/// <summary>
		/// Query the database to get a single value
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
		/// <param name="QueryString">SQL query to retrieve the value requested</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
		/// <returns>Single value of type T</returns>
		public static T GetScalar<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
			=> RefinedResultsHelper.GetScalar<T>(EstablishedConnection, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);

		public static DataRow GetDataRow(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
			=> RefinedResultsHelper.GetDataRow(ConfigConnectionString, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

		public static DataRow GetDataRow(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
			=> RefinedResultsHelper.GetDataRow(EstablishedConnection, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);

		/// <summary>
		/// Query the database and return a table object
		/// </summary>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to retrieve the table requested</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw exception or swallow and return null</param>
		/// <returns>DataTable with requested data</returns>
		public static DataTable GetDataTable(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
			=> RefinedResultsHelper.GetDataTable(ConfigConnectionString, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

		public static DataTable GetDataTable(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
			=> RefinedResultsHelper.GetDataTable(EstablishedConnection, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

		public static T GetDataObject<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
			=> ObjectResultsHelper.GetDataObject<T>(ConfigConnectionString, QueryString, Parameters, ThrowException, SqlTransaction, AllowUserVariables);

		public static T GetDataObject<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
			=> ObjectResultsHelper.GetDataObject<T>(ExistingConnection, QueryString, Parameters, ThrowException, SqlTransaction, AllowUserVariables);

		public static IEnumerable<T> GetDataObjects<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
			=> ObjectResultsHelper.GetDataObjects<T>(ConfigConnectionString, QueryString, Parameters, ThrowException, SqlTransaction, AllowUserVariables);

		public static IEnumerable<T> GetDataObjects<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
			=> ObjectResultsHelper.GetDataObjects<T>(ExistingConnection, QueryString, Parameters, ThrowException, SqlTransaction, AllowUserVariables);

		public static IEnumerable<T> GetDataList<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
			=> ObjectResultsHelper.GetDataList<T>(ConfigConnectionString, QueryString, Parameters, ThrowException, SqlTransaction, AllowUserVariables);

		public static IEnumerable<T> GetDataList<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
			=> ObjectResultsHelper.GetDataList<T>(ExistingConnection, QueryString, Parameters, ThrowException, SqlTransaction, AllowUserVariables);

		/// <summary>
		/// Factory to retrieve a new bulk table writer instance. Caller can then run bulk inserts as per BulkTableWriter class.
		/// </summary>
		/// <param name="ConfigConnectionString">The DALHelper connection string type</param>
		/// <param name="InsertQuery">The full SQL query to be run on each row insert</param>
		/// <param name="UseTransaction">Indicate whether to write all the data in a single transaction</param>
		/// <param name="ThrowException">Indicate whether to throw exception or record and proceed</param>
		/// <returns>An object to add data to and write that data to the database</returns>
		public static BulkTableWriter<T> GetBulkTableWriter<T>(Enum ConfigConnectionString, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
			=> DataOutputOperations.GetBulkTableWriter<T>(ConfigConnectionString, InsertQuery, UseTransaction, ThrowException, SqlTransaction);

		public static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection ExistingConnection, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
			=> DataOutputOperations.GetBulkTableWriter<T>(ExistingConnection, InsertQuery, UseTransaction, ThrowException, SqlTransaction);

		public static int BulkTableWrite<T>(MySqlConnection ExistingConnection, T SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null)
			=> DataOutputOperations.BulkTableWrite<T>(ExistingConnection, SourceData, TableName, SqlTransaction, ForceType);

		public static int BulkTableWrite<T>(MySqlConnection ExistingConnection, IEnumerable<T> SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null)
			=> DataOutputOperations.BulkTableWrite<T>(ExistingConnection, SourceData, TableName, SqlTransaction, ForceType);

		public static int BulkTableWrite<T>(Enum ConfigConnectionString, T SourceData, string TableName = null, Type ForceType = null)
			=> DataOutputOperations.BulkTableWrite<T>(ConfigConnectionString, SourceData, TableName, ForceType);

		public static int BulkTableWrite<T>(Enum ConfigConnectionString, IEnumerable<T> SourceData, string TableName = null, Type ForceType = null)
			=> DataOutputOperations.BulkTableWrite<T>(ConfigConnectionString, SourceData, TableName, ForceType);

		/// <summary>
		/// Execute a non-query on the database with the specified parameters without returning a value
		/// </summary>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw swallow exception</param>
		public static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
			=> DatabaseWorkHelper.DoDatabaseWork(ConfigConnectionString, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

		/// <summary>
		/// Execute a non-query on the database with the specified parameters without returning a value
		/// </summary>
		/// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw swallow exception</param>
		public static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
			=> DatabaseWorkHelper.DoDatabaseWork(EstablishedConnection, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);

		/// <summary>
		/// Execute a non-query on the database and return the number of rows affected
		/// </summary>
		/// <typeparam name="T">Return type - only accepts String or Int</typeparam>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw exception or swallow and return null</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		/// <returns>Number of rows affected</returns>
		public static T DoDatabaseWork<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
			=> DatabaseWorkHelper.DoDatabaseWork<T>(ConfigConnectionString, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

		/// <summary>
		/// Execute a non-query on the database and return the generic type specified
		/// </summary>
		/// <typeparam name="T">Return type - only accepts String or Int</typeparam>
		/// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw exception or swallow and return null</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		/// <returns>Data in the type specified</returns>
		public static T DoDatabaseWork<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
		 => DatabaseWorkHelper.DoDatabaseWork<T>(EstablishedConnection, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);

		/// <summary>
		/// Execute a query on the database using the provided function without returning a value
		/// </summary>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="ActionCallback">Customized function to execute when connected to the database</param>
		/// <param name="ThrowException">Throw or swallow exception</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		public static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
			=> DatabaseWorkHelper.DoDatabaseWork(ConfigConnectionString, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

		/// <summary>
		/// Execute a query on the database using the provided function without returning a value
		/// </summary>
		/// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="ActionCallback">Customized function to execute when connected to the database</param>
		/// <param name="ThrowException">Throw or swallow exception</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		public static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
			=> DatabaseWorkHelper.DoDatabaseWork(EstablishedConnection, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction);

		/// <summary>
		/// Execute a query on the database using the provided function, returning value of type T
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to retrieve the data requested</param>
		/// <param name="ActionCallback">Customized function to execute when connected to the database</param>
		/// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		/// <returns>Data of any type T</returns>
		public static T DoDatabaseWork<T>(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
			=> DatabaseWorkHelper.DoDatabaseWork<T>(ConfigConnectionString, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);

		/// <summary>
		/// Execute a query on the database using the provided function, returning value of type T
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
		/// <param name="QueryString">SQL query to retrieve the data requested</param>
		/// <param name="ActionCallback">Customized function to execute when connected to the database</param>
		/// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		/// <returns>Data of any type T</returns>
		public static T DoDatabaseWork<T>(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
			=> DatabaseWorkHelper.DoDatabaseWork<T>(EstablishedConnection, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction);

		public static bool TruncateTable<T>(Enum ConnectionStringType, string TableName = null, Type ForceType = null)
			=> TableOperationsHelper.TruncateTable<T>(ConnectionStringType, TableName, ForceType);

		public static bool TruncateTable<T>(MySqlConnection ExistingConnection, string TableName = null, Type ForceType = null, MySqlTransaction SqlTransaction = null)
			=> TableOperationsHelper.TruncateTable<T>(ExistingConnection, TableName, ForceType, SqlTransaction);

		public static bool CreateTable<T>(Enum ConnectionStringType)
			=> TableOperationsHelper.CreateTable<T>(ConnectionStringType);

		public static bool CreateTable<T>(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null)
			=> TableOperationsHelper.CreateTable<T>(ExistingConnection, SqlTransaction);
	}
}
