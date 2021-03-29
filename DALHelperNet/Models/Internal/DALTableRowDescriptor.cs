using DALHelperNet.Interfaces.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Models.Internal
{
    /// <summary>
    /// This class is used to gather information about database tables and their columns to enable automatic bulk table writes.
    /// </summary>
    internal class DALTableRowDescriptor : DALBaseModel
    {
        [DALResolvable]
        public string Field { get; set; }
        [DALResolvable]
        public string Type { get; set; }
        [DALResolvable]
        public string Null { get; set; }
        [DALResolvable]
        public string Key { get; set; }
        [DALResolvable]
        public string Default { get; set; }
        [DALResolvable]
        public string Extra { get; set; }

        public DALTableRowDescriptor() : base() { }
        private static string ThisTypeName => typeof(DALTableRowDescriptor).Name;
        public DALTableRowDescriptor(DataRow TrackerRow, string AlternateTableName = null) : base(TrackerRow, AlternateTableName ?? ThisTypeName) { }
    }
}
