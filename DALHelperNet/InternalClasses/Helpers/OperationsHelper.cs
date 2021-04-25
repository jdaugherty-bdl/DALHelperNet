using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers
{
    internal class OperationsHelper
    {
		private static object ConvertScalar<T>(object ScalaraValue)
		{
			if (ScalaraValue == null || ScalaraValue is DBNull)
				return default(T);
			else if (typeof(T) == typeof(string))
				return ScalaraValue.ToString();
			else if (typeof(T) == typeof(int))
				return int.TryParse(ScalaraValue.ToString(), out int scalar) ? scalar : default;
			else if (typeof(T) == typeof(long))
				return long.TryParse(ScalaraValue.ToString(), out long scalar) ? scalar : default;
			else if (typeof(T) == typeof(decimal))
				return decimal.TryParse(ScalaraValue.ToString(), out decimal scalar) ? scalar : default;
			else if (typeof(T) == typeof(float))
				return float.TryParse(ScalaraValue.ToString(), out float scalar) ? scalar : default;
			else if (typeof(T) == typeof(bool))
				return !(ScalaraValue.ToString() == "0");
			else if (typeof(T) == typeof(DateTime))
				return DateTime.TryParse(ScalaraValue.ToString(), out DateTime scalar) ? scalar : default;
			else
				return (T)ScalaraValue;
		}

		private static void AddAllParameters(this MySqlParameterCollection CommandParameters, Dictionary<string, object> Parameters)
		{
			CommandParameters
				.AddRange(
					Parameters?
						.Select(x => new MySqlParameter(x.Key, x.Value))
						.ToArray()
					??
					Enumerable
						.Empty<MySqlParameter>()
						.ToArray());
		}
	}
}
