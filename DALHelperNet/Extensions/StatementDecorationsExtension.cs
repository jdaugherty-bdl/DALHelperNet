using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Extensions
{
    public static class StatementDecorationsExtension
    {
        public static string MySqlObjectQuote(this string ObjectName)
        {
            var updatedObjectName = ObjectName;

            if (!(updatedObjectName?.StartsWith("`") ?? true))
                updatedObjectName = "`" + updatedObjectName;

            if (!(updatedObjectName?.EndsWith("`") ?? true))
                updatedObjectName += "`";

            return updatedObjectName;
        }
    }
}
