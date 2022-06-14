using DALHelperNet.Interfaces.Attributes;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Models.Properties
{
    public class DALPropertyType
    {
        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
        public PropertyInfo PropertyTypeInformation { get; set; }

        public string PropertyColumnType { get; private set; }
        public MySqlDbType PropertyMySqlDbType { get; private set; }
        public Type PropertyType { get; private set; }
        public int DefaultColumnSize { get; private set; }

        public DALResolvable ResolvableSettings { get; set; }

        // items first in list take precedence when converting from one tuple item to another - this list may look like it has duplicates, but it doesn't
        private static readonly IEnumerable<Tuple<string, MySqlDbType, Type, int>> MySqlTypeConverter = new List<Tuple<string, MySqlDbType, Type, int>>
        {
            new Tuple<string, MySqlDbType, Type, int>("bigint", MySqlDbType.Int64, typeof(long), 20),
            new Tuple<string, MySqlDbType, Type, int>("varchar", MySqlDbType.VarChar, typeof(string), 45),
            new Tuple<string, MySqlDbType, Type, int>("char", MySqlDbType.VarChar, typeof(string), 45),
            new Tuple<string, MySqlDbType, Type, int>("smallint", MySqlDbType.Int16, typeof(short), -1),
            new Tuple<string, MySqlDbType, Type, int>("int", MySqlDbType.Int32, typeof(int), -1),
            new Tuple<string, MySqlDbType, Type, int>("mediumint", MySqlDbType.Int24, typeof(int), -1),
            new Tuple<string, MySqlDbType, Type, int>("tinyint", MySqlDbType.Int16, typeof(short), 1),
            new Tuple<string, MySqlDbType, Type, int>("tinyint", MySqlDbType.Int16, typeof(bool), 1),
            new Tuple<string, MySqlDbType, Type, int>("tinyint", MySqlDbType.Int16, typeof(byte), 1),
            new Tuple<string, MySqlDbType, Type, int>("bit", MySqlDbType.Bit, typeof(byte), -1),
            new Tuple<string, MySqlDbType, Type, int>("datetime", MySqlDbType.DateTime, typeof(DateTime), -1),
            new Tuple<string, MySqlDbType, Type, int>("timestamp", MySqlDbType.Timestamp, typeof(DateTime), -1),
            new Tuple<string, MySqlDbType, Type, int>("blob", MySqlDbType.Blob, typeof(string), -1),
            new Tuple<string, MySqlDbType, Type, int>("decimal", MySqlDbType.Decimal, typeof(decimal), -1),
            new Tuple<string, MySqlDbType, Type, int>("double", MySqlDbType.Double, typeof(double), -1),
            new Tuple<string, MySqlDbType, Type, int>("float", MySqlDbType.Float, typeof(float), -1),
            new Tuple<string, MySqlDbType, Type, int>("guid", MySqlDbType.Guid, typeof(Guid), -1),
            new Tuple<string, MySqlDbType, Type, int>("text", MySqlDbType.Text, typeof(string), -1),
            new Tuple<string, MySqlDbType, Type, int>("longtext", MySqlDbType.LongText, typeof(string), -1),
            new Tuple<string, MySqlDbType, Type, int>("time", MySqlDbType.Time, typeof(DateTime), -1),
            new Tuple<string, MySqlDbType, Type, int>("date", MySqlDbType.Date, typeof(DateTime), -1),
            new Tuple<string, MySqlDbType, Type, int>("varchar", MySqlDbType.VarChar, typeof(object), 45),
            new Tuple<string, MySqlDbType, Type, int>("json", MySqlDbType.JSON, typeof(object), -1),
            new Tuple<string, MySqlDbType, Type, int>("varchar", MySqlDbType.VarChar, typeof(TimeSpan), 45)
        };

        public static implicit operator string(DALPropertyType Source) => MySqlTypeConverter.Where(x => x.Item1 == Source.PropertyColumnType).FirstOrDefault().Item1;
        public static implicit operator MySqlDbType(DALPropertyType Source) => MySqlTypeConverter.Where(x => x.Item2 == Source.PropertyMySqlDbType).FirstOrDefault().Item2;
        public static implicit operator Type(DALPropertyType Source) => MySqlTypeConverter.Where(x => x.Item3 == Source.PropertyType).FirstOrDefault().Item3;

        public static explicit operator DALPropertyType(string Source) => new DALPropertyType(Source);
        public static explicit operator DALPropertyType(MySqlDbType Source) => new DALPropertyType(Source);
        public static explicit operator DALPropertyType(Type Source) => new DALPropertyType(Source);

        public override string ToString() => $"[{PropertyColumnType} | {PropertyMySqlDbType} | {PropertyType}]";

        public override bool Equals(object SourcePropertyColumnName)
        {
            if (SourcePropertyColumnName.GetType() == typeof(string))
                return PropertyColumnType == (string)SourcePropertyColumnName;

            if (SourcePropertyColumnName.GetType() == typeof(MySqlDbType))
                return PropertyMySqlDbType == (MySqlDbType)SourcePropertyColumnName;

            if (SourcePropertyColumnName.GetType() == typeof(Type))
                return PropertyType == (Type)SourcePropertyColumnName;

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public DALPropertyType(string SourcePropertyColumnName)
        {
            PropertyColumnType = SourcePropertyColumnName;
            PropertyMySqlDbType = ColumnNameToColumnType(PropertyColumnType);
            PropertyType = ColumnNameToPropertyType(PropertyColumnType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyColumnName);
        }

        public DALPropertyType(MySqlDbType SourcePropertyColumnType)
        {
            PropertyMySqlDbType = SourcePropertyColumnType;
            PropertyColumnType = ColumnTypeToColumnName(PropertyMySqlDbType);
            PropertyType = ColumnTypeToPropertyType(PropertyMySqlDbType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyColumnType);
        }

        public DALPropertyType(Type SourcePropertyType)
        {
            PropertyType = SourcePropertyType;

            if (SourcePropertyType.GetInterface(typeof(ICollection<>).Name) != null)
                PropertyType = typeof(string);

            PropertyColumnType = PropertyTypeToColumnName(PropertyType);
            PropertyMySqlDbType = PropertyTypeToColumnType(PropertyType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyType);
        }

        public static MySqlDbType ColumnNameToColumnType(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item2 ?? default;
        public static MySqlDbType PropertyTypeToColumnType(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == PropertyType).FirstOrDefault()?.Item2 ?? default;
        public static string ColumnTypeToColumnName(MySqlDbType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item1;
        public static string PropertyTypeToColumnName(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == PropertyType).FirstOrDefault()?.Item1;
        public static Type ColumnNameToPropertyType(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item3;
        public static Type ColumnTypeToPropertyType(MySqlDbType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item3;

        public static int GetDefaultColumnSize(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item4 ?? default;
        public static int GetDefaultColumnSize(MySqlDbType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item4 ?? default;
        public static int GetDefaultColumnSize(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == PropertyType).FirstOrDefault()?.Item4 ?? default;
    }
}
