using DALHelperNet.Interfaces.Attributes;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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
        public string InternalId { get; set; }
        [DALResolvable]
        public DateTime CreateDate { get; set; }
        [DALResolvable]
        public DateTime LastUpdated { get; set; }

        // The regular express search and replace strings to turn CapitalCase property names into underscore_case column names
        public static string UppercaseSearchPattern => @"(?<!_|^|Internal)([A-Z])";
        public static string UnderscoreReplacePattern => @"_$1";

        /// <summary>
        /// Resets every property to its default value
        /// </summary>
        private void ResetCoreAttributes()
        {
            Active = true;

            InternalId = Guid.NewGuid().ToString();

            CreateDate = DateTime.Now;
            LastUpdated = CreateDate;

        }

        public DALBaseModel()
        {
            ResetCoreAttributes();
        }

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
            var convertableProperties = TargetObject
                .GetType()
                .GetProperties()
                .Where(x => !GetOnlyDbResolvables || x.GetCustomAttributes(true).Any(y => new Type[] { typeof(DALResolvable) }.Contains(y.GetType())))
                .ToList();

            var underscoreNames = convertableProperties
                .ToDictionary(x => x.Name.StartsWith("InternalId") ? x.Name : Regex.Replace(x.Name, UppercaseSearchPattern, UnderscoreReplacePattern), x => new Tuple<string, PropertyInfo>(x.Name, x))
                .ToList();

            return underscoreNames;
        }

        private static string ThisTypeName => typeof(DALBaseModel).Name;

        public DALBaseModel(DataRow ModelData, string AlternateTableName = null)
        {
            ResetCoreAttributes();

            // match up all properties to columns using underscore names and populate matches with data from the row
            foreach (var underscoreName in GetUnderscoreProperties())
            {
                // first do the default column names
                if (ModelData.Table.Columns.Contains(underscoreName.Key) && !(ModelData[underscoreName.Key] is DBNull))
                    underscoreName.Value.Item2.SetValue(this, GetValueData(underscoreName.Key, underscoreName.Value.Item2.PropertyType, ModelData));

                // then do the alternate table names
                if (ModelData.Table.Columns.Contains($"{underscoreName.Key}_{AlternateTableName ?? ThisTypeName}") && !(ModelData[$"{underscoreName.Key}_{AlternateTableName ?? ThisTypeName}"] is DBNull))
                    underscoreName.Value.Item2.SetValue(this, GetValueData($"{underscoreName.Key}_{AlternateTableName ?? ThisTypeName}", underscoreName.Value.Item2.PropertyType, ModelData));
            }
        }

        public DALBaseModel(DALBaseModel FromModel)
        {
            this.Active = FromModel?.Active ?? false;
            this.InternalId = FromModel?.InternalId;
            this.CreateDate = FromModel?.CreateDate ?? default;
            this.LastUpdated = FromModel?.LastUpdated ?? default;
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

            // if none of those are true, then we have some serialized JSON data, so deserialize it
            else
                valueData = JsonConvert.DeserializeObject(ModelData[UnderscoreKey].ToString(), PropertyValueType);

            return valueData;
        }
    }
}
