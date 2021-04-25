using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers
{
    internal class IdentityHelper
    {
		/// <summary>
		/// Use the MySql built in function to get the ID of the last row inserted.
		/// </summary>
		/// <param name="ConfigConnectionString">The connection type to use when getting the last ID.</param>
		/// <returns>A string representation of the ID.</returns>
		internal static string GetLastInsertId(Enum ConfigConnectionString)
		{
			return StandardAtomicFunctions.GetScalar<string>(ConfigConnectionString, "SELECT LAST_INSERT_ID();");
		}

		/// <summary>
		/// Converts an InternalId to an autonumbered row ID.
		/// </summary>
		/// <param name="ConfigConnectionString">The connection type to use.</param>
		/// <param name="Table">Table name to use for the conversion.</param>
		/// <param name="InternalId">The GUID of the InternalId to convert.</param>
		/// <returns>ID of the row matching the InternalId.</returns>
		internal static string GetIdFromInternalId(Enum ConfigConnectionString, string Table, string InternalId)
		{
			return StandardAtomicFunctions.GetScalar<string>(ConfigConnectionString, $"SELECT ID FROM {Table} WHERE InternalId = @InternalId", new Dictionary<string, object> { { "@InternalId", InternalId } });
		}
	}
}
