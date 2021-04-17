using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample.Models
{
    [DALTable("example_basic_objects")]
    public class ExampleBasicObject : DALBaseModel
    {
        public ExampleBasicObject() : base() { }
        private static string ThisTypeName => typeof(ExampleBasicObject).Name;
        public ExampleBasicObject(DataRow TrackerRow, string AlternateTableName = null) : base(TrackerRow, AlternateTableName ?? ThisTypeName) { }
    }
}
