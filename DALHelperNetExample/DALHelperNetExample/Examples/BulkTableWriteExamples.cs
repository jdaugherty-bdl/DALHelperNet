using DALHelperNet;
using DALHelperNet.Extensions;
using DALHelperNetExample.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample.Examples
{
    public class BulkTableWriteExamples
    {
        public static bool RunAllBulkTalbeWriteExamples()
        {
            var rowsUpdated = WriteToDatabaseFullAutomation();

            rowsUpdated = BulkTableWritePartialAutomation();
            
            rowsUpdated = BulkTableWriterFullExplicit();

            return rowsUpdated > 0;
        }

        #region Mockup code
        private static List<ExampleObject> MockupObjects()
        {
            var exampleObjects = new List<ExampleObject>
            {
                new ExampleObject
                {
                    ExampleProperty1 = "1 | example property 1",
                    ExampleProperty2 = 1,
                    ExampleProperty3 = 1L,
                    ExampleProperty4 = 1m,
                    ExampleProperty5 = DateTime.Now.AddSeconds(1),
                    ExampleProperty6 = ExampleObject.ExampleObjectEnumItems.EnumItem1,
                    LocalPropertyOnly = "1 | local property only",  // this property won't be written to the database
                    NoDalProperty = "1 | no DAL property",          // this property won't be written to the database
                    NoDtoProperty = "1 | no DTO property"
                },
                new ExampleObject
                {
                    ExampleProperty1 = "2 | example property 1",
                    ExampleProperty2 = 2,
                    ExampleProperty3 = 2L,
                    ExampleProperty4 = 2m,
                    ExampleProperty5 = DateTime.Now.AddSeconds(2),
                    ExampleProperty6 = ExampleObject.ExampleObjectEnumItems.EnumItem2,
                    LocalPropertyOnly = "2 | local property only",  // this property won't be written to the database
                    NoDalProperty = "2 | no DAL property",          // this property won't be written to the database
                    NoDtoProperty = "2 | no DTO property"
                },
                new ExampleObject
                {
                    ExampleProperty1 = "3 | example property 1",
                    ExampleProperty2 = 3,
                    ExampleProperty3 = 3L,
                    ExampleProperty4 = 3m,
                    ExampleProperty5 = DateTime.Now.AddSeconds(3),
                    ExampleProperty6 = ExampleObject.ExampleObjectEnumItems.EnumItem3,
                    LocalPropertyOnly = "3 | local property only",  // this property won't be written to the database
                    NoDalProperty = "3 | no DAL property",          // this property won't be written to the database
                    NoDtoProperty = "3 | no DTO property"
                },
            };

            exampleObjects[1].ExampleChildren = new List<ExampleObjectChild>
            {
                new ExampleObjectChild
                {
                    ExampleParentInternalId = exampleObjects[1].InternalId,
                    ChildProperty1 = "1 | child property 1",
                    LocalPropertyOnly = "1 | local property only",  // this property won't be written to the database
                    NoDalProperty = "1 | no DAL property",          // this property won't be written to the database
                    NoDtoProperty = "1 | no DTO property"
                },
                new ExampleObjectChild
                {
                    ExampleParentInternalId = exampleObjects[1].InternalId,
                    ChildProperty1 = "2 | child property 1",
                    LocalPropertyOnly = "2 | local property only",  // this property won't be written to the database
                    NoDalProperty = "2 | no DAL property",          // this property won't be written to the database
                    NoDtoProperty = "2 | no DTO property"
                }
            };

            exampleObjects[2].ExampleChildren = new List<ExampleObjectChild>
            {
                new ExampleObjectChild
                {
                    ExampleParentInternalId = exampleObjects[2].InternalId,
                    ChildProperty1 = "1 | child property 1",
                    LocalPropertyOnly = "1 | local property only",
                    NoDalProperty = "1 | no DAL property",
                    NoDtoProperty = "1 | no DTO property"
                },
                new ExampleObjectChild
                {
                    ExampleParentInternalId = exampleObjects[2].InternalId,
                    ChildProperty1 = "2 | child property 1",
                    LocalPropertyOnly = "2 | local property only",
                    NoDalProperty = "2 | no DAL property",
                    NoDtoProperty = "2 | no DTO property"
                },
                new ExampleObjectChild
                {
                    ExampleParentInternalId = exampleObjects[2].InternalId,
                    ChildProperty1 = "3 | child property 1",
                    LocalPropertyOnly = "3 | local property only",
                    NoDalProperty = "3 | no DAL property",
                    NoDtoProperty = "3 | no DTO property"
                }
            };

            return exampleObjects;
        }
        #endregion

        /// <summary>
        /// This function uses full DALHelper automation to write out objects to a database. Requires both DALTable and DALResolvable attributes
        /// to be defined on the class being written.
        /// </summary>
        /// <returns>Number of rows updated.</returns>
        public static int WriteToDatabaseFullAutomation()
        {
            var mockupObjects = MockupObjects();
            

            
            // first show how to write out a single object with multiple children
            var singleObjectWithChildren = mockupObjects[2];

            // this will write only the first level object
            var rowsUpdated = singleObjectWithChildren.WriteToDatabase(ExampleConnectionStringTypes.FirstApplicationDatabase);

            // this will bulk write all children objects
            rowsUpdated = singleObjectWithChildren.ExampleChildren.WriteToDatabase(ExampleConnectionStringTypes.FirstApplicationDatabase);



            // how to write a complex structure such as the full mockup object with multiple objects each having potentially multiple children?
            // with LINQ:

            // write out all top level mockup objects (i.e. no children)
            rowsUpdated = mockupObjects.WriteToDatabase(ExampleConnectionStringTypes.FirstApplicationDatabase);

            // to write out all children of all objects, gather all children into one list, then write out that list
            rowsUpdated = mockupObjects
                .SelectMany(mockupObject => mockupObject.ExampleChildren)
                .WriteToDatabase(ExampleConnectionStringTypes.FirstApplicationDatabase);



            // write data using an existing connection and supplied transaction
            using (var conn = DALHelper.GetConnectionFromString(ExampleConnectionStringTypes.FirstApplicationDatabase))
            {
                var exampleTransaction = conn.BeginTransaction();

                // this will write only the first level object
                rowsUpdated = singleObjectWithChildren.WriteToDatabase(conn, exampleTransaction);

                // this will bulk write all children objects
                rowsUpdated = singleObjectWithChildren.ExampleChildren.WriteToDatabase(conn, exampleTransaction);

                // write out all top level mockup objects (i.e. no children)
                rowsUpdated = mockupObjects.WriteToDatabase(conn, exampleTransaction);

                // to write out all children of all objects, gather all children into one list, then write out that list
                rowsUpdated = mockupObjects
                    .SelectMany(mockupObject => mockupObject.ExampleChildren)
                    .WriteToDatabase(conn, exampleTransaction);

                exampleTransaction.Commit();
            }

            return rowsUpdated;
        }

        public static int BulkTableWritePartialAutomation()
        {
            var mockupObjects = MockupObjects();



            // first show how to write out a single object with multiple children
            var singleObjectWithChildren = mockupObjects[2];

            // this will write only the first level object
            var rowsUpdated = DALHelper.BulkTableWrite(ExampleConnectionStringTypes.FirstApplicationDatabase, singleObjectWithChildren);
            // optional parameter to force the table name
            rowsUpdated = DALHelper.BulkTableWrite(ExampleConnectionStringTypes.FirstApplicationDatabase, singleObjectWithChildren, TableName: "example_objects");
            // optional parameter to force the table name using the DALTable attribute of a class
            rowsUpdated = DALHelper.BulkTableWrite(ExampleConnectionStringTypes.FirstApplicationDatabase, singleObjectWithChildren, ForceType: typeof(ExampleObject)); 

            // this will bulk write all children objects
            rowsUpdated = DALHelper.BulkTableWrite(ExampleConnectionStringTypes.FirstApplicationDatabase, singleObjectWithChildren.ExampleChildren);



            // how to write a complex structure such as the full mockup object with multiple objects each having potentially multiple children?
            // with LINQ:

            // write out all top level mockup objects (i.e. no children)
            rowsUpdated = DALHelper.BulkTableWrite(ExampleConnectionStringTypes.FirstApplicationDatabase, mockupObjects);

            // to write out all children of all objects, gather all children into one list, then write out that list
            rowsUpdated = DALHelper.BulkTableWrite(ExampleConnectionStringTypes.FirstApplicationDatabase, mockupObjects.SelectMany(mockupObject => mockupObject.ExampleChildren));



            // write data using an existing connection and supplied transaction
            using (var conn = DALHelper.GetConnectionFromString(ExampleConnectionStringTypes.FirstApplicationDatabase))
            {
                var exampleTransaction = conn.BeginTransaction();

                // this will write only the first level object
                rowsUpdated = DALHelper.BulkTableWrite(conn, singleObjectWithChildren, SqlTransaction: exampleTransaction);
                // optional parameter to force the table name
                rowsUpdated = DALHelper.BulkTableWrite(conn, singleObjectWithChildren, SqlTransaction: exampleTransaction, TableName: "example_objects");
                // optional parameter to force the table name using the DALTable attribute of a class
                rowsUpdated = DALHelper.BulkTableWrite(conn, singleObjectWithChildren, SqlTransaction: exampleTransaction, ForceType: typeof(ExampleObject)); 

                // this will bulk write all children objects
                rowsUpdated = DALHelper.BulkTableWrite(conn, singleObjectWithChildren.ExampleChildren, SqlTransaction: exampleTransaction);

                // write out all top level mockup objects (i.e. no children)
                rowsUpdated = DALHelper.BulkTableWrite(conn, mockupObjects, SqlTransaction: exampleTransaction);

                // to write out all children of all objects, gather all children into one list, then write out that list
                rowsUpdated = DALHelper.BulkTableWrite(conn, mockupObjects.SelectMany(mockupObject => mockupObject.ExampleChildren), SqlTransaction: exampleTransaction);

                exampleTransaction.Commit();
            }

            return rowsUpdated;
        }

        /// <summary>
        /// Shows how to bulk write DALHelper object data to the database using the most manual way possible short of making direct DALHelper.DoDatabaseWork() calls on each individual row.
        /// </summary>
        /// <returns>Number of rows written.</returns>
        public static int BulkTableWriterFullExplicit()
        {
            var mockupObjects = MockupObjects();


            // NOTE: none of the following will write out the example object children - another set of code like this would be needed to do that

            // ExampleObject insert query
            var exampleInsertQuery = @"INSERT INTO example_objects
                    (example_property1, example_read_only_property1, example_property2, example_property3, example_property4, example_property5, example_property6, no_dto_property, active, InternalId)
                VALUES
                    (@example_property1, @example_read_only_property1, @example_property2, @example_property3, @example_property4, @example_property5, @example_property6, @no_dto_property, @active, @InternalId)
                ON DUPLICATE KEY UPDATE
                    example_property1 = VALUES(example_property1), example_read_only_property1 = VALUES(example_read_only_property1), example_property2 = VALUES(example_property2), 
                    example_property3 = VALUES(example_property3), example_property4 = VALUES(example_property4), example_property5 = VALUES(example_property5), example_property6 = VALUES(example_property6), 
                    no_dto_property = VALUES(no_dto_property), active = VALUES(active);";

            // write data using connection string enum
            var rowsUpdated = DALHelper.GetBulkTableWriter<ExampleObject>(ExampleConnectionStringTypes.FirstApplicationDatabase)
                // .AddColumn() and .AddColumns() both populate table column definitions the same way behind the scenes, both ways are shown here for illustration purposes
                .AddColumn("example_property1", MySqlDbType.VarChar, 512)
                .AddColumn("example_read_only_property1", MySqlDbType.VarChar, 256)
                .AddColumn("example_property2", MySqlDbType.Int32, 4)
                .AddColumn("example_property3", MySqlDbType.Int64, 8)
                .AddColumn("example_property4", MySqlDbType.Decimal, -1)
                .AddColumn("example_property5", MySqlDbType.DateTime, -1)
                .AddColumn("example_property6", MySqlDbType.VarChar, 64)
                .AddColumn("no_dto_property", MySqlDbType.VarChar, 512)
                .AddColumn("active", MySqlDbType.Int16, 2)
                .AddColumn("InternalId", MySqlDbType.VarChar, 45)
                .SetBatchSize(10)
                .SetInsertQuery(exampleInsertQuery)
                .SetSourceData(mockupObjects)
                .ThrowException(true)
                .UseTransaction(true)
                .Write((columnName, objectItem) =>
                {
                    switch (columnName)
                    {
                        case "example_property1": return objectItem.ExampleProperty1;
                        case "example_read_only_property1": return objectItem.ExampleReadOnlyProperty1;
                        case "example_property2": return objectItem.ExampleProperty2;
                        case "example_property3": return objectItem.ExampleProperty3;
                        case "example_property4": return objectItem.ExampleProperty4;
                        case "example_property5": return objectItem.ExampleProperty5;
                        case "example_property6": return objectItem.ExampleProperty6;
                        case "no_dto_property": return objectItem.NoDtoProperty;
                        case "active": return objectItem.Active ? 1 : 0;
                        case "InternalId": return objectItem.InternalId;
                        default: return null;
                    }
                });

            // write data using an existing connection and supplied transaction
            using (var conn = DALHelper.GetConnectionFromString(ExampleConnectionStringTypes.FirstApplicationDatabase))
            {
                var exampleTransaction = conn.BeginTransaction();

                rowsUpdated = DALHelper.GetBulkTableWriter<ExampleObject>(conn)
                    // .AddColumn() and .AddColumns() both populate table column definitions the same way behind the scenes, both ways are shown here for illustration purposes
                    .AddColumns(new List<Tuple<string, MySqlDbType, int>>
                    {
                        new Tuple<string, MySqlDbType, int>("example_property1", MySqlDbType.VarChar, 512),
                        new Tuple<string, MySqlDbType, int>("example_read_only_property1", MySqlDbType.VarChar, 256),
                        new Tuple<string, MySqlDbType, int>("example_property2", MySqlDbType.Int32, 4),
                        new Tuple<string, MySqlDbType, int>("example_property3", MySqlDbType.Int64, 8),
                        new Tuple<string, MySqlDbType, int>("example_property4", MySqlDbType.Decimal, -1),
                        new Tuple<string, MySqlDbType, int>("example_property5", MySqlDbType.DateTime, -1),
                        new Tuple<string, MySqlDbType, int>("example_property6", MySqlDbType.VarChar, 64),
                        new Tuple<string, MySqlDbType, int>("no_dto_property", MySqlDbType.VarChar, 512),
                        new Tuple<string, MySqlDbType, int>("active", MySqlDbType.Int16, 2),
                        new Tuple<string, MySqlDbType, int>("InternalId", MySqlDbType.VarChar, 45),
                    })
                    .SetBatchSize(10)
                    .SetInsertQuery(exampleInsertQuery)
                    .SetSourceData(mockupObjects)
                    .ThrowException(true)
                    .SetTransaction(exampleTransaction)
                    .Write((columnName, objectItem) =>
                    {
                        switch (columnName)
                        {
                            case "example_property1": return objectItem.ExampleProperty1;
                            case "example_read_only_property1": return objectItem.ExampleReadOnlyProperty1;
                            case "example_property2": return objectItem.ExampleProperty2;
                            case "example_property3": return objectItem.ExampleProperty3;
                            case "example_property4": return objectItem.ExampleProperty4;
                            case "example_property5": return objectItem.ExampleProperty5;
                            case "example_property6": return objectItem.ExampleProperty6;
                            case "no_dto_property": return objectItem.NoDtoProperty;
                            case "active": return objectItem.Active ? 1 : 0;
                            case "InternalId": return objectItem.InternalId;
                            default: return null;
                        }
                    });

                exampleTransaction.Commit();
            }

            return rowsUpdated;
        }
    }
}
