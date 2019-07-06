using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace acmeconvert
{
	public static class StringExtensions
	{
		public static bool EqualsCCNoCase(this string str, string currentCultureNoCase)
		{
			return str.Equals(currentCultureNoCase, StringComparison.CurrentCultureIgnoreCase);
		}

		public static bool EqualsICNoCase(this string str, string invariantCultureNoCase)
		{
			return str.Equals(invariantCultureNoCase, StringComparison.InvariantCultureIgnoreCase);
		}

		public static bool ContainsICNoCase(this string str, params string[] invaringCultureNoCase)
		{
			return invaringCultureNoCase.Any
			(
				v => str.Contains(v, StringComparison.InvariantCultureIgnoreCase)
			);
		}
	}
}
