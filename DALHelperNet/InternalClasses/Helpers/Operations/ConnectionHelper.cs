using DALHelperNet.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers.Operations
{
    internal class ConnectionHelper
    {
		// a pointer to the application's resolver instance
		internal static IDALResolver DALResolver = GetResolverInstance();

		/// <summary>
		/// find an object inheriting from IDALResolver, but only look in the entry assembly (where all your custom code is)
		/// once it is found, then that object is loaded through Reflection to be used later on.
		/// </summary>
		/// <returns>The application's DALResolver instance.</returns>
		internal static IDALResolver GetResolverInstance()
		{
			// try to get the resolver the standard way
			var entryAssembly = AppDomain
				.CurrentDomain
				.GetAssemblies()
				.Where(x => !string.IsNullOrWhiteSpace(x.EntryPoint?.Name))
				.SelectMany(x => x
					.GetModules()
					.SelectMany(y => y
						.GetTypes()
						.Where(z => z
							.GetInterfaces()
							.Any(a => a == typeof(IDALResolver)))))
				.FirstOrDefault();

			// if the standard way didn't work, do a little detective work (may not work 100% of the time)
			var clientDalResolverType =
				entryAssembly
				??
				AppDomain
				.CurrentDomain
				.GetAssemblies()
				.Where(x => x
					.GetCustomAttributes(true)
					.Any(y => y is AssemblyCompanyAttribute attribute && !attribute.Company.StartsWith("Microsoft", StringComparison.InvariantCultureIgnoreCase)))
				.SelectMany(x => x
					.GetModules()
					.SelectMany(y => y
						.GetTypes()
						.Where(z => z
							.GetInterfaces()
							.Any(a => a == typeof(IDALResolver)))))
				.FirstOrDefault();

			if (clientDalResolverType != null)
				return (IDALResolver)Activator.CreateInstance(clientDalResolverType);
			else
				throw new NullReferenceException("[GenericDALResolver]IDALResolver not found");
		}

		/// <summary>
		/// Gets a MySQL connection builder that is then used to establish a connection to the database
		/// </summary>
		/// <param name="ConfigConnectionString">A properly formatted database connection string</param>
		/// <returns>A connection string builder that can be used to establish connections</returns>
		internal static MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum ConfigConnectionString)
		{
			return DALResolver?.GetConnectionBuilderFromConnectionType(ConfigConnectionString);
		}

		internal static MySqlConnection GetConnectionFromString(Enum ConfigConnectionString, bool AllowUserVariables = false)
		{
			var connectionBuilder = GetConnectionBuilderFromConnectionType(ConfigConnectionString); // new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[connectionString].ConnectionString);
			connectionBuilder.ConvertZeroDateTime = true;

			if (AllowUserVariables)
				connectionBuilder.AllowUserVariables = true;

			return new MySqlConnection(connectionBuilder.ToString());
		}
	}
}
