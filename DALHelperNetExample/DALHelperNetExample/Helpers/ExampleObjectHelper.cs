using DALHelperNet;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNetExample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample.Helpers
{
    public class ExampleObjectHelper
    {
        private static IEnumerable<ExampleObject> GetExampleObjects(string ExampleObjectInternalId = null)
        {
            var typeQuery = $@"SELECT * FROM {typeof(ExampleObject).GetCustomAttribute<DALTable>().TableName ?? "example_objects"}
                WHERE active = 1
                {(string.IsNullOrWhiteSpace(ExampleObjectInternalId) ? "#" : null)}AND InternalId = @internal_id
                {(string.IsNullOrWhiteSpace(ExampleObjectInternalId) ? "#" : null)}LIMIT 1
                ;";

            return DALHelper.GetDataObjects<ExampleObject>(ExampleConnectionStringTypes.FirstApplicationDatabase, typeQuery, new Dictionary<string, object> { { "@internal_id", ExampleObjectInternalId } });
        }

        public static ExampleObject GetExampleObject(string ExampleObjectInternalId)
        {
            return GetExampleObjects(ExampleObjectInternalId: ExampleObjectInternalId)
                .FirstOrDefault();
        }

        public static IEnumerable<ExampleObject> GetAllExampleObjects()
        {
            return GetExampleObjects();
        }

        public static int DeleteExampleObject(string ExampleObjectId)
        {
            var deleteQuery = $@"UPDATE {typeof(ExampleObject).GetCustomAttribute<DALTable>()?.TableName ?? "example_objects"}
                    SET active = 0
                    WHERE InternalId = @internal_id;";

            var rowsUpdated = DALHelper.DoDatabaseWork<int>(ExampleConnectionStringTypes.FirstApplicationDatabase, deleteQuery, new Dictionary<string, object> { { "@internal_id", ExampleObjectId } });

            return rowsUpdated;
        }
    }
}
