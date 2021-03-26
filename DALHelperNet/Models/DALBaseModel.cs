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

        public static string UppercaseSearchPattern => @"(?<!_|^|Internal)([A-Z])";
        public static string UnderscoreReplacePattern => @"_$1";

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

        public static List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscorePropertiesOfObject(object TargetObject, bool GetOnlyDbResolvables = true)
        {
            var convertableProperties = TargetObject
                .GetType()
                .GetProperties()
                .Where(x => GetOnlyDbResolvables
                    ? x.GetCustomAttributes(true).Any(y => new Type[] { typeof(DALResolvable) }.Contains(y.GetType()))
                    : true)
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

        public int WriteToDatabase(Enum ConnectionStringType)
        {
            return DALHelper.BulkTableWrite(ConnectionStringType, this, ForceType: this.GetType());
        }

        public int WriteToDatabase(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null)
        {
            return DALHelper.BulkTableWrite(ExistingConnection, this, SqlTransaction: SqlTransaction, ForceType: this.GetType());
        }

        private object GetValueData(string UnderscoreKey, Type PropertyValueType, DataRow ModelData)
        {
            object valueData;

            if (PropertyValueType == ModelData[UnderscoreKey].GetType() || ModelData[UnderscoreKey].GetType() == typeof(DateTime))
                valueData = ModelData[UnderscoreKey];

            else if (PropertyValueType.BaseType == typeof(Enum))
                valueData = Enum.Parse(PropertyValueType, ModelData[UnderscoreKey].ToString());

            else if (PropertyValueType == typeof(DateTime) && ModelData[UnderscoreKey].GetType() == typeof(string))
                valueData = DateTime.Parse(ModelData[UnderscoreKey].ToString());

            else
                valueData = JsonConvert.DeserializeObject(ModelData[UnderscoreKey].ToString(), PropertyValueType);

            return valueData;
        }
    }
}
