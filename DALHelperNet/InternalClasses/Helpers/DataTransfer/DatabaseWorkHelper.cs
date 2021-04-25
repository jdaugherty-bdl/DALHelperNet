using DALHelperNet.InternalClasses.Helpers.Operations;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers.DataTransfer
{
	internal class DatabaseWorkHelper
	{
		// caches the last execution error encountered
		internal static string LastExecutionError;
		// convenience function to check if there's an error cached
		internal static bool HasError => !string.IsNullOrEmpty(LastExecutionError);

		/// <summary>
		/// Execute a non-query on the database with the specified parameters without returning a value
		/// </summary>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="Parameters">Dictionary of named parameters</param>
		/// <param name="ThrowException">Throw swallow exception</param>
		internal static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
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
		internal static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
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
		internal static T DoDatabaseWork<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
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
		internal static T DoDatabaseWork<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
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

		/// <summary>
		/// Execute a query on the database using the provided function without returning a value
		/// </summary>
		/// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
		/// <param name="QueryString">SQL query to execute</param>
		/// <param name="ActionCallback">Customized function to execute when connected to the database</param>
		/// <param name="ThrowException">Throw or swallow exception</param>
		/// <param name="UseTransaction">Specify whether to use a transaction for this call</param>
		internal static void DoDatabaseWork(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
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
		internal static void DoDatabaseWork(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
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
		internal static T DoDatabaseWork<T>(Enum ConfigConnectionString, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false)
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
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
		internal static T DoDatabaseWork<T>(MySqlConnection EstablishedConnection, string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
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
