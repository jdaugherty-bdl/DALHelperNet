using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DALHelperNet.InternalClasses.Helpers
{
    internal static class DatabaseCoreUtilities
    {
		// message for the "no DALTable attribute" exception
		internal static string NoDalTableAttributeError => "Cannot get table name from class, try adding a 'DALTable' attribute.";
		// message for the "no DALResolvable attributes" exception
		internal static string NoDalPropertyAttributeError => "Cannot find any table properties in class, try adding a 'DALResolvable' attribute.";

		internal static object ConvertScalar<T>(object ScalaraValue)
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

		internal static void AddAllParameters(this MySqlParameterCollection CommandParameters, Dictionary<string, object> Parameters)
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

		/// <summary>
		/// Creates a labmda expression to instantiate objects of type T which take two constructor parameters.
		/// </summary>
		/// <typeparam name="TArg1">First parameter type.</typeparam>
		/// <typeparam name="TArg2">Second parameter type.</typeparam>
		/// <typeparam name="T">Return type.</typeparam>
		/// <returns>An instantiation function that will create a new concrete object of type T.</returns>
		internal static Func<TArg1, TArg2, T> CreateCreatorExpression<TArg1, TArg2, T>()
		{
			// Lambda Expressions are much faster than Activator.CreateInstance when creating more than one object due to Expression caching

			// get object constructor
			var constructor = typeof(T).GetConstructor(new Type[] { typeof(TArg1), typeof(TArg2) });

			// define individual parameters
			var parameterList = new ParameterExpression[]
			{
				Expression.Parameter(typeof(TArg1)),
				Expression.Parameter(typeof(TArg2))
			};

			// create the expression
			var creatorExpression = Expression.Lambda<Func<TArg1, TArg2, T>>(Expression.New(constructor, parameterList), parameterList);

			// compile the expression
			return creatorExpression.Compile();
		}
	}
}
