using DALHelperNet.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Extensions
{
    public static class ListExtensions
    {
        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, Enum ConnectionStringType) where T : DALBaseModel
        {
            return DALHelper.BulkTableWrite<T>(ConnectionStringType, DbModelData);
        }

        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null) where T : DALBaseModel
        {
            return DALHelper.BulkTableWrite<T>(ExistingConnection, DbModelData, SqlTransaction: SqlTransaction);
        }
    }
}
