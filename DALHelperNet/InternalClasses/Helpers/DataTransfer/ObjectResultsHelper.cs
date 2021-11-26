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
		internal static IEnumerable<T> GetDataList<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) //where T : DALBaseModel
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataList<T>(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException);
			}
		}

		internal static IEnumerable<T> GetDataList<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) //where T : DALBaseModel
		{
			return RefinedResultsHelper.GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction)
				.AsEnumerable()
				.Select(x => (T)DatabaseCoreUtilities.ConvertScalar<T>(x[0]));
		}

		internal static T GetDataObject<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataObject<T>(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException);
			}
		}

		internal static T GetDataObject<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) where T : DALBaseModel
		{
			return GetDataObjects<T>(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction)
				.FirstOrDefault();
		}

		internal static IEnumerable<T> GetDataObjects<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : DALBaseModel
		{
			using (var conn = ConnectionHelper.GetConnectionFromString(ConfigConnectionString, AllowUserVariables))
			{
				return GetDataObjects<T>(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException);
			}
		}

		internal static IEnumerable<T> GetDataObjects<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) where T : DALBaseModel
		{
			return RefinedResultsHelper.GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction)
				.AsEnumerable()
				.Select(x => x == null 
					? null 
					: typeof(T).GetConstructors().Any(y => y.GetParameters().Length > 2)
						? DatabaseCoreUtilities.CreateCreatorExpression<DataRow, string, bool, bool, T>()(x, null, false, false)
						: DatabaseCoreUtilities.CreateCreatorExpression<DataRow, string, T>()(x, null));
		}
	}
}
