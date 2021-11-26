using DALHelperNet.Extensions;
using DALHelperNet.InternalClasses.Helpers.DataTransfer;
using DALHelperNet.InternalClasses.Models;
using DALHelperNet.Models.Properties;
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
        private readonly int DEFAULT_BATCH_SIZE = 5; // default size of the write batches

        // exposed by functional methods
        private string InsertQuery;
        private string TableName;
        private bool WriteWithTransaction; // awkward name here so we can use the nice name for the set method below
        private bool ShouldThrowException; // awkward name here so we can use the nice name for the set method below
        private bool ShouldAllowUserVariables; // awkward name here so we can use the nice name for the set method below
        
        private Dictionary<string, Tuple<MySqlDbType, int, string, string>> _tableColumns;
        private Dictionary<string, Tuple<MySqlDbType, int, string, string>> TableColumns => _tableColumns = _tableColumns ?? new Dictionary<string, Tuple<MySqlDbType, int, string, string>>();
        
        private int BatchSize;
        private IEnumerable<T> SourceData;
        private MySqlTransaction SqlTransaction;

        // local objects
        private readonly MySqlConnection ExistingConnection;
        private readonly Enum ConfigConnectionString;
        private DataTable OutputTable;
        private readonly Dictionary<string, MySqlDbType> MySqlTypeConverter = new Dictionary<string, MySqlDbType>
        {
            { "bigint", MySqlDbType.Int64 },
            { "bigint unsigned", MySqlDbType.UInt64 },
            { "char", MySqlDbType.VarChar },
            { "varchar", MySqlDbType.VarChar },
            { "smallint", MySqlDbType.Int16 },
            { "smallint unsigned", MySqlDbType.UInt16 },
            { "mediumint", MySqlDbType.Int24 },
            { "mediumint unsigned", MySqlDbType.UInt24 },
            { "int", MySqlDbType.Int32 },
            { "int unsigned", MySqlDbType.UInt32 },
            { "tinyint", MySqlDbType.Int16 },
            { "tinyint unsigned", MySqlDbType.UInt16 },
            { "bit", MySqlDbType.Bit },
            { "timestamp", MySqlDbType.Timestamp },
            { "datetime", MySqlDbType.DateTime },
            { "blob", MySqlDbType.Blob },
            { "decimal", MySqlDbType.Decimal },
            { "double", MySqlDbType.Double },
            { "float", MySqlDbType.Float },
            { "guid", MySqlDbType.Guid },
            { "text", MySqlDbType.Text },
            { "longtext", MySqlDbType.LongText },
            { "time", MySqlDbType.Time },
            { "date", MySqlDbType.Date },
            { "json", MySqlDbType.JSON }
        };

        // hide the constructor so that users need to use the factory pattern through DALHelper
        internal BulkTableWriter() { }

        // start with a connection string enum
        internal BulkTableWriter(Enum ConfigConnectionString, string InsertQuery = null, bool ThrowException = true, bool UseTransaction = false, bool AllowUserVariables = false)
        {
            this.ConfigConnectionString = ConfigConnectionString;

            CommonSetup(InsertQuery, UseTransaction, ThrowException, null, AllowUserVariables);
        }

        // start wtih an already opened connection
        internal BulkTableWriter(MySqlConnection ExistingConnection, string InsertQuery = null, bool ThrowException = true, bool UseTransaction = false, MySqlTransaction SqlTransaction = null)
        {
            this.ExistingConnection = ExistingConnection;

            CommonSetup(InsertQuery, UseTransaction, ThrowException, SqlTransaction, false);
        }

        private void CommonSetup(string InsertQuery, bool UseTransaction, bool ThrowException, MySqlTransaction SqlTransaction, bool AllowUserVariables)
        {
            this.InsertQuery = InsertQuery;
            this.WriteWithTransaction = UseTransaction;
            this.ShouldThrowException = ThrowException;
            this.ShouldAllowUserVariables = AllowUserVariables;
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
            var sourceDataCount = SourceData?.Count() ?? 0;

            if (sourceDataCount == 0)
                return 0;

            PopulateColumnDetails();

            var outputIterations = sourceDataCount <= BatchSize
                ? 1
                : sourceDataCount / BatchSize;

            if (outputIterations * BatchSize < sourceDataCount)
                outputIterations++;

            var recordsInserted = 0;
            for (var i = 0; i < outputIterations; i++)
            {
                CreateOutputDataTable(DataTableFunction, i);

                if (ExistingConnection != null)
                    recordsInserted += DatabaseWorkHelper.DoDatabaseWork<int>(ExistingConnection, InsertQuery, CommonDatabaseWork, UseTransaction: WriteWithTransaction, ThrowException: ShouldThrowException, SqlTransaction: SqlTransaction);
                else
                    recordsInserted += DatabaseWorkHelper.DoDatabaseWork<int>(ConfigConnectionString, InsertQuery, CommonDatabaseWork, UseTransaction: WriteWithTransaction, ThrowException: ShouldThrowException, AllowUserVariables: ShouldAllowUserVariables);
            }

            return recordsInserted;
        }

        /// <summary>
        /// Standard database parameter setup and command execution function.
        /// </summary>
        /// <param name="CommandObject">The command to execute.</param>
        /// <returns>Number of rows affected by the command.</returns>
        private object CommonDatabaseWork(MySqlCommand CommandObject)
        {
            CommandObject.UpdatedRowSource = UpdateRowSource.None;

            // add all the parameters and point to the source table
            CommandObject
                .Parameters
                .AddRange(TableColumns
                    .Select(x => new MySqlParameter($"@{x.Key}", x.Value.Item1, x.Value.Item2, x.Key))
                    .ToArray());

            // Specify the number of records to be Inserted/Updated in one go. Default is 1.
            var adpt = new MySqlDataAdapter
            {
                InsertCommand = CommandObject,
                UpdateBatchSize = BatchSize
            };

            // output the data using batch output
            return adpt.Update(OutputTable);
        }

        /// <summary>
        /// Populates table columns and the insert query as needed.
        /// </summary>
        private void PopulateColumnDetails()
        {
            // if we have no table columns or insert query, build them automatically
            if ((TableColumns?.Count ?? 0) == 0 || string.IsNullOrWhiteSpace(InsertQuery))
            {
                // we need a table name at minimum to autobuild the rest
                if (string.IsNullOrWhiteSpace(TableName))
                    throw new ArgumentNullException("Error auto-populating Bulk Table Writer call: table name not defined");

                // pull the table details from the database
                var currentTableDetails = (ExistingConnection != null)
                    ? ObjectResultsHelper.GetDataObjects<DALTableRowDescriptor>(ExistingConnection, $"DESCRIBE {TableName}")
                    : ObjectResultsHelper.GetDataObjects<DALTableRowDescriptor>(ConfigConnectionString, $"DESCRIBE {TableName}");

                // use all column for insert EXCEPT autonumber fields and the boilerplate create_date and last_updated columns
                var insertColumns = currentTableDetails
                    .Where(x => !x.Extra.Contains("auto_increment") && !new string[] { "create_date", "last_updated" }.Contains(x.Field));

                // don't update primary key or unique columns on duplicate key as it's unnecessary
                var updateColumns = insertColumns
                    .Where(x => !x.Key.Contains("PRI") && !x.Key.Contains("UNI"));

                // if we don't have an insert query, make one
                if (string.IsNullOrWhiteSpace(InsertQuery))
                {
                    var newQuery = new StringBuilder();

                    newQuery.Append("INSERT INTO ");
                    newQuery.Append(TableName);
                    newQuery.Append(" (`");
                    newQuery.Append(string.Join("`,`", insertColumns.Select(x => x.Field)));
                    newQuery.Append("`) VALUES (");
                    newQuery.Append(string.Join(",", insertColumns.Select(x => $"@{x.Field}")));
                    newQuery.Append(") ");
                    newQuery.Append("ON DUPLICATE KEY UPDATE ");
                    newQuery.Append(string.Join(",", insertColumns.Where(x => !x.Key.Contains("PRI") && !x.Key.Contains("UNI")).Select(x => $"`{x.Field}` = VALUES(`{x.Field}`)"))); // don't update primary key or unique columns on duplicate key as it's unnecessary
                    newQuery.Append(";");

                    SetInsertQuery(newQuery.ToString());
                }

                // if we don't have a table columns list, make it
                if ((TableColumns?.Count ?? 0) <= 0)
                {
                    // use all of the insert columns because it's the larger set
                    var columnDefinitions = insertColumns
                        .Select((x) =>
                        {
                            var fieldType = x.Type;
                            var fieldSize = -1;

                            // try to pull the type and size from the column name
                            if (fieldType.Contains("("))
                            {
                                var typeParts = fieldType.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

                                fieldType = typeParts[0];

                                // if there's no size or the size is unparseable, just use -1 when inserting data
                                fieldSize = int.TryParse(typeParts[1], out int sizeField) ? sizeField : -1;
                            }

                            var convertedType = new DALPropertyType(fieldType);

                            // we don't already have this conversion defined, throw exception
                            //if (!MySqlTypeConverter.ContainsKey(fieldType))
                            if (convertedType.PropertyType == null)
                                throw new KeyNotFoundException($"Error auto-populating columns for [{TableName}]: Invalid field type [{fieldType}]");

                            //return new Tuple<string, MySqlDbType, int, string, string>(x.Field, MySqlTypeConverter[fieldType], fieldSize, null, x.Default);
                            return new Tuple<string, MySqlDbType, int, string, string>(x.Field, convertedType.PropertyMySqlDbType, fieldSize, null, x.Default);
                        });

                    // add all those columns to the output table
                    AddColumns(columnDefinitions);
                }
            }
        }

        /// <summary>
        /// Creates a populated output table using the provided column name => data object conversion function.
        /// </summary>
        /// <param name="DataTableFunction">The data conversion function to use, or null to use the automatic conversions.</param>
        /// <returns>A fully populated output data table.</returns>
        private DataTable CreateOutputDataTable(Func<string, T, object> DataTableFunction, int IterationCount)
        {
            // make a new table
            OutputTable = new DataTable();
            OutputTable.Clear();

            // add the columns
            OutputTable.Columns.AddRange(TableColumns.Select(x => new DataColumn(x.Key)).ToArray());

            // add the rows
            foreach (var data in SourceData.Skip(IterationCount * BatchSize).Take(BatchSize))
            {
                OutputTable.Rows.Add(CreateOutputDataRow(OutputTable, data, DataTableFunction));
            }

            return OutputTable;
        }

        /// <summary>
        /// Populates a single row of data in the output data table.
        /// </summary>
        /// <param name="formData">The output data table.</param>
        /// <param name="RowData">The object with the convertable proeprties to use as the data.</param>
        /// <param name="DataTableFunction">The data conversion function that takes a column name and returns an object of data.</param>
        /// <returns>One single row of populated data.</returns>
        private DataRow CreateOutputDataRow(DataTable formData, T RowData, Func<string, T, object> DataTableFunction)
        {
            var newRow = formData.NewRow();

            // if there is no data conversion function specified, auto generate
            if (DataTableFunction == null)
            {
                /*
                // get all potential properties that can be converted to data
                var convertableProperties = RowData
                    .GetType()
                    .GetProperties()
                    .ToList();

                var uppercaseSearchPattern = @"(?<!_|^|Internal)([A-Z])";
                var replacePattern = @"_$1";

                // get the underscore names and type info of the properties
                var underscoreProperties = convertableProperties
                    .ToDictionary(x => x.Name.StartsWith("InternalId") ? x.Name : Regex.Replace(x.Name, uppercaseSearchPattern, replacePattern), x => new Tuple<string, PropertyInfo>(x.Name, x))
                    .ToList();
                */
                var underscoreProperties = RowData.ConvertPropertiesToUnderscoreNames();

                // autoresolve object properties here

                // run through each column of table
                foreach (var tableColumn in TableColumns)
                {
                    // check if AlternatePropertyName is a property on this object
                    var alternateUnderscoreName = tableColumn.Value.Item3 == null ? null : Regex.Replace(tableColumn.Value.Item3, UnderscoreNamesHelper.UppercaseSearchPattern, UnderscoreNamesHelper.ReplacePattern);

                    var underscoreProperty = (KeyValuePair<string, Tuple<string, PropertyInfo>>?)null;

                    // if AlternatePropertyName is null or not on the object, convert column name to property name
                    // check to see if there is an alternate name converted to an underscore name with that key, if there isn't then check the underscoreName 
                    if (underscoreProperties.Any(x => x.Key.Equals(tableColumn.Key, StringComparison.InvariantCultureIgnoreCase) || x.Key.Equals(alternateUnderscoreName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        underscoreProperty = underscoreProperties
                            .Where(x => x.Key.Equals(alternateUnderscoreName, StringComparison.InvariantCultureIgnoreCase))
                            .FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(underscoreProperty.Value.Key))
                        {
                            underscoreProperty = underscoreProperties
                                .Where(x => x.Key.Equals(tableColumn.Key, StringComparison.InvariantCultureIgnoreCase))
                                .FirstOrDefault();
                        }
                    }

                    // if we found the underscore name then grab the value
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
                            case MySqlDbType.VarChar:
                                if (underscoreProperty.Value.Value.Item2.PropertyType == typeof(Enum) || underscoreProperty.Value.Value.Item2.PropertyType.GenericTypeArguments?.FirstOrDefault() == typeof(Enum))
                                    resolvedObject = ((Enum)underscoreProperty.Value.Value.Item2.GetValue(RowData, null)).ToString();
                                break;
                        }

                        // assign the data to the row
                        newRow[tableColumn.Key] = resolvedObject;
                    }
                    else
                    {
                        // if can't find property, just return null
                        newRow[tableColumn.Key] = tableColumn.Value.Item4;
                    }
                }
            }
            else // data conversion function is provided
            {
                // go through each column and use the data conversion function to populate the row
                foreach (var column in TableColumns)
                {
                    newRow[column.Key] = DataTableFunction(column.Key, RowData);
                }
            }

            return newRow;
        }

        /// <summary>
        /// Forces use of a transaction or not. BulkTableWriter uses transactions by default if not otherwise specified.
        /// </summary>
        /// <param name="UseTransaction">Force or prevent useage of a transaction.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> UseTransaction(bool UseTransaction)
        {
            this.WriteWithTransaction = UseTransaction;

            return this;
        }

        /// <summary>
        /// Provide a previously began transaction to use when writing data.
        /// </summary>
        /// <param name="SqlTransaction">Transaction object to use when writing data.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> SetTransaction(MySqlTransaction SqlTransaction)
        {
            this.SqlTransaction = SqlTransaction;

            return this;
        }

        /// <summary>
        /// Indicate whether BulkTableWriter should throw an exception on error, or just set DALHelper.HasError
        /// </summary>
        /// <param name="ThrowException">Whether to throw an exception or not.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> ThrowException(bool ThrowException)
        {
            this.ShouldThrowException = ThrowException;

            return this;
        }

        public BulkTableWriter<T> AllowUserVariables(bool AllowUserVariables)
        {
            this.ShouldAllowUserVariables = AllowUserVariables;

            return this;
        }

        /// <summary>
        /// Provide a custom insert query.
        /// </summary>
        /// <param name="InsertQuery">The full SQL insert query to use when writing.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> SetInsertQuery(string InsertQuery)
        {
            this.InsertQuery = InsertQuery;

            return this;
        }

        /// <summary>
        /// Writes a list of items to the database.
        /// </summary>
        /// <param name="SourceData">The list of items to write to the database.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> SetSourceData(IEnumerable<T> SourceData)
        {
            this.SourceData = SourceData;

            return this;
        }

        /// <summary>
        /// Writes a single object to the database.
        /// </summary>
        /// <param name="SourceData">The single object to write to the database.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> SetSourceData(T SourceData)
        {
            this.SourceData = new List<T> { SourceData };

            return this;
        }

        /// <summary>
        /// Add a single column to the output table.
        /// </summary>
        /// <param name="ColumnName">The underscore name to use as the column name.</param>
        /// <param name="DbType">The MySql type of the column.</param>
        /// <param name="Size">Size in bytes of the column.</param>
        /// <param name="AlternatePropertyName">Optional alternate property to correlate with this column.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> AddColumn(string ColumnName, MySqlDbType DbType, int Size, string AlternatePropertyName = null, string ColumnDefault = null)
        {
            TableColumns.Add(ColumnName, new Tuple<MySqlDbType, int, string, string>(DbType, Size, AlternatePropertyName, ColumnDefault));

            return this;
        }

        /// <summary>
        /// Add a list of columns to the output table.
        /// </summary>
        /// <param name="Columns">The list of columns using the specified structure: Tuple(ColumnName, MySqlDbType, ColumnSize, AlternateColumnName).</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> AddColumns(IEnumerable<Tuple<string, MySqlDbType, int, string, string>> Columns)
        {
            _tableColumns = TableColumns
                .Concat(Columns?
                    .Where(x => x?.Item2 != null)
                    .ToDictionary(x => x.Item1, x => new Tuple<MySqlDbType, int, string, string>(x.Item2, x.Item3, x.Item4, x.Item5))
                    ??
                    new Dictionary<string, Tuple<MySqlDbType, int, string, string>>())
                .ToDictionary(x => x.Key, x => x.Value);

            return this;
        }

        /// <summary>
        /// Add a list of columns to the output table.
        /// </summary>
        /// <param name="Columns">The list of columns using the specified structure: Tuple(ColumnName, MySqlDbType, ColumnSize).</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> AddColumns(IEnumerable<Tuple<string, MySqlDbType, int>> Columns)
        {
            return AddColumns(Columns.Select(x => new Tuple<string, MySqlDbType, int, string, string>(x.Item1, x.Item2, x.Item3, null, null)));
        }

        /// <summary>
        /// Sets the table name to be written to.
        /// </summary>
        /// <param name="TableName">Name of the table to write to.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> SetTableName(string TableName)
        {
            this.TableName = TableName;

            return this;
        }

        /// <summary>
        /// Remove the specified column from the output data table.
        /// </summary>
        /// <param name="ColumnName">Name of the column to remove.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> RemoveColumn(string ColumnName)
        {
            if (TableColumns.ContainsKey(ColumnName))
                TableColumns.Remove(ColumnName);

            return this;
        }

        /// <summary>
        /// Sets the write batch size to the specified number.
        /// </summary>
        /// <param name="BatchSize">Size of batches to write.</param>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> SetBatchSize(int BatchSize)
        {
            this.BatchSize = BatchSize;

            return this;
        }

        /// <summary>
        /// Resets the write batch size back to default.
        /// </summary>
        /// <returns>Itself</returns>
        public BulkTableWriter<T> ResetBatchSize()
        {
            this.BatchSize = DEFAULT_BATCH_SIZE;

            return this;
        }
    }
}
