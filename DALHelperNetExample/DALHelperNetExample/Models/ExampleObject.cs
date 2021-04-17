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
        public ExampleObject() : base() { }
        public ExampleObject(DataRow TrackerRow, string AlternateTableName = null) : base(TrackerRow, AlternateTableName) { }

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

        /// <summary>
        /// This property can be read/written to the database, but it will not be included in the DTO unless explicitly included.
        /// </summary>
        [DALResolvable]
        public string NoDtoProperty { get; set; }

        /// <summary>
        /// This property can be used within your program and will be included in the DTO, but will not be read/written to the database.
        /// </summary>
        [DALTransferProperty]
        public string NoDalProperty { get; set; }

        /// <summary>
        /// This property can be used within your program, and will not be included in the DTO, nor will it be read/written to the database
        /// </summary>
        public string LocalPropertyOnly { get; set; }

        private IEnumerable<ExampleObjectChild> _exampleChildren;
        [DALTransferProperty]
        public IEnumerable<ExampleObjectChild> ExampleChildren
        {
            get
            {
                _exampleChildren = _exampleChildren ?? ExampleObjectChildHelper.GetExampleObjectChildren(InternalId);

                return _exampleChildren;
            }
            set
            {
                _exampleChildren = value;
            }
        }
    }
}
