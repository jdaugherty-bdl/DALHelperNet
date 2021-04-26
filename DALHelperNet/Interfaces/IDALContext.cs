using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Interfaces
{
    public interface IDALContext : IDisposable
    {
        bool TableExists(string TableName);
        bool TableExists(Type TableType);
        bool TableExists<T>();
        bool CreateTable<T>(bool TruncateIfExists = false);
        bool CreateTableIf<T>(Func<IDALContext, Type, bool> CheckPredicate, bool TruncateIfExists = false);
        bool TruncateTable<T>(string TableName = null);
        bool TruncateTable(string TableName = null, Type ForceType = null);
        MySqlTransaction StartTransaction();
        void EndTransaction();
    }
}
