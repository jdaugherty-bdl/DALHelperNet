using DALHelperNet.Interfaces.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DALHelperNet.Extensions
{
    public static class UnderscoreNamesHelper
    {
        public static string UppercaseSearchPattern => @"(?<!_|^|Internal)([A-Z])";
        public static string ReplacePattern => @"_$1";


        //TODO: update ReplacePattern to include option for converting to lowercase everything EXCEPT "InternalId"


        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> ConvertPropertiesToUnderscoreNames(Type DataType, bool ForceLowerCase = false, bool GetOnlyDalResolvables = true)
        {
            // get all potential properties that can be converted to data
            var convertableProperties = DataType
                .GetProperties()
                .Where(x => !GetOnlyDalResolvables || x.GetCustomAttribute<DALResolvable>() != null) // x.GetCustomAttributes(true).Any(y => y.GetType() == typeof(DALResolvable)))
                .ToList();

            // get the underscore names and type info of the properties
            var convertableList = convertableProperties
                .ToDictionary(x => x.Name.StartsWith("InternalId") 
                        ? x.Name 
                        : Regex.Replace(x.Name, UppercaseSearchPattern, ReplacePattern), 
                    x => new Tuple<string, PropertyInfo>(x.Name, x))
                .Select(x => new KeyValuePair<string, Tuple<string, PropertyInfo>>(ForceLowerCase
                        ? x.Key.ToLower().Replace("internalid", "InternalId")
                        : x.Key,
                    x.Value))
                .ToList();

            return convertableList;
        }

        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> ConvertPropertiesToUnderscoreNames<T>(this T RowData, bool ForceLowerCase = false)
        {
            return ConvertPropertiesToUnderscoreNames(RowData.GetType(), ForceLowerCase: ForceLowerCase, GetOnlyDalResolvables: true);
        }
    }
}
