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
    public class ExampleBasicObjectHelper
    {
        private static IEnumerable<ExampleBasicObject> GetExampleBasicObjects(string ExampleBasicObjectInternalId = null)
        {
            var exampleQuery = $@"SELECT * FROM {typeof(ExampleBasicObject).GetCustomAttribute<DALTable>()?.TableName ?? "example_basic_objects"}
                WHERE active = 1
                {(string.IsNullOrWhiteSpace(ExampleBasicObjectInternalId) ? "#" : null)}AND InternalId = @internal_id
                {(string.IsNullOrWhiteSpace(ExampleBasicObjectInternalId) ? "#" : null)}LIMIT 1
                ;";

            return DALHelper.GetDataObjects<ExampleBasicObject>(ExampleConnectionStringTypes.FirstApplicationDatabase, exampleQuery, new Dictionary<string, object> { { "@internal_id", ExampleBasicObjectInternalId } });
        }

        public static ExampleBasicObject GetExampleBasicObject(string ExampleBasicObjectInternalId)
        {
            return GetExampleBasicObjects(ExampleBasicObjectInternalId: ExampleBasicObjectInternalId)
                .FirstOrDefault();
        }

        public static IEnumerable<ExampleBasicObject> GetAllExampleBasicObjects()
        {
            return GetExampleBasicObjects();
        }

        public static int DeleteExampleBasicObject(string ExampleBasicObjectId)
        {
            var deleteQuery = $@"UPDATE {typeof(ExampleBasicObject).GetCustomAttribute<DALTable>()?.TableName ?? "example_basic_objects"}
                    SET active = 0
                    WHERE InternalId = @internal_id;";

            var rowsUpdated = DALHelper.DoDatabaseWork<int>(ExampleConnectionStringTypes.FirstApplicationDatabase, deleteQuery, new Dictionary<string, object> { { "@internal_id", ExampleBasicObjectId } });

            return rowsUpdated;
        }
    }
}
