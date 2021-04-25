using DALHelperNet.InternalClasses.Helpers.Operations;
using DALHelperNet.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers.DataTransfer
{
	internal class ObjectResultsHelper
	{
		internal static IEnumerable<T> GetDataList<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataList<T>(conn, QueryString, Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		internal static IEnumerable<T> GetDataList<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			return RefinedResultsHelper.GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables)
				.AsEnumerable()
				.Select(x => (T)DatabaseCoreUtilities.ConvertScalar<T>(x[0]));
		}

		internal static T GetDataObject<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataObject<T>(conn, QueryString, Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		internal static T GetDataObject<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			return GetDataObjects<T>(ExistingConnection, QueryString, Parameters, ThrowException, SqlTransaction, AllowUserVariables)
				.FirstOrDefault();
		}

		internal static IEnumerable<T> GetDataObjects<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataObjects<T>(conn, QueryString, Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables);
			}
		}

		internal static IEnumerable<T> GetDataObjects<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null, bool AllowUserVariables = false) where T : DALBaseModel
		{
			return RefinedResultsHelper.GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction, AllowUserVariables: AllowUserVariables)
				.AsEnumerable()
				.Select(x => x == null ? null : DatabaseCoreUtilities.CreateCreatorExpression<DataRow, string, T>()(x, null)); //return (T)Activator.CreateInstance(typeof(T), x, null);
		}
	}
}
