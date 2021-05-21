using DALHelperNet.Extensions;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.InternalClasses.Helpers;
using DALHelperNet.Models.Properties;
using MoreLinq;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Models
{
    public class WritableTableDefinition<T>
    {
        private string _databaseName;

        public string TableName { get; private set; }
        public Type TableType { get; private set; }

        public string DatabaseName 
        { 
            get
            {
                return _databaseName;
            }
            set
            {
                _databaseName = value.MySqlObjectQuote();
            }
        }
        public IEnumerable<DALPropertyType> TableProperties { get; set; }
        public Dictionary<TriggerTypes, DALTrigger<T>> Triggers { get; set; }

        internal WritableTableDefinition()
        {
            TableType = typeof(T);
            TableName = TableType
                .GetCustomAttribute<DALTable>()?.TableName?.MySqlObjectQuote()
                ?? 
                throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError);

            //TODO: check if table exists and either truncate or drop as necessary

            var resolvableProperties = TableType
                .GetProperties()
                .Where(x => x.GetCustomAttribute<DALResolvable>() != null);

            if (resolvableProperties.Count() == 0)
                throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalPropertyAttributeError);

            // get properties from object, convert to underscore names
            TableProperties = UnderscoreNamesHelper
                .ConvertPropertiesToUnderscoreNames(TableType, ForceLowerCase: true, GetOnlyDalResolvables: true)
                .Select(x => new DALPropertyType(x.Value.Item2.PropertyType)
                {
                    ColumnName = x.Key.MySqlObjectQuote(),
                    PropertyName = x.Value.Item1,
                    PropertyTypeInformation = x.Value.Item2,
                    ResolvableSettings = x.Value.Item2.GetCustomAttribute<DALResolvable>()
                });
        }

        public bool ClearAllTriggers()
        {
            Triggers = new Dictionary<TriggerTypes, DALTrigger<T>>();

            return true;
        }

        public WritableTableDefinition<T> SetTrigger(TriggerTypes TriggerType, string TriggerBody)
        {
            return AppendTriggerData(TriggerType, TriggerBody, AppendTriggerBody: false);
        }

        public WritableTableDefinition<T> AppendTrigger(TriggerTypes TriggerType, string TriggerBody)
        {
            return AppendTriggerData(TriggerType, TriggerBody, AppendTriggerBody: true);
        }

        private WritableTableDefinition<T> AppendTriggerData(TriggerTypes TriggerType, string TriggerBody, bool AppendTriggerBody = true)
        {
            var trigger = new DALTrigger<T>(TriggerType, TriggerBody)
            {
                DatabaseName = this.DatabaseName
            };

            Triggers = Triggers ?? new Dictionary<TriggerTypes, DALTrigger<T>>();

            if (Triggers.ContainsKey(TriggerType))
            {
                if (AppendTriggerBody) 
                    trigger.TriggerBody = $"{Triggers[TriggerType].TriggerBody};{Environment.NewLine}{trigger.TriggerBody}"; 

                Triggers[TriggerType] = trigger;
            }
            else
                Triggers.Add(TriggerType, trigger);

            return this;
        }

        public override string ToString()
        {
            var createTableStatement = new StringBuilder();
            createTableStatement.Append("CREATE TABLE ");

            if (!string.IsNullOrWhiteSpace(DatabaseName))
                createTableStatement.Append($"{DatabaseName}.");

            createTableStatement.Append(TableName);
            createTableStatement.Append(" (");

            // do columns
            createTableStatement.Append(string
                .Join(",", TableProperties
                    //.Select(x => new KeyValuePair<string, Tuple<DALPropertyType, DALResolvable>>(x.Key, new Tuple<DALPropertyType, DALResolvable>(x.Value, x.Value.PropertyTypeInformation.GetCustomAttribute<DALResolvable>())))
                    .Select(x => new StringBuilder()
                        .AppendLine()
                        .Append("\t")
                        .Append(x.ColumnName)
                        .Append(" ")
                        .Append(x.PropertyColumnType)
                        .Append(x.ResolvableSettings.ColumnSize != -1 || x.ResolvableSettings.CompoundColumnSize != null
                            ? $"({string.Join(",", x.ResolvableSettings.ColumnSize == -1 ? x.ResolvableSettings.CompoundColumnSize : new int[] { x.ResolvableSettings.ColumnSize })})"
                            : x.DefaultColumnSize != -1
                                ? $"({x.DefaultColumnSize})"
                                : x == MySqlDbType.VarChar
                                    ? throw new ArgumentException($"Cannot create table, error in DALResolvable attribute on [{TableType.Name}.{x.PropertyName}]: '{x.PropertyColumnType}' requires a column size.")
                                    : string.Empty)
                        .Append(" ")
                        .Append(x.ResolvableSettings.Unique ? "UNIQUE " : string.Empty)
                        .Append(!x.ResolvableSettings.IsNullable ? "NOT " : string.Empty)
                        .Append("NULL")
                        .Append(x.ResolvableSettings.Autonumber ? " AUTO_INCREMENT" : string.Empty)
                        .Append(!string.IsNullOrWhiteSpace(x.ResolvableSettings.DefaultValue) ? $" DEFAULT {x.ResolvableSettings.DefaultValue}" : string.Empty)
                        .ToString())));

            // do primary key
            var primaryKey = TableProperties.Where(x => x.ResolvableSettings?.PrimaryKey ?? false).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(primaryKey?.ColumnName))
            {
                createTableStatement.Append(",");
                createTableStatement.AppendLine();
                createTableStatement.Append($"\tPRIMARY KEY ({primaryKey.ColumnName}),");
            }

            // do indexes
            var indexKeys = TableProperties.Where(x => !string.IsNullOrWhiteSpace(x.ResolvableSettings?.Index));
            if (indexKeys.Count() > 0)
            {
                createTableStatement
                    .Append(string
                        .Join(",", indexKeys
                            .Select(x => new Tuple<string, bool, string>(x.ResolvableSettings?.Index?.MySqlObjectQuote(), x.ResolvableSettings?.IndexDescending ?? false, x.ColumnName))
                            .Segment((previous, next, index) =>
                            {
                                return previous.Item1 != next.Item1;
                            })
                            .Select(x => new StringBuilder()
                                .AppendLine()
                                .Append("\tINDEX ")
                                .Append(x.FirstOrDefault().Item1)
                                .Append(" (")
                                .Append(string.Join(", ", x.Select(y => new StringBuilder()
                                    .Append(y.Item3)
                                    .Append(" ")
                                    .Append(y.Item2 ? "DESC" : "ASC")
                                    .ToString())))
                                .Append(")")
                                .ToString())));
            }

            createTableStatement.AppendLine(");");
            createTableStatement.AppendLine();

            // one last chance to set the database name
            Triggers.ForEach(x => x.Value.DatabaseName = DatabaseName);

            createTableStatement.Append(string.Join(Environment.NewLine, Triggers.Select(x => x.Value.ToString())));

            return createTableStatement.ToString();
        }
    }
}
