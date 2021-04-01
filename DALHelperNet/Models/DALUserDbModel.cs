using DALHelperNet.Interfaces.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Models
{
    public class DALUserDbModel : DALBaseModel
    {
        [DALResolvable]
        public int UserId { get; set; }
        [DALResolvable]
        public string UserEmail { get; set; }
        [DALResolvable]
        public string UserName { get; set; }

        public DALUserDbModel() : base() { }
        private static string ThisTypeName => typeof(DALUserDbModel).Name;
        public DALUserDbModel(DataRow TrackerRow, string AlternateTableName = null) : base(TrackerRow, AlternateTableName ?? ThisTypeName) { }
    }
}
