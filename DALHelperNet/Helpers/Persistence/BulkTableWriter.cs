using DALHelperNet.Models.Internal;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DALHelperNet.Helpers.Persistence
{
    public class BulkTableWriter<T>
    {
        // config
        private readonly int DEFAULT_BATCH_SIZE = 5;

        // exposed by functional methods
        private string InsertQuery;
        private string TableName;
        private bool WriteWithTransaction; // awkward name here so we can use the nice name for the set method below
        private bool ShouldThrowException; // awkward name here so we can use the nice name for the set method below
        private Dictionary<string, Tuple<MySqlDbType, int, string>> TableColumns;
        private int BatchSize;
        private IEnumerable<T> SourceData;
        private MySqlTransaction SqlTransaction;
        private MySqlConnection ExistingConnection;

        // local objects
        private readonly Enum ConfigConnectionString;
        private DataTable OutputTable;
        private IEnumerable<string> ColumnNames;

        internal BulkTableWriter() { }

        internal BulkTableWriter(Enum ConfigConnectionString, string InsertQuery = null, bool WriteWithTransaction = false, bool ShouldThrowException = true, MySqlTransaction SqlTransaction = null)
        {
            this.ConfigConnectionString = ConfigConnectionString;

            CommonSetup(InsertQuery, WriteWithTransaction, ShouldThrowException, SqlTransaction);
        }

        internal BulkTableWriter(MySqlConnection ExistingConnection, string InsertQuery = null, bool WriteWithTransaction = false, bool ShouldThrowException = true, MySqlTransaction SqlTransaction = null)
        {
            this.ExistingConnection = ExistingConnection;

            CommonSetup(InsertQuery, WriteWithTransaction, ShouldThrowException, SqlTransaction);
        }

        private void CommonSetup(string InsertQuery, bool WriteWithTransaction, bool ShouldThrowException, MySqlTransaction SqlTransaction)
        {
            this.InsertQuery = InsertQuery;
            this.WriteWithTransaction = WriteWithTransaction;
            this.ShouldThrowException = ShouldThrowException;
            this.SqlTransaction = SqlTransaction;

            BatchSize = DEFAULT_BATCH_SIZE;
        }

        /// <summary>
        /// Will bulk write data to the database. Assumes a list of objects with "underscorable" property names that will be auto-resolved into their relevant table columns.
        /// </summary>
        /// <returns>The number of rows affected by this bulk write.</returns>
        public int Write()
        {
            return Write(null);
        }

        /// <summary>
        /// Will bulk write data to the database.
        /// </summary>
        /// <param name="DataTableFunction">A function that takes in an object and a column name, and resolves those two pieces of information into a piece of data.</param>
        /// <returns>The number of rows affected by this bulk write.</returns>
        public int Write(Func<string, T, object> DataTableFunction)
        {
            PopulateColumnDetails();

            CreateOutputDataTable(DataTableFunction);

            var recordsInserted = -1;

            if (ExistingConnection != null)
                recordsInserted = DALHelper.DoDatabaseWork<int>(ExistingConnection, InsertQuery, CommonDatabaseWork, UseTransaction: WriteWithTransaction, ThrowException: ShouldThrowException, SqlTransaction: SqlTransaction);
            else
                recordsInserted = DALHelper.DoDatabaseWork<int>(ConfigConnectionString, InsertQuery, CommonDatabaseWork, UseTransaction: WriteWithTransaction, ThrowException: ShouldThrowException, SqlTransaction: SqlTransaction);

            return recordsInserted;
        }

        private object CommonDatabaseWork(MySqlCommand CommandObject)
        {
            CommandObject.UpdatedRowSource = UpdateRowSource.None;

            foreach (var column in TableColumns)
            {
                CommandObject.Parameters.Add($"@{column.Key}", column.Value.Item1, column.Value.Item2, column.Key);
            }

            // Specify the number of records to be Inserted/Updated in one go. Default is 1.
            var adpt = new MySqlDataAdapter
            {
                InsertCommand = CommandObject,
                UpdateBatchSize = BatchSize
            };

            return adpt.Update(OutputTable);
        }

        private void PopulateColumnDetails()
        {
            if ((TableColumns?.Count ?? 0) == 0 || string.IsNullOrWhiteSpace(InsertQuery))
            {
                if (string.IsNullOrWhiteSpace(TableName))
                    throw new ArgumentNullException("Error auto-populating Bulk Table Writer call: table name not defined");

                var currentTableDetails = DALHelper.GetDataObjects<DALTableRowDescriptor>(ConfigConnectionString, $"DESCRIBE {TableName}");

                var insertColumns = currentTableDetails
                    .Where(x => !x.Extra.Contains("auto_increment") && !new string[] { "create_date", "last_updated" }.Contains(x.Field));

                var updateColumns = insertColumns
                    .Where(x => !x.Key.Contains("PRI") && !x.Key.Contains("UNI"));

                if (string.IsNullOrWhiteSpace(InsertQuery))
                {
                    var newQuery = new StringBuilder();

                    newQuery.Append("INSERT INTO ");
                    newQuery.Append(TableName);
                    newQuery.Append(" (");
                    newQuery.Append(string.Join(",", insertColumns.Select(x => x.Field)));
                    newQuery.Append(") VALUES (");
                    newQuery.Append(string.Join(",", insertColumns.Select(x => $"@{x.Field}")));
                    newQuery.Append(") ");
                    newQuery.Append("ON DUPLICATE KEY UPDATE ");
                    newQuery.Append(string.Join(",", updateColumns.Select(x => $"{x.Field} = VALUES({x.Field})")));
                    newQuery.Append(";");

                    SetInsertQuery(newQuery.ToString());
                }

                if ((TableColumns?.Count ?? 0) <= 0)
                {
                    var columnDefinitions = insertColumns
                        .Select((x) =>
                        {
                            var fieldType = x.Type;
                            var fieldSize = -1;

                            if (fieldType.Contains("("))
                            {
                                var typeParts = fieldType.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

                                fieldType = typeParts[0];

                                if (!int.TryParse(typeParts[1], out fieldSize))
                                    fieldSize = -1;
                            }

                            var typeConverter = new Dictionary<string, MySqlDbType>
                            {
                                { "bigint", MySqlDbType.Int64 },
                                { "char", MySqlDbType.VarChar },
                                { "varchar", MySqlDbType.VarChar },
                                { "smallint", MySqlDbType.Int16 },
                                { "mediumint", MySqlDbType.Int24 },
                                { "int", MySqlDbType.Int32 },
                                { "tinyint", MySqlDbType.Int16 },
                                { "bit", MySqlDbType.Bit },
                                { "timestamp", MySqlDbType.Timestamp },
                                { "datetime", MySqlDbType.DateTime },
                                { "blob", MySqlDbType.Blob },
                                { "decimal", MySqlDbType.Decimal },
                                { "double", MySqlDbType.Double },
                                { "float", MySqlDbType.Float },
                                { "guid", MySqlDbType.Guid },
                                { "text", MySqlDbType.Text },
                                { "time", MySqlDbType.Time },
                                { "date", MySqlDbType.Date },
                                { "json", MySqlDbType.JSON }
                            };

                            if (!typeConverter.ContainsKey(fieldType))
                                throw new KeyNotFoundException($"Error auto-populating columns for [{TableName}]: Invalid field type [{fieldType}]");

                            return new Tuple<string, MySqlDbType, int, string>(x.Field, typeConverter[fieldType], fieldSize, null);
                        });

                    AddColumns(columnDefinitions);
                }
            }
        }

        private DataTable CreateOutputDataTable(Func<string, T, object> DataTableFunction)
        {
            OutputTable = new DataTable();

            OutputTable.Clear();

            foreach (var column in TableColumns)
            {
                OutputTable.Columns.Add(column.Key);
            }

            foreach (var data in SourceData)
            {
                OutputTable.Rows.Add(CreateOutputDataRow(OutputTable, data, DataTableFunction));
            }

            return OutputTable;
        }

        private DataRow CreateOutputDataRow(DataTable formData, T RowData, Func<string, T, object> DataTableFunction)
        {
            var newRow = formData.NewRow();

            if (DataTableFunction == null)
            {
                var convertableProperties = RowData
                    .GetType()
                    .GetProperties()
                    .ToList();

                var uppercaseSearchPattern = @"(?<!_|^|Internal)([A-Z])";
                var replacePattern = @"_$1";

                var underscoreProperties = convertableProperties
                    .ToDictionary(x => x.Name.StartsWith("InternalId") ? x.Name : Regex.Replace(x.Name, uppercaseSearchPattern, replacePattern), x => new Tuple<string, PropertyInfo>(x.Name, x))
                    .ToList();

                // autoresolve object properties here

                // run through each column of table
                foreach (var tableColumn in TableColumns)
                {
                    // check if AlternatePropertyName is a property on this object
                    var alternateUnderscoreName = tableColumn.Value.Item3 == null ? null : Regex.Replace(tableColumn.Value.Item3, uppercaseSearchPattern, replacePattern);

                    var underscoreProperty = (KeyValuePair<string, Tuple<string, PropertyInfo>>?)null;

                    // if AlternetPropertyName is null or not on the object, convert column name to property name
                    // check to see if there is an alternate name converted to an underscore name with that key, if there isn't then check the underscoreName 
                    if (underscoreProperties.Any(x => x.Key.ToLower() == tableColumn.Key.ToLower() || x.Key.ToLower() == alternateUnderscoreName?.ToLower()))
                    {
                        underscoreProperty = underscoreProperties
                            .Where(x => x.Key.ToLower() == alternateUnderscoreName?.ToLower())
                            .FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(underscoreProperty.Value.Key))
                        {
                            underscoreProperty = underscoreProperties
                                .Where(x => x.Key.ToLower() == tableColumn.Key.ToLower())
                                .FirstOrDefault();
                        }
                    }

                    // if there is grab the value
                    if (underscoreProperty.HasValue)
                    {
                        var resolvedObject = underscoreProperty.Value.Value.Item2.GetValue(RowData, null);

                        // get value from property name, perform any type conversions as necessary
                        switch (tableColumn.Value.Item1)
                        {
                            case MySqlDbType.Bit:
                            case MySqlDbType.Int16:
                            case MySqlDbType.Int24:
                            case MySqlDbType.Int32:
                            case MySqlDbType.Int64:
                                if (underscoreProperty.Value.Value.Item2.PropertyType == typeof(bool) || underscoreProperty.Value.Value.Item2.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(bool))
                                    resolvedObject = (bool)underscoreProperty.Value.Value.Item2.GetValue(RowData, null) ? 1 : 0;
                                break;
                            case MySqlDbType.Timestamp:
                            case MySqlDbType.DateTime:
                                if (underscoreProperty.Value.Value.Item2.PropertyType == typeof(DateTime) || underscoreProperty.Value.Value.Item2.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(DateTime))
                                    resolvedObject = ((DateTime)underscoreProperty.Value.Value.Item2.GetValue(RowData, null)).ToString("yyyy-MM-dd HH:mm:ss");
                                break;
                            case MySqlDbType.Date:
                                if (underscoreProperty.Value.Value.Item2.PropertyType == typeof(DateTime) || underscoreProperty.Value.Value.Item2.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(DateTime))
                                    resolvedObject = ((DateTime)underscoreProperty.Value.Value.Item2.GetValue(RowData, null)).ToString("yyyy-MM-dd");
                                break;
                            case MySqlDbType.Time:
                                if (underscoreProperty.Value.Value.Item2.PropertyType == typeof(DateTime) || underscoreProperty.Value.Value.Item2.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(DateTime))
                                    resolvedObject = ((DateTime)underscoreProperty.Value.Value.Item2.GetValue(RowData, null)).ToString("HH:mm:ss");
                                break;
                            case MySqlDbType.JSON:
                                resolvedObject = Newtonsoft.Json.JsonConvert.SerializeObject(underscoreProperty.Value.Value.Item2.GetValue(RowData, null));
                                break;
                        }

                        newRow[tableColumn.Key] = resolvedObject;
                        /*
                        newRow[tableColumn.Key] = (new MySqlDbType[] { MySqlDbType.Bit, MySqlDbType.Int16, MySqlDbType.Int24, MySqlDbType.Int32, MySqlDbType.Int64 }.Contains(tableColumn.Value.Item1) && underscoreProperty.Value.Value.Item2.PropertyType == typeof(bool))
                            ? (bool)underscoreProperty.Value.Value.Item2.GetValue(RowData, null) ? 1 : 0
                            : (new MySqlDbType[] { MySqlDbType.Timestamp, MySqlDbType.DateTime }.Contains(tableColumn.Value.Item1) && underscoreProperty.Value.Value.Item2.PropertyType == typeof(DateTime))
                                ? ((DateTime)underscoreProperty.Value.Value.Item2.GetValue(RowData, null)).ToString("yyyy-MM-dd HH:mm:ss")
                                : tableColumn.Value.Item1 == MySqlDbType.Date
                                    ? ((DateTime)underscoreProperty.Value.Value.Item2.GetValue(RowData, null)).ToString("yyyy-MM-dd")
                                    : tableColumn.Value.Item1 == MySqlDbType.Time
                                        ? ((DateTime)underscoreProperty.Value.Value.Item2.GetValue(RowData, null)).ToString("HH:mm:ss")
                                        : underscoreProperty.Value.Value.Item2.GetValue(RowData, null);
                        */
                    }
                    else
                    {
                        // if can't find property, just return null
                        newRow[tableColumn.Key] = null;
                    }
                }
            }
            else
            {
                foreach (var column in TableColumns)
                {
                    newRow[column.Key] = DataTableFunction(column.Key, RowData);
                }
            }

            return newRow;
        }

        public BulkTableWriter<T> UseTransaction(bool WriteWithTransaction)
        {
            this.WriteWithTransaction = WriteWithTransaction;

            return this;
        }

        public BulkTableWriter<T> SetTransaction(MySqlTransaction SqlTransaction)
        {
            this.SqlTransaction = SqlTransaction;

            return this;
        }

        public BulkTableWriter<T> ThrowException(bool ShouldThrowException)
        {
            this.ShouldThrowException = ShouldThrowException;

            return this;
        }

        public BulkTableWriter<T> SetInsertQuery(string InsertQuery)
        {
            this.InsertQuery = InsertQuery;

            return this;
        }

        public BulkTableWriter<T> SetSourceData(IEnumerable<T> SourceData)
        {
            this.SourceData = SourceData;

            return this;
        }

        public BulkTableWriter<T> SetSourceData(T SourceData)
        {
            this.SourceData = new List<T> { SourceData };

            return this;
        }

        public BulkTableWriter<T> AddColumn(string ColumnName, MySqlDbType DbType, int Size, string AlternatePropertyName = null)
        {
            if (TableColumns == null)
                TableColumns = new Dictionary<string, Tuple<MySqlDbType, int, string>>();

            TableColumns.Add(ColumnName, new Tuple<MySqlDbType, int, string>(DbType, Size, AlternatePropertyName));

            return this;
        }

        public BulkTableWriter<T> AddColumns(IEnumerable<Tuple<string, MySqlDbType, int, string>> Columns)
        {
            if (TableColumns == null)
                TableColumns = new Dictionary<string, Tuple<MySqlDbType, int, string>>();

            this.ColumnNames = Columns?.Select(x => x.Item1);

            if (Columns?.FirstOrDefault()?.Item2 == null)
                return this;

            foreach (var column in Columns)
            {
                TableColumns.Add(column.Item1, new Tuple<MySqlDbType, int, string>(column.Item2, column.Item3, column.Item4));
            }

            return this;
        }

        public BulkTableWriter<T> SetTableName(string TableName)
        {
            this.TableName = TableName;

            return this;
        }

        public BulkTableWriter<T> RemoveColumn(string ColumnName)
        {
            if (TableColumns.ContainsKey(ColumnName))
                TableColumns.Remove(ColumnName);

            return this;
        }

        public BulkTableWriter<T> SetBatchSize(int BatchSize)
        {
            this.BatchSize = BatchSize;

            return this;
        }

        public BulkTableWriter<T> ResetBatchSize()
        {
            this.BatchSize = DEFAULT_BATCH_SIZE;

            return this;
        }
    }
}
