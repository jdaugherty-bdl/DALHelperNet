﻿using DALHelperNet.Helpers.Persistence;
using DALHelperNet.Interfaces;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet
{
	public static class DALHelper
	{
		private static string NoDalTableAttributeError => "Cannot get table name from class, try adding a 'DALTable' attribute.";

		public static string LastExecutionError;
		public static IDALResolver DALResolver;
		public static bool HasError
		{
			get
			{
				return !string.IsNullOrEmpty(LastExecutionError);
			}
		}

		/*
		public enum ConnectionStringTypes
		{
			TraderFlowDatabase,
			BdlPortalDatabase
		}
		*/
		/*
		static private Assembly GetWebEntryAssembly()
		{
			var type = System.Web.HttpContext.Current?.ApplicationInstance?.GetType();
			while (type?.Namespace == "ASP")
			{
				type = type.BaseType;
			}

			return type?.Assembly;
		}
		*/

		static DALHelper()
		{
			// find an IDALResolver object marked with the GenericDALResolver attribute anywhere in the assembly and loads it to this DAL helper
			// the GenericDALResolver attribute allows us to have multiple IDALResolver objects in our project and be able to mark only one active at a time
			// filter aseembly list below to only custom libraries instead of resolving all assemblies (includes System.*, etc. libraries)

			//var entryAssembly = Assembly.GetEntryAssembly() ?? GetWebEntryAssembly(); // only look in the entry assembly to make things quicker
			//var clientDalResolverType = entryAssembly
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

			var clientDalResolverType =
				entryAssembly
				??
				AppDomain
				.CurrentDomain
				.GetAssemblies()
				.Where(x => x
					.GetCustomAttributes(true)
					.Any(y => y is AssemblyCompanyAttribute && !((AssemblyCompanyAttribute)y).Company.ToLower().StartsWith("microsoft")))
				.SelectMany(x => x
					.GetModules()
					.SelectMany(y => y
						.GetTypes()
						.Where(z => z
							.GetInterfaces()
							.Any(a => a == typeof(IDALResolver)))))
				/*
				.Where(x => x
					.GetCustomAttributes(true)
					.Any(y => y.GetType() == typeof(GenericDALResolver)))
				*/
				.FirstOrDefault();

			if (clientDalResolverType != null)
				DALResolver = (IDALResolver)Activator.CreateInstance(clientDalResolverType);
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

		public static string GetLastInsertId(Enum ConfigConnectionString)
		{
			return GetScalar<string>(ConfigConnectionString, "SELECT LAST_INSERT_ID();");
		}

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
		/// <returns>Single value of type T</returns>
		public static T GetScalar<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			if (SqlTransaction != null)
				UseTransaction = true;

			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetScalar<T>(conn, QueryString, Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction, SqlTransaction: SqlTransaction);
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
			if (SqlTransaction != null)
				UseTransaction = true;

			return DALHelper.DoDatabaseWork<T>(EstablishedConnection, QueryString,
				(cmd) =>
				{
					if (Parameters != null)
					{
						foreach (var parameter in Parameters)
						{
							cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
						}
					}

					var scalarResult = cmd.ExecuteScalar();

					if (scalarResult == null || scalarResult is DBNull)
						return default(T);
					else if (typeof(T) == typeof(string))
						return scalarResult.ToString();
					else if (typeof(T) == typeof(int))
						return int.Parse(scalarResult.ToString());
					else if (typeof(T) == typeof(long))
						return long.Parse(scalarResult.ToString());
					else if (typeof(T) == typeof(decimal))
						return decimal.Parse(scalarResult.ToString());
					else if (typeof(T) == typeof(float))
						return float.Parse(scalarResult.ToString());
					else if (typeof(T) == typeof(bool))
						return !(scalarResult.ToString() == "0");
					else
						return (T)scalarResult;
				},
				ThrowException: ThrowException, UseTransaction: UseTransaction, SqlTransaction: SqlTransaction);
		}

		public static DataRow GetDataRow(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			if (SqlTransaction != null)
				UseTransaction = true;

			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataRow(conn, QueryString, Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction, SqlTransaction: SqlTransaction);
			}
		}

		public static DataRow GetDataRow(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
		{
			var intermediate = GetDataTable(EstablishedConnection, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);

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
			if (SqlTransaction != null)
				UseTransaction = true;

			using (var conn = GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataTable(conn, QueryString, Parameters, ThrowException: ThrowException, UseTransaction: UseTransaction, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		public static DataTable GetDataTable(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			if (SqlTransaction != null)
				UseTransaction = true;

			return DoDatabaseWork<DataTable>(EstablishedConnection, QueryString,
				(cmd) =>
				{
					if (Parameters != null)
					{
						foreach (var parameter in Parameters)
						{
							cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
						}
					}

					using (var tableAdapter = new MySqlDataAdapter())
					{
						tableAdapter.SelectCommand = cmd;
						tableAdapter.SelectCommand.CommandType = CommandType.Text;

						var outputTable = new DataTable();
						tableAdapter.Fill(outputTable);

						return outputTable;
					}
				}, ThrowException: ThrowException, UseTransaction: UseTransaction, SqlTransaction: SqlTransaction);
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
			return GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables)
				.AsEnumerable()
				.Select(x => (T)Activator.CreateInstance(typeof(T), x, null))
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
				.Select(x => (T)Activator.CreateInstance(typeof(T), x, null));
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
			return new BulkTableWriter<T>(ConfigConnectionString, InsertQuery, UseTransaction, ThrowException, SqlTransaction);
		}

		public static BulkTableWriter<T> GetBulkTableWriter<T>(MySqlConnection ExistingConnection, string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
		{
			return new BulkTableWriter<T>(ExistingConnection, InsertQuery, UseTransaction, ThrowException, SqlTransaction);
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
				DoDatabaseWork(conn, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);
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
					if (Parameters != null)
					{
						foreach (var parameter in Parameters)
						{
							cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
						}
					}

					return cmd.ExecuteNonQuery();
				},
				ThrowException: ThrowException, UseTransaction: UseTransaction, SqlTransaction: SqlTransaction);
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
				return DoDatabaseWork<T>(conn, QueryString, Parameters, ThrowException, UseTransaction, SqlTransaction);
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
					if (Parameters != null)
					{
						foreach (var parameter in Parameters)
						{
							cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
						}
					}

					var executionWork = cmd.ExecuteNonQuery();

					if (typeof(T) == typeof(string))
						return executionWork.ToString();
					else if (typeof(T) == typeof(int))
						return executionWork;
					else
						return default(T);
				},
				ThrowException: ThrowException, UseTransaction: UseTransaction, SqlTransaction: SqlTransaction);
		}

		private static MySqlConnection GetConnectionFromString(Enum ConfigConnectionString, bool AllowUserVariables = false)
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
			DoDatabaseWork<object>(ConfigConnectionString, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction, AllowUserVariables);
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
			DoDatabaseWork<object>(EstablishedConnection, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction);
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
				return DoDatabaseWork<T>(conn, QueryString, ActionCallback, ThrowException, UseTransaction, SqlTransaction);
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
			var internalOpen = false;
			var openedNewTransaction = false;

			LastExecutionError = null;

			//var connectionBuilder = GetConnectionBuilderFromConnectionType(ConfigConnectionString); // new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[connectionString].ConnectionString);
			//connectionBuilder.ConvertZeroDateTime = true;

			//using (var conn = new MySqlConnection(connectionBuilder.ToString()))
			{
				MySqlTransaction currentTransaction = SqlTransaction;

				try
				{
					if (EstablishedConnection.State != ConnectionState.Open)
					{
						EstablishedConnection.Open();
						internalOpen = true;
					}

					if (UseTransaction & SqlTransaction == null)
					{
						currentTransaction = EstablishedConnection.BeginTransaction();

						openedNewTransaction = true;
					}

					using (var cmd = new MySqlCommand(QueryString, EstablishedConnection))
					{
						cmd.CommandTimeout = int.MaxValue;

						var result = (T)ActionCallback(cmd);

						if (openedNewTransaction) // UseTransaction && currentTransaction != null)
							currentTransaction?.Commit();

						return result;
					}
				}
				catch (MySqlException mysqlEx)
				{
					if (UseTransaction)
						currentTransaction?.Rollback();

					if (ThrowException)
						throw new Exception(mysqlEx.Message, mysqlEx);
					else
					{
						//LogHelper.Error(mysqlEx.GetType(), $"MySQL error", mysqlEx);
						LastExecutionError = mysqlEx.Message;

						return default(T);
					}
				}
				catch (Exception ex)
				{
					if (UseTransaction)
						currentTransaction?.Rollback();

					if (ThrowException)
						throw new Exception(ex.Message, ex);
					else
					{
						//LogHelper.Error(ex.GetType(), $"Error", ex);
						LastExecutionError = ex.Message;

						return default(T);
					}
				}
				finally
				{
					if (internalOpen && EstablishedConnection.State == ConnectionState.Open)
						EstablishedConnection.Close();
				}
			}
		}
	}
}