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
        public ExampleObjectChild() : base() { }
        public ExampleObjectChild(DataRow TrackerRow, string AlternateTableName = null) : base(TrackerRow, AlternateTableName) { }

        [DALResolvable]
        [DALTransferProperty]
        public string ChildProperty1 { get; set; }

        /// <summary>
        /// This property can be read/written to the database, but it will not be included in the DTO unless explicitly included
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

        [DALResolvable]
        [DALTransferProperty]
        public string ExampleParentInternalId { get; set; }

        private ExampleObject _exampleParent;
        [DALTransferProperty]
        public ExampleObject ExampleParent
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExampleParentInternalId)) return null;

                _exampleParent = _exampleParent ?? ExampleObjectHelper.GetExampleObject(ExampleParentInternalId);

                return _exampleParent;
            }
            set
            {
                _exampleParent = value;
            }
        }
    }
}
