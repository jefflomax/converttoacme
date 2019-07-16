using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace acmeconvert
{
	// In 2500AS, * = *+SECTION_LENGTH-OVERLAY_SIZE filled with $00
	// But in ACME it's filling with $FF
	// !initmem $ff seems like the best solution, added before ORG emit
	public class ADAConvert : Convert
	{
		public ADAConvert
		(
			string filePath,
			StreamReader streamReader,
			Settings settings
		) : base(filePath, streamReader, settings)
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

		public override string FillDefaultValue()
		{
			return ", $FF";
		}

		public override void InitMemoryDefaults()
		{
			Write("!initmem $FF","");
		}
	}
}
