using DALHelperNet.Helpers.Persistence;
using DALHelperNet.Interfaces;
using DALHelperNet.Interfaces.Attributes;
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
		// message for the "no DALTable attribute" exception below
		private static string NoDalTableAttributeError => "Cannot get table name from class, try adding a 'DALTable' attribute.";

		// caches the last execution error encountered
		public static string LastExecutionError;
		// convenience function to check if there's an error cached
		public static bool HasError => !string.IsNullOrEmpty(LastExecutionError);
		// a pointer to the application's resolver instance
		public static IDALResolver DALResolver = GetResolverInstance();

		/// <summary>
		/// find an object inheriting from IDALResolver, but only look in the entry assembly (where all your custom code is)
		/// once it is found, then that object is loaded through Reflection to be used later on.
		/// </summary>
		/// <returns>The application's DALResolver instance.</returns>
		static IDALResolver GetResolverInstance()
		{
			// try to get the resolver the standard way
			var entryAssembly = AppDomain
				.CurrentDomain
				.GetAssemblies()
				.Where(x => !string.IsNullOrWhiteSpace(x.EntryPoint?.Name))
				.SelectMany(x => x
					.GetModules()
					.SelectMany(y => y
						.GetTypes()
						.Where(z => z
							.GetInterfaces()
							.Any(a => a == typeof(IDALResolver)))))
				.FirstOrDefault();

			// if the standard way didn't work, do a little detective work (may not work 100% of the time)
			var clientDalResolverType =
				entryAssembly
				??
				AppDomain
				.CurrentDomain
				.GetAssemblies()
				.Where(x => x
					.GetCustomAttributes(true)
					.Any(y => y is AssemblyCompanyAttribute attribute && !attribute.Company.StartsWith("Microsoft", StringComparison.InvariantCultureIgnoreCase)))
				.SelectMany(x => x
					.GetModules()
					.SelectMany(y => y
						.GetTypes()
						.Where(z => z
							.GetInterfaces()
							.Any(a => a == typeof(IDALResolver)))))
				.FirstOrDefault();

			if (clientDalResolverType != null)
				return (IDALResolver)Activator.CreateInstance(clientDalResolverType);
			else
				throw new NullReferenceException("[GenericDALResolver]IDALResolver not found");
		}

		/// <summary>
		/// Gets a MySQL connection builder that is then used to establish a connection to the database
		/// </summary>
		/// <param name="ConfigConnectionString">A properly formatted database connection string</param>
		/// <returns>A connection string builder that can be used to establish connections</returns>
		public static MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum ConfigConnectionString)
		{
			return DALResolver?.GetConnectionBuilderFromConnectionType(ConfigConnectionString);
		}

		/// <summary>
		/// Use the MySql built in function to get the ID of the last row inserted.
		/// </summary>
		/// <param name="ConfigConnectionString">The connection type to use when getting the last ID.</param>
		/// <returns>A string representation of the ID.</returns>
		public static string GetLastInsertId(Enum ConfigConnectionString)
		{
			return GetScalar<string>(ConfigConnectionString, "SELECT LAST_INSERT_ID();");
		}

		/// <summary>
		/// Converts an InternalId to an autonumbered row ID.
		/// </summary>
		/// <param name="ConfigConnectionString">The connection type to use.</param>
		/// <param name="Table">Table name to use for the conversion.</param>
		/// <param name="InternalId">The GUID of the InternalId to convert.</param>
		/// <returns>ID of the row matching the InternalId.</returns>
		public static string GetIdFromInternalId(Enum ConfigConnectionString, string Table, string InternalId)
		{
			return GetScalar<string>(ConfigConnectionString, $"SELECT ID FROM {Table} WHERE InternalId = @InternalId", new Dictionary<string, object> { { "@InternalId", InternalId } });
		}

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
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetScalar<T>(conn, QueryString, Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
			}
		}

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
		{
			return DoDatabaseWork<T>(EstablishedConnection, QueryString,
				(cmd) =>
				{
					cmd.Parameters.AddAllParameters(Parameters);

					var scalarResult = cmd.ExecuteScalar();

					return ConvertScalar<T>(scalarResult);
				},
				ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
		}

		private static object ConvertScalar<T>(object ScalaraValue)
        {
			if (ScalaraValue == null || ScalaraValue is DBNull)
				return default(T);
			else if (typeof(T) == typeof(string))
				return ScalaraValue.ToString();
			else if (typeof(T) == typeof(int))
				return int.TryParse(ScalaraValue.ToString(), out int scalar) ? scalar : default;
			else if (typeof(T) == typeof(long))
				return long.TryParse(ScalaraValue.ToString(), out long scalar) ? scalar : default;
			else if (typeof(T) == typeof(decimal))
				return decimal.TryParse(ScalaraValue.ToString(), out decimal scalar) ? scalar : default;
			else if (typeof(T) == typeof(float))
				return float.TryParse(ScalaraValue.ToString(), out float scalar) ? scalar : default;
			else if (typeof(T) == typeof(bool))
				return !(ScalaraValue.ToString() == "0");
			else if (typeof(T) == typeof(DateTime))
				return DateTime.TryParse(ScalaraValue.ToString(), out DateTime scalar) ? scalar : default;
			else
				return (T)ScalaraValue;
		}

		private static void AddAllParameters(this MySqlParameterCollection CommandParameters, Dictionary<string, object> Parameters)
        {
			CommandParameters
				.AddRange(
					Parameters?
						.Select(x => new MySqlParameter(x.Key, x.Value))
						.ToArray()
					??
					Enumerable
						.Empty<MySqlParameter>()
						.ToArray());
		}

		public static DataRow GetDataRow(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataRow(conn, QueryString, Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
			}
		}

		public static DataRow GetDataRow(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
		{
			var intermediate = GetDataTable(EstablishedConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);

			return intermediate.Rows.Count > 0 ? intermediate.Rows[0] : null;
		}

		/// <summary>
		/// Query the database and return a table object
		/// </summary>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to retrieve the table requested</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw exception or swallow and return null</param>
		/// <returns>DataTable with requested data</returns>
		public static DataTable GetDataTable(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataTable(conn, QueryString, Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		public static DataTable GetDataTable(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			return DoDatabaseWork<DataTable>(EstablishedConnection, QueryString,
				(cmd) =>
				{
					cmd.Parameters.AddAllParameters(Parameters);

					using (var tableAdapter = new MySqlDataAdapter())
					{
						tableAdapter.SelectCommand = cmd;
						tableAdapter.SelectCommand.CommandType = CommandType.Text;

						var outputTable = new DataTable();
						tableAdapter.Fill(outputTable);

						return outputTable;
					}
				}, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
		}

		public static T GetDataObject<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataObject<T>(conn, QueryString, Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		public static T GetDataObject<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			return GetDataObjects<T>(ExistingConnection, QueryString, Parameters, ThrowException, SqlTransaction, AllowUserVariables)
				.FirstOrDefault();
		}

		public static IEnumerable<T> GetDataObjects<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataObjects<T>(conn, QueryString, Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		public static IEnumerable<T> GetDataObjects<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			return GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables)
				.AsEnumerable()
				.Select(x => x == null ? null : CreateCreatorExpression<DataRow, string, T>()(x, null)); //return (T)Activator.CreateInstance(typeof(T), x, null);
		}

		/// <summary>
		/// Creates a labmda expression to instantiate objects of type T which take two constructor parameters.
		/// </summary>
		/// <typeparam name="TArg1">First parameter type.</typeparam>
		/// <typeparam name="TArg2">Second parameter type.</typeparam>
		/// <typeparam name="T">Return type.</typeparam>
		/// <returns>An instantiation function that will create a new concrete object of type T.</returns>
		private static Func<TArg1, TArg2, T> CreateCreatorExpression<TArg1, TArg2, T>()
		{
			// Lambda Expressions are much faster than Activator.CreateInstance when creating more than one object due to Expression caching

			// get object constructor
			var constructor = typeof(T).GetConstructor(new Type[] { typeof(TArg1), typeof(TArg2) });

			// define individual parameters
			var parameterList = new ParameterExpression[]
			{
				Expression.Parameter(typeof(TArg1)),
				Expression.Parameter(typeof(TArg2))
			};

			// create the expression
			var creatorExpression = Expression.Lambda<Func<TArg1, TArg2, T>>(Expression.New(constructor, parameterList), parameterList);

			// compile the expression
			return creatorExpression.Compile();
		}

		public static IEnumerable<T> GetDataList<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataList<T>(conn, QueryString, Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		public static IEnumerable<T> GetDataList<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			return GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables)
				.AsEnumerable()
				.Select(x => (T)ConvertScalar<T>(x[0]));
		}

		/// <summary>
		/// Factory to retrieve a new bulk table writer instance. Caller can then run bulk inserts as per BulkTableWriter class.
		/// </summary>
		/// <param name="ConfigConnectionString">The DALHelper connection string type</param>
		/// <param name="InsertQuery">The full SQL query to be run on each row insert</param>
		/// <param name="UseTransaction">Indicate whether to write all the data in a single transaction</param>
		/// <param name="ThrowException">Indicate whether to throw exception or record and proceed</param>
		/// <returns>An object to add data to and write that data to the database</returns>
		public static BulkTableWriter<T> GetBulkTableWriter<T>(Enum ConfigConnectionString, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
		{
			return new BulkTableWriter<T>(ConfigConnectionString, InsertQuery: InsertQuery, UseTransaction: UseTransaction || SqlTransaction != null, ThrowException: ThrowException, SqlTransaction: SqlTransaction);
		}

		public static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection ExistingConnection, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
		{
			return new BulkTableWriter<T>(ExistingConnection, InsertQuery: InsertQuery, UseTransaction: UseTransaction || SqlTransaction != null, ThrowException: ThrowException, SqlTransaction: SqlTransaction);
		}

		//TODO: make each BulkTableWrite below chain up to a single BulkTableWrite that then calls GetBulkTableWriter
		public static int BulkTableWrite<T>(MySqlConnection ExistingConnection, T SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null)
		{
			var rowsUpdated = GetBulkTableWriter<T>(ExistingConnection)
				.SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(NoDalTableAttributeError))
				.SetSourceData(SourceData)
				.UseTransaction(true)
				.SetTransaction(SqlTransaction)
				.Write();

			return rowsUpdated;
		}

		public static int BulkTableWrite<T>(MySqlConnection ExistingConnection, IEnumerable<T> SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null)
		{
			var rowsUpdated = GetBulkTableWriter<T>(ExistingConnection)
				.SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(NoDalTableAttributeError))
				.SetSourceData(SourceData)
				.UseTransaction(true)
				.SetTransaction(SqlTransaction)
				.Write();

			return rowsUpdated;
		}

		public static int BulkTableWrite<T>(Enum ConfigConnectionString, T SourceData, string TableName = null, Type ForceType = null)
		{
			var rowsUpdated = GetBulkTableWriter<T>(ConfigConnectionString)
				.SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(NoDalTableAttributeError))
				.SetSourceData(SourceData)
				.UseTransaction(true)
				.Write();

			return rowsUpdated;
		}

		public static int BulkTableWrite<T>(Enum ConfigConnectionString, IEnumerable<T> SourceData, string TableName = null, Type ForceType = null)
		{
			var rowsUpdated = GetBulkTableWriter<T>(ConfigConnectionString)
				.SetTableName(TableName ?? (ForceType ?? typeof(T)).GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(NoDalTableAttributeError))
				.SetSourceData(SourceData)
				.UseTransaction(true)
				.Write();

			return rowsUpdated;
		}

		/// <summary>
		/// Execute a non-query on the database with the specified parameters without returning a value
		/// </summary>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw swallow exception</param>
		public static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				DoDatabaseWork(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
			}
		}

		/// <summary>
		/// Execute a non-query on the database with the specified parameters without returning a value
		/// </summary>
		/// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw swallow exception</param>
		public static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
		{
			DoDatabaseWork(EstablishedConnection, QueryString,
				(cmd) =>
				{
					cmd.Parameters.AddAllParameters(Parameters);

					return cmd.ExecuteNonQuery();
				},
				ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
		}

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
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return DoDatabaseWork<T>(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
			}
		}

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
		{
			return DoDatabaseWork<T>(EstablishedConnection, QueryString,
				(cmd) =>
				{
					cmd.Parameters.AddAllParameters(Parameters);

					var executionWork = cmd.ExecuteNonQuery();

					if (typeof(T) == typeof(string))
						return executionWork.ToString();
					else if (typeof(T) == typeof(int))
						return executionWork;
					else
						return default;
				},
				ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
		}

		public static MySqlConnection GetConnectionFromString(Enum ConfigConnectionString, bool AllowUserVariables = false)
		{
			var connectionBuilder = GetConnectionBuilderFromConnectionType(ConfigConnectionString); // new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[connectionString].ConnectionString);
			connectionBuilder.ConvertZeroDateTime = true;

			if (AllowUserVariables)
				connectionBuilder.AllowUserVariables = true;

			return new MySqlConnection(connectionBuilder.ToString());
		}

		/// <summary>
		/// Execute a query on the database using the provided function without returning a value
		/// </summary>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="ActionCallback">Customized function to execute when connected to the database</param>
		/// <param name="ThrowException">Throw or swallow exception</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		public static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			DoDatabaseWork<object>(ConfigConnectionString, QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
		}

		/// <summary>
		/// Execute a query on the database using the provided function without returning a value
		/// </summary>
		/// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="ActionCallback">Customized function to execute when connected to the database</param>
		/// <param name="ThrowException">Throw or swallow exception</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		public static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
		{
			DoDatabaseWork<object>(EstablishedConnection, QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
		}

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
		{
			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return DoDatabaseWork<T>(conn, QueryString, ActionCallback, ThrowException: ThrowException, UseTransaction: UseTransaction || SqlTransaction != null, SqlTransaction: SqlTransaction);
			}
		}

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
		{
			var internalOpen = false; // indicates whether the connection was already open or not
			var openedNewTransaction = false; // indicates whether a new transaction was created here or not
			var currentTransaction = SqlTransaction; // preload current transaction

			// reset the last execution error
			LastExecutionError = null;

			try
			{
				// if the connection isn't open, then open it and record that we did that
				if (EstablishedConnection.State != ConnectionState.Open)
				{
					EstablishedConnection.Open();
					internalOpen = true;
				}

				// if the caller wants to use transactions but they didn't provide one, create a new one
				if (UseTransaction && SqlTransaction == null)
				{
					currentTransaction = EstablishedConnection.BeginTransaction();
					openedNewTransaction = true;
				}

				// execute the SQL
				using (var cmd = new MySqlCommand(QueryString, EstablishedConnection))
				{
					cmd.CommandTimeout = int.MaxValue;

					// execute whatever code the caller provided
					var result = (T)ActionCallback(cmd);

					// if we opened the transaction here, just commit it because we're going to be closing it right away
					if (openedNewTransaction)
						currentTransaction?.Commit();

					return result;
				}
			}
			catch (MySqlException mysqlEx) // use special handling for MySQL exceptions
			{
				// there was an error, roll back the transaction
				if (UseTransaction)
					currentTransaction?.Rollback();

				// if we want exceptions to be thrown, rethrow the current one, otherwise just record the error
				if (ThrowException)
					throw new Exception(mysqlEx.Message, mysqlEx);
				else
				{
					LastExecutionError = mysqlEx.Message;

					return default;
				}
			}
			catch (Exception ex) // handle all other unhandled exceptions
			{
				// there was an error, roll back the transaction
				if (UseTransaction)
					currentTransaction?.Rollback();

				// if we want exceptions to be thrown, rethrow the current one, otherwise just record the error
				if (ThrowException)
					throw new Exception(ex.Message, ex);
				else
				{
					LastExecutionError = ex.Message;

					return default;
				}
			}
			finally
			{
				// if we opened the connection, close it back up before it's disposed
				if (internalOpen && EstablishedConnection.State == ConnectionState.Open)
					EstablishedConnection.Close();
			}
		}
	}
}
