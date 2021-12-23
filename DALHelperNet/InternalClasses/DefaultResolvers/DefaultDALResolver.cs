using DALHelperNet.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.DefaultResolvers
{
    internal class DefaultDALResolver : IDALResolver
    {
        // if no other DAL Resolvers are specified in the client program, this one is used
        public MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum ConfigConnectionString)
        {
            // converts the enum name directly to string and then looks for that in the configuration file
            return new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[ConfigConnectionString.ToString()].ConnectionString);
        }
    }
}
