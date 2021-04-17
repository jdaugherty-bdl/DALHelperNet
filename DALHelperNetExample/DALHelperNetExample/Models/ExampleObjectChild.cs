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
    [DALTable("example_object_children")]
    public class ExampleObjectChild : DALBaseModel
    {
        [DALResolvable]
        [DALTransferProperty]
        public string ChildProperty1 { get; set; }

        /// <summary>
        /// This property can be read/written to the database, but it will not be included in the DTO unless explicitly included
        /// </summary>
        [DALResolvable]
        public string NoDtoProperty { get; set; }

        [DALResolvable]
        [DALTransferProperty]
        public string ExampleBasicObjectInternalId { get; set; }

        private ExampleObject _exampleParent;
        [DALTransferProperty]
        public ExampleObject ExampleParent
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExampleBasicObjectInternalId)) return null;

                _exampleParent = _exampleParent ?? ExampleObjectHelper.GetExampleObject(ExampleBasicObjectInternalId);

                return _exampleParent;
            }
            set
            {
                _exampleParent = value;
            }
        }

        public ExampleObjectChild() : base() { }
        private static string ThisTypeName => typeof(ExampleObjectChild).Name;
        public ExampleObjectChild(DataRow TrackerRow, string AlternateTableName = null) : base(TrackerRow, AlternateTableName ?? ThisTypeName) { }
    }
}
