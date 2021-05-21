using DALHelperNet.Extensions;
using DALHelperNet.Interfaces.Attributes;
using DALHelperNet.InternalClasses.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Models
{
    public class DALTrigger<T>
    {
        public string TableName { get; private set; }
        public TriggerTypes TriggerType { get; private set; }

        public string TriggerBody { get; set; }
        public string DefinerUser { get; set; }
        public string StatementDelimiter { get; set; }

        private string _databaseName;
        public string DatabaseName 
        { 
            get { return _databaseName; }
            set { _databaseName = value.MySqlObjectQuote(); }
        }


        internal DALTrigger(TriggerTypes TriggerType, string TriggerBody = null)
        {
            TableName = typeof(T)
                .GetCustomAttribute<DALTable>()?.TableName?.MySqlObjectQuote()
                ??
                throw new CustomAttributeFormatException(DatabaseCoreUtilities.NoDalTableAttributeError);

            this.TriggerType = TriggerType;
            this.TriggerBody = TriggerBody;
            this.DefinerUser = "CURRENT_USER";
            this.StatementDelimiter = "$$";
        }

        private string TriggerTypeToIdentifier(bool IsCommand)
        {
            switch (TriggerType)
            {
                case TriggerTypes.BeforeInsert: return "BEFORE_INSERT".Replace("_", IsCommand ? " " : "_");
                case TriggerTypes.AfterInsert: return "AFTER_INSERT".Replace("_", IsCommand ? " " : "_");
                case TriggerTypes.BeforeUpdate: return "BEFORE_UPDATE".Replace("_", IsCommand ? " " : "_");
                case TriggerTypes.AfterUpdate: return "AFTER_UPDATE".Replace("_", IsCommand ? " " : "_");
                case TriggerTypes.BeforeDelete: return "BEFORE_DELETE".Replace("_", IsCommand ? " " : "_");
                case TriggerTypes.AfterDelete: return "AFTER_DELETE".Replace("_", IsCommand ? " " : "_");
                default: return null;
            }
        }

        public override string ToString()
        {
            var triggerName = $"{TableName.Substring(0, TableName.Length - 1)}_{TriggerTypeToIdentifier(false)}`";

            var createTriggerStatement = new StringBuilder();

            createTriggerStatement.Append("DROP TRIGGER IF EXISTS ");

            if (!string.IsNullOrWhiteSpace(DatabaseName))
                createTriggerStatement.Append($"{DatabaseName}.");

            createTriggerStatement.Append(triggerName);
            createTriggerStatement.AppendLine(";");

            createTriggerStatement.AppendLine();

            createTriggerStatement.Append("DELIMITER ");
            createTriggerStatement.AppendLine(StatementDelimiter);

            if (!string.IsNullOrWhiteSpace(DatabaseName))
            {
                createTriggerStatement.Append("USE ");
                createTriggerStatement.Append(DatabaseName);
                createTriggerStatement.AppendLine(StatementDelimiter);
            }

            createTriggerStatement.Append("CREATE DEFINER = ");
            createTriggerStatement.Append(DefinerUser);
            createTriggerStatement.Append(" TRIGGER ");

            if (!string.IsNullOrWhiteSpace(DatabaseName))
                createTriggerStatement.Append($"{DatabaseName}.");

            createTriggerStatement.Append(triggerName);
            createTriggerStatement.Append(" ");
            createTriggerStatement.Append(TriggerTypeToIdentifier(true));
            createTriggerStatement.Append(" ON ");
            createTriggerStatement.Append(TableName);
            createTriggerStatement.AppendLine(" FOR EACH ROW");

            createTriggerStatement.AppendLine("BEGIN");

            createTriggerStatement.AppendLine(TriggerBody);

            createTriggerStatement.Append("END");
            createTriggerStatement.AppendLine(StatementDelimiter);

            createTriggerStatement.AppendLine("DELIMITER ;");

            return createTriggerStatement.ToString();
        }
    }
}
