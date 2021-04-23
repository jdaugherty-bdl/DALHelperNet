using DALHelperNet.Extensions;
using DALHelperNet.Interfaces.Attributes;
using MoreLinq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DALHelperNet.Models
{
    public class DALBaseModel
    {
        [DALResolvable]
        public bool Active { get; set; }
        [DALResolvable]
        [DALTransferProperty]
        public string InternalId { get; set; }
        [DALResolvable]
        public DateTime CreateDate { get; set; }
        [DALResolvable]
        public DateTime LastUpdated { get; set; }

        // The regular expression search and replace strings to turn "CapitalCase" property names into "underscore_case" column names
        public static string UnderscoreSearchPattern => @"(?<!_|^|Internal)([A-Z])";
        public static string UnderscoreReplacePattern => @"_$1";

        /// <summary>
        /// Resets every property to its default value
        /// </summary>
        public DALBaseModel ResetCoreAttributes()
        {
            Active = true;

            InternalId = Guid.NewGuid().ToString();

            CreateDate = DateTime.Now;
            LastUpdated = CreateDate;

            return this;
        }

        public DALBaseModel()
        {
            ResetCoreAttributes();
        }

        public DALBaseModel(DALBaseModel FromModel)
        {
            this.Active = FromModel?.Active ?? false;
            this.InternalId = FromModel?.InternalId;
            this.CreateDate = FromModel?.CreateDate ?? default;
            this.LastUpdated = FromModel?.LastUpdated ?? default;
        }

        private static string ThisTypeName => typeof(DALBaseModel).Name;

        /// <summary>
        /// Creates an object and automatcially places data from a database row into it based on naming conventions.
        /// </summary>
        /// <param name="ModelData">Row of data from the database.</param>
        /// <param name="AlternateTableName">The alternate table name to search for in data results.</param>
        public DALBaseModel(DataRow ModelData, string AlternateTableName = null)
        {
            var alternateTableName = AlternateTableName ?? GetType().Name;

            ResetCoreAttributes();

            // match up all properties to columns using underscore names and populate matches with data from the row
            foreach (var underscoreName in GetUnderscoreProperties())
            {
                //TODO: replace Contains(underscoreName.Key) both places below with "IndexOf(underscoreName.Key, StringComparison.InvariantCultureIgnoreCase) >= 0"? Not sure if we care about case.

                // first do the default column names
                if (ModelData.Table.Columns.Contains(underscoreName.Key) && !(ModelData[underscoreName.Key] is DBNull) && underscoreName.Value.Item2.SetMethod != null)
                    underscoreName.Value.Item2.SetValue(this, GetValueData(underscoreName.Key, underscoreName.Value.Item2.PropertyType, ModelData));

                // then do the alternate table names
                if (ModelData.Table.Columns.Contains($"{underscoreName.Key}_{alternateTableName ?? ThisTypeName}") && !(ModelData[$"{underscoreName.Key}_{alternateTableName ?? ThisTypeName}"] is DBNull) && underscoreName.Value.Item2.SetMethod != null)
                    underscoreName.Value.Item2.SetValue(this, GetValueData($"{underscoreName.Key}_{alternateTableName ?? ThisTypeName}", underscoreName.Value.Item2.PropertyType, ModelData));
            }
        }

        /// <summary>
        /// Gets the data from the named column in the DataRow and properly parses/converts it based on Type factors.
        /// </summary>
        /// <param name="UnderscoreKey">Underscore name of the column.</param>
        /// <param name="PropertyValueType">Type of the property, used for parsing/conversion.</param>
        /// <param name="ModelData">Raw database data row.</param>
        /// <returns>The processed data.</returns>
        private object GetValueData(string UnderscoreKey, Type PropertyValueType, DataRow ModelData)
        {
            object valueData;

            // most primitive types are just 1:1 passthrough and don't require post-processing
            if (PropertyValueType == ModelData[UnderscoreKey].GetType() || ModelData[UnderscoreKey].GetType() == typeof(DateTime))
                valueData = ModelData[UnderscoreKey];

            // if it's an Enum, do a parse
            else if (PropertyValueType.BaseType == typeof(Enum))
                valueData = Enum.Parse(PropertyValueType, ModelData[UnderscoreKey].ToString());

            // if we're putting it in a DateTime, but we have a string, parse it
            else if (PropertyValueType == typeof(DateTime) && ModelData[UnderscoreKey].GetType() == typeof(string))
                valueData = DateTime.TryParse(ModelData[UnderscoreKey].ToString(), out DateTime _dateData) ? _dateData : default;

            // if we're putting it in a TimeSpan, but we have a string, parse it
            else if (PropertyValueType == typeof(TimeSpan) && ModelData[UnderscoreKey].GetType() == typeof(string))
                valueData = TimeSpan.TryParse(ModelData[UnderscoreKey].ToString(), out TimeSpan _timeSpanData) ? _timeSpanData : default;

            // if none of those are true, then we have some serialized JSON data, so deserialize it
            else
                valueData = JsonConvert.DeserializeObject(ModelData[UnderscoreKey].ToString(), PropertyValueType);

            return valueData;
        }

        /// <summary>
        /// Gets the full info about the current object's properties, including the underscore names.
        /// </summary>
        /// <param name="GetOnlyDbResolvables">Indicate to get only properties marked with the DALResolvable attribute.</param>
        /// <returns>The full list of property info including underscore names.</returns>
        public List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(bool GetOnlyDbResolvables = true)
        {
            return GetUnderscorePropertiesOfObject(this, GetOnlyDbResolvables);
        }

        /// <summary>
        /// Takes in an object and gets the full info about its properties, including the underscore names.
        /// </summary>
        /// <param name="TargetObject">The object to pull the properties from.</param>
        /// <param name="GetOnlyDbResolvables">Indicate to get only properties marked with the DALResolvable attribute.</param>
        /// <returns>The full list of property info including underscore names.</returns>
        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscorePropertiesOfObject(object TargetObject, bool GetOnlyDbResolvables = true)
        {
            //TODO: investigate whether it makes sense to look for the DALResolvable attribute or to just attempt to convert all class properties
            // get all properties marked with the DALResolvable attribute
            var convertableProperties = TargetObject
                .GetType()
                .GetProperties()
                .Where(x => !GetOnlyDbResolvables || x.GetCustomAttributes(true).Any(y => new Type[] { typeof(DALResolvable) }.Contains(y.GetType())))
                .ToList();

            // get the underscore names of all properties
            var underscoreNames = convertableProperties
                .ToDictionary(x => x.Name.StartsWith("InternalId") ? x.Name : Regex.Replace(x.Name, UnderscoreSearchPattern, UnderscoreReplacePattern), x => new Tuple<string, PropertyInfo>(x.Name, x))
                .ToList();

            return underscoreNames;
        }

        /// <summary>
        /// Writes the current object to the database using the table named in the DALTable attribute.
        /// </summary>
        /// <param name="ConnectionStringType">Type of connection to use.</param>
        /// <returns>The number of rows written to the database.</returns>
        public int WriteToDatabase(Enum ConnectionStringType)
        {
            return DALHelper.BulkTableWrite(ConnectionStringType, this, ForceType: this.GetType());
        }

        /// <summary>
        /// Writes the current object to the database using the table named in the DALTable attribute.
        /// </summary>
        /// <param name="ExistingConnection">An existing and open connection to use when writing this data.</param>
        /// <param name="SqlTransaction">An optional transaction to write to the database under.</param>
        /// <returns>The number of rows written to the database.</returns>
        public int WriteToDatabase(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null)
        {
            return DALHelper.BulkTableWrite(ExistingConnection, this, SqlTransaction: SqlTransaction, ForceType: this.GetType());
        }

        /// <summary>
        /// Generate a DTO POCO object based on properties marked with a DALTransferProperty attribute, plus any requested included properties, minus any requested excluded properties.
        /// If no DALTransferProperty attributes are found on a child object, this function will just include all properties from the child object.
        /// </summary>
        /// <param name="IncludeProperties">A list of properties to include in the DTO, even if they aren't marked with DALTransferProperty.</param>
        /// <param name="ExcludeProperties">A list of properties to exclude from the DTO, even if they are marked with DALTransferProperty.</param>
        /// <returns>A serializable object with only the requested properties included.</returns>
        public dynamic GenerateDTO(IEnumerable<string> IncludeProperties = null, IEnumerable<string> ExcludeProperties = null)
        {
            var baseRef = this;

            var namespaceIterations = baseRef
                .GetType()
                .FullName
                .Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            // get object properties, if any are DALBaseModels marked with DALTransferProperty then GenerateDTO() on those recursively, otherwise just return the value. if there are any IEnumerables, DTO each item in the enumerable.
            return (ExpandoObject)baseRef
                .GetType()
                .GetRuntimeProperties()
                .Select(x => new KeyValuePair<PropertyInfo, IEnumerable<string>>(x, namespaceIterations.Select((y, index) => string.Join(".", namespaceIterations.Skip(index).Append(x.Name))).Append(x.Name)))
                .Where(x => (x.Key.GetCustomAttribute<DALTransferProperty>() != null 
                        || ((IncludeProperties?.Intersect(x.Value, StringComparer.InvariantCultureIgnoreCase)?.Count() ?? 0) > 0))
                    && !((ExcludeProperties?.Intersect(x.Value, StringComparer.InvariantCultureIgnoreCase).Count() ?? 0) > 0))
                .Select(x => x.Key)
                .Aggregate(new ExpandoObject() as IDictionary<string, object>, 
                    (seed, property) =>
                    {
                        // look for enumerables, DTO each item within
                        if (property
                                .PropertyType
                                .GetInterfaces()
                                .Intersect(typeof(IEnumerable<>)
                                    .GetInterfaces())
                                .Count() 
                                > 0
                            &&
                            property
                                .PropertyType
                                .GenericTypeArguments
                                .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) 
                                    ? null 
                                    : new Type[] { x.BaseType })
                                .Contains(typeof(DALBaseModel)))
                        {
                            seed.Add(property.Name, 
                                ((IEnumerable<DALBaseModel>)property
                                    .GetValue(baseRef))
                                    .Select(x => x.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties)));
                        }
                        else
                        {
                            /* FOR DEBUGGING
                            var hasTransfer = property
                                    .PropertyType
                                    ?.GetRuntimeProperties()
                                    ?.Any(x => x.GetCustomAttribute<DALTransferProperty>() != null);

                            var isBaseModel = new Type[] { property.PropertyType }
                                    .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                                    .Contains(typeof(DALBaseModel));

                            var fieldValue = property.GetValue(baseRef);
                            if ((hasTransfer ?? false) && isBaseModel)
                                fieldValue = ((DALBaseModel)property.GetValue(baseRef))?.GenerateDTO(BaseRef: baseRef, IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties);

                            seed.Add(property.Name, fieldValue);
                            */
                            // convert a property
                            seed.Add(property.Name,
                                (property
                                    .PropertyType
                                    ?.GetRuntimeProperties()
                                    ?.Any(x => x.GetCustomAttribute<DALTransferProperty>() != null)
                                    ??
                                    false)
                                &&
                                new Type[] { property.PropertyType }
                                    .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                                    .Contains(typeof(DALBaseModel))
                                ? ((DALBaseModel)property.GetValue(baseRef))?.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties)
                                : property.GetValue(baseRef));
                        }
                        return seed;
                    });
        }

        private static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            // base case:
            IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };
            foreach (var sequence in sequences)
            {
                var s = sequence; // don't close over the loop variable
                                  // recursive case: use SelectMany to build the new product out of the old one
                result =
                    from seq in result
                    from item in s
                    select seq.Concat(new[] { item });
            }
            return result;
        }

        public DALBaseModel Duplicate()
        {
            return (DALBaseModel)this.MemberwiseClone();
        }
    }
}
