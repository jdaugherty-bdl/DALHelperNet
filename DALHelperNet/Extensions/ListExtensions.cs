using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static IEnumerable<dynamic> GenerateDTO<T>(this IEnumerable<T> BaseObjects, IEnumerable<string> IncludeProperties = null, IEnumerable<string> ExcludeProperties = null) where T : DALBaseModel
        {
            return BaseObjects.Select(x => x.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties));
        }

        public static string GetDalTable<T>(this T @this) where T : DALBaseModel
        {
            return typeof(T).GetCustomAttribute<DALTable>()?.TableName;
        }
    }
}
