using DALHelperNet.Interfaces;
using DALHelperNet.InternalClasses.Helpers.Context;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet
{
    public class DALContext : IDALContext
    {
        private DALContextHelper _currentContext;

        internal DALContext(Enum ConnectionStringType, bool AllowUserVariables = false, bool AutomaticallyOpenConnection = true)
        {
            _currentContext = new DALContextHelper(ConnectionStringType, AllowUserVariables: AllowUserVariables, AutomaticallyOpenConnection: AutomaticallyOpenConnection);
        }

        internal DALContext(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null)
        {
            _currentContext = new DALContextHelper(ExistingConnection, SqlTransaction: SqlTransaction);
        }

        public bool TableExists(string TableName)
            => _currentContext.TableExists(TableName);

        public bool TableExists(Type TableType)
            => _currentContext.TableExists(TableType);

        public bool TableExists<T>()
            => _currentContext.TableExists<T>();

        public bool CreateTable<T>(bool TruncateIfExists = false)
            => _currentContext.CreateTable<T>(TruncateIfExists: TruncateIfExists);

        public bool CreateTableIf<T>(Func<IDALContext, Type, bool> CheckPredicate, bool TruncateIfExists = false)
            => _currentContext.CreateTableIf<T>(CheckPredicate, TruncateIfExists: TruncateIfExists);

        public bool TruncateTable<T>(string TableName = null)
            => _currentContext.TruncateTable<T>(TableName: TableName);

        public bool TruncateTable(string TableName = null, Type ForceType = null)
            => _currentContext.TruncateTable(TableName: TableName, ForceType: ForceType);

        public MySqlTransaction StartTransaction()
            => _currentContext.StartTransaction();

        public void EndTransaction()
            => _currentContext.EndTransaction();

        public void Dispose()
        {
            _currentContext.Dispose();
        }
    }
}
