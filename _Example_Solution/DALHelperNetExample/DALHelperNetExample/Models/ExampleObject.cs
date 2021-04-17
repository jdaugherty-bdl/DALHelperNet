using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.Models;
using DALHelperNetExample.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNetExample.Models
{
    [DALTable("example_objects")]
    public class ExampleObject : DALBaseModel
    {
        [DALResolvable]
        [DALTransferProperty]
        public string ExampleProperty1 { get; set; }

        [DALResolvable]
        [DALTransferProperty]
        public string ExampleReadOnlyProperty1 => string.IsNullOrWhiteSpace(ExampleProperty1) || ExampleProperty1.Length == 0 ? null : ExampleProperty1.Substring(0, (int)Math.Ceiling(ExampleProperty1.Length / 2m)).Trim();

        [DALResolvable]
        [DALTransferProperty]
        public int ExampleProperty2 { get; set; }
        [DALResolvable]
        [DALTransferProperty]
        public long ExampleProperty3 { get; set; }
        [DALResolvable]
        [DALTransferProperty]
        public decimal ExampleProperty4 { get; set; }
        [DALResolvable]
        [DALTransferProperty]
        public DateTime ExampleProperty5 { get; set; }

        public enum ExampleObjectEnumItems
        {
            EnumItem1,
            EnumItem2,
            EnumItem3,
            EnumItem4
        }

        [DALResolvable]
        [DALTransferProperty]
        public ExampleObjectEnumItems ExampleProperty6 { get; set; }

        [DALResolvable]
        [DALTransferProperty]
        public string ExampleBasicObjectInternalId { get; set; }

        private ExampleBasicObject _basicObject;
        [DALTransferProperty]
        public ExampleBasicObject BasicObject
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExampleBasicObjectInternalId)) return null;

                _basicObject = _basicObject ?? ExampleBasicObjectHelper.GetExampleBasicObject(ExampleBasicObjectInternalId);

                return _basicObject;
            }
            set
            {
                _basicObject = value;
            }
        }

        public ExampleObject() : base() { }
        private static string ThisTypeName => typeof(ExampleObject).Name;
        public ExampleObject(DataRow TrackerRow, string AlternateTableName = null) : base(TrackerRow, AlternateTableName ?? ThisTypeName) { }
    }
}
