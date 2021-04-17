using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample.Models.Basic
{
    /// <summary>
    /// This is an example of the most basic DALHelper object. It represents a fully DALHelper-enabled object that only needs custom properties added.
    /// </summary>
    [DALTable("example_basic_objects")] // the table name to use when writing data
    public class ExampleBasicObject : DALBaseModel
    {
        // this is needed to allow empty constructors
        public ExampleBasicObject() : base() { }
        // this is needed to allow DALHelper to turn a TableRow into a single object
        public ExampleBasicObject(DataRow TrackerRow, string AlternateTableName = null) : base(TrackerRow, AlternateTableName) { }




        /* ... custom properties here ...
         * 
         * 
         * Example custom property:
         * 
         * [DALResolvable]         // marks this property as being enabled for read/write to the database. if this is a read-only property, data will be written to the database but not read from.
         * [DALTransferProperty]   // marks this property as being enabled for DTO generation
         * public string CustomProperty1 { get; set; }
        */




    }
}
