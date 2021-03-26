using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.Interfaces
{
	public interface IDALResolver
	{
		MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum ConfigConnectionString);
	}
}
