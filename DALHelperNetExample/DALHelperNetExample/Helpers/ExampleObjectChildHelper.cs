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
    public class ExampleObjectChildHelper
    {
        private static IEnumerable<ExampleObjectChild> GetExampleObjectChildren(string ExampleObjectChildInternalId = null, string ExampleObjectInternalId = null)
        {
            var exampleQuery = $@"SELECT * FROM {typeof(ExampleObjectChild).GetCustomAttribute<DALTable>()?.TableName ?? "example_object_children"}
                WHERE active = 1
                {(string.IsNullOrWhiteSpace(ExampleObjectInternalId) ? "#" : null)}AND example_object_InternalId = @example_object_InternalId
                {(string.IsNullOrWhiteSpace(ExampleObjectChildInternalId) ? "#" : null)}AND InternalId = @internal_id
                {(string.IsNullOrWhiteSpace(ExampleObjectChildInternalId) ? "#" : null)}LIMIT 1
                ;";

            return DALHelper.GetDataObjects<ExampleObjectChild>(ExampleConnectionStringTypes.FirstApplicationDatabase, exampleQuery, new Dictionary<string, object> 
            { 
                { "@example_object_InternalId", ExampleObjectInternalId },
                { "@internal_id", ExampleObjectChildInternalId }
            });
        }

        public static ExampleObjectChild GetExampleObjectChild(string ExampleObjectChildInternalId)
        {
            return GetExampleObjectChildren(ExampleObjectChildInternalId: ExampleObjectChildInternalId)
                .FirstOrDefault();
        }

        public static IEnumerable<ExampleObjectChild> GetExampleObjectChildren(string ExampleObjectInternalId)
        {
            return GetExampleObjectChildren(ExampleObjectChildInternalId: null, ExampleObjectInternalId: ExampleObjectInternalId);
        }

        public static IEnumerable<ExampleObjectChild> GetAllExampleObjectChildren()
        {
            return GetExampleObjectChildren();
        }

        public static int DeleteExampleObjectChild(string ExampleObjectChildId)
        {
            var deleteQuery = $@"UPDATE {typeof(ExampleObjectChild).GetCustomAttribute<DALTable>()?.TableName ?? "example_object_children"}
                    SET active = 0
                    WHERE InternalId = @internal_id;";

            var rowsUpdated = DALHelper.DoDatabaseWork<int>(ExampleConnectionStringTypes.FirstApplicationDatabase, deleteQuery, new Dictionary<string, object> { { "@internal_id", ExampleObjectChildId } });

            return rowsUpdated;
        }
    }
}
