using DALHelperNet.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers
{
    internal class ListOperations
    {
		public static IEnumerable<T> GetDataList<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataList<T>(conn, QueryString, Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		public static IEnumerable<T> GetDataList<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			return StandardAtomicFunctions.GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables)
				.AsEnumerable()
				.Select(x => (T)OperationsHelper.ConvertScalar<T>(x[0]));
		}

		public static T GetDataObject<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
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
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataObjects<T>(conn, QueryString, Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		public static IEnumerable<T> GetDataObjects<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			return StandardAtomicFunctions.GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables)
				.AsEnumerable()
				.Select(x => x == null ? null : OperationsHelper.CreateCreatorExpression<DataRow, string, T>()(x, null)); //return (T)Activator.CreateInstance(typeof(T), x, null);
		}
	}
}
