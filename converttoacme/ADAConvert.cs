using System;
using System.Collections.Generic;
using System.Text;

namespace acmeconvert
{
	public class ADAConvert : Convert
	{
		public ADAConvert
		(
			string filePath,
			Settings settings
		) : base(filePath, settings)
		{
			_line = new ADALine();
		}

		/// <summary>
		/// 2500 AD directives to skip
		/// </summary>
		private readonly string[] SkipDirectives =
		{
			"PAGE",
			"PW",
			"LIST"
		};

		public override bool IsSkippedDirective( string directive )
		{
			return TokenIs(directive, SkipDirectives);
		}
	}
}
