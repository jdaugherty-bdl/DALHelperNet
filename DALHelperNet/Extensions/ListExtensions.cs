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

        public static IEnumerable<T> FlattenTreeObject<T>(this IEnumerable<T> EnumerableList, Func<T, IEnumerable<T>> GetChildrenFunction)
        {
            return EnumerableList
                .SelectMany(enumerableItem =>
                    Enumerable
                    .Repeat(enumerableItem, 1)
                    .Concat(GetChildrenFunction(enumerableItem)
                        ?.FlattenTreeObject(GetChildrenFunction)
                        ??
                        Enumerable.Empty<T>()));
        }
    }
}
