using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// This is an example DAL Resolver class. This class would be customized and placed anywhere in the target application as long as it is publically accessible, because this
/// class is identified for usage by DALHelper using Reflection to look for the Interface "IDALResolver". It's only purpose is to convert the convenience enum ConnectionStringTypes 
/// to a MySqlConnectionStringBuilder, which DALHelper uses internally.
/// </summary>

namespace DALHelperNet.Interfaces.Examples
{
	/// <summary>
	/// An enum with connection types defined at the namespace level so that it can be referenced without the need for a class name.
	/// </summary>
	public enum ExampleConnectionStringTypes
	{
		FirstApplicationDatabase,
		SecondApplicationDatabase
	}

	/// <summary>
	/// This class resolves enumerated values defined in the common class library into application specific connection string builders. The resolution
	/// details can be fully custom, as long as it takes in an enumeration value and returns a database connection string builder.
	/// 
	/// Place this file into your App_Code folder for ASP.NET projects, and anywhere in your application assembly for all other projects.
	/// </summary>
	public class ExampleDALResolver : IDALResolver
	{
		/// <summary>
		/// Called by DALHelper to resolve enum values into web.config connection strings
		/// </summary>
		/// <param name="ConfigConnectionString">Enum value of the connection string</param>
		/// <returns>Return MySqlConnectionStringBuilder for use in various follow-on functionality (example: to pull just the database name for use in multi-database queries)</returns>
		public MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum ConfigConnectionString)
		{
			var connectionString = string.Empty;
			switch (ConfigConnectionString)
			{
				case ExampleConnectionStringTypes.FirstApplicationDatabase:
					connectionString = "firstApplicationDatabaseConnectionString";
					break;
				case ExampleConnectionStringTypes.SecondApplicationDatabase:
					connectionString = "secondApplicationDatabaseConnectionString";
					break;
			}

			return new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[connectionString].ConnectionString);
		}
	}
}

