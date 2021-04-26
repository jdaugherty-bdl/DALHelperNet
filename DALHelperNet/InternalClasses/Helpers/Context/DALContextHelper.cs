using DALHelperNet.Interfaces;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.InternalClasses.Helpers.DataTransfer;
using DALHelperNet.InternalClasses.Helpers.Operations;
using DALHelperNet.InternalClasses.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers.Context
{
    internal class DALContextHelper : IDALContext
    {
        private Enum _currentConnectionStringType;
        private bool _currentAllowUserVariables;
        private MySqlConnection _currentExistingConnection;
        private MySqlTransaction _currentSqlTransaction;

        public DALContextHelper(Enum ConnectionStringType, bool AllowUserVariables = false, bool AutomaticallyOpenConnection = true)
        {
            _currentConnectionStringType = ConnectionStringType;
            _currentAllowUserVariables = AllowUserVariables;

            if (AutomaticallyOpenConnection)
                PrecheckCurrentConnection();
        }

        public DALContextHelper(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null)
        {
            _currentExistingConnection = ExistingConnection;
            _currentSqlTransaction = SqlTransaction;
        }

        private void PrecheckCurrentConnection()
        {
            _currentExistingConnection = _currentExistingConnection ?? ConnectionHelper.GetConnectionFromString(_currentConnectionStringType, AllowUserVariables: _currentAllowUserVariables);

            if (_currentExistingConnection.State != ConnectionState.Open)
                _currentExistingConnection.Open();

            _currentSqlTransaction = _currentSqlTransaction ?? _currentExistingConnection.BeginTransaction();
        }

        public MySqlTransaction StartTransaction()
        {
            PrecheckCurrentConnection();

            return _currentSqlTransaction;
        }

        public void EndTransaction()
        {
            _currentSqlTransaction?.Commit();

            _currentSqlTransaction.Dispose();
            _currentSqlTransaction = null;
        }

        public bool TableExists(string TableName)
        {
            PrecheckCurrentConnection();

            return TableOperationsHelper.TableExists(_currentExistingConnection, TableName, SqlTransaction: _currentSqlTransaction);
        }

        public bool TableExists(Type TableType = null)
        {
            var tableName = TableType.GetCustomAttribute<DALTable>()?.TableName ?? throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError);

            return TableExists(tableName);
        }

        public bool TableExists<T>()
        {
            return TableExists(typeof(T));
        }

        public bool CreateTable<T>(bool TruncateIfExists = false)
        {
            PrecheckCurrentConnection();

            return TableOperationsHelper.CreateTable<T>(_currentExistingConnection, SqlTransaction: _currentSqlTransaction, TruncateIfExists: TruncateIfExists);
        }

        public bool CreateTableIf<T>(Func<IDALContext, Type, bool> CheckPredicate, bool TruncateIfExists = false)
        {
            if (CheckPredicate(this, typeof(T)))
                return CreateTable<T>(TruncateIfExists: TruncateIfExists);

            return false;
        }

        public bool TruncateTable<T>(string TableName = null)
        {
            return TruncateTable(TableName, typeof(T));
        }

        public bool TruncateTable(string TableName = null, Type ForceType = null)
        {
            PrecheckCurrentConnection();

            return TableOperationsHelper.TruncateTable(_currentExistingConnection, TableName: TableName, ForceType: ForceType, SqlTransaction: _currentSqlTransaction);
        }

        public void Dispose()
        {
            if ((_currentExistingConnection?.State ?? ConnectionState.Broken) == ConnectionState.Open)
                _currentExistingConnection.Close();

            _currentExistingConnection?.Dispose();
            _currentExistingConnection = null;
        }
    }
}
