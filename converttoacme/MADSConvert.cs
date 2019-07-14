using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace acmeconvert
{
	public class MADSConvert : Convert
	{
		public MADSConvert
		(
			string filePath,
			StreamReader streamReader,
			Settings settings
		) : base(filePath, streamReader, settings)
		{
			_line = new MADSLine();
		}

		/// <summary>
		/// MADS directives to skip
		/// </summary>
		private readonly string[] SkipDirectives =
		{
			"OPT"
		};

		public override bool IsSkippedDirective( string directive )
		{
			return TokenIs(directive, SkipDirectives);
		}

		public override bool HasLocalSymbol
		(
			string symbol,
			string operand,
			out string operandSymbol
		)
		{
			operandSymbol = string.Empty;
			return false;
		}

		public override bool HasEQU( string directive )
		{
			// Probably don't need to rewrite
			return false;
		}

		public override bool IsInclude( string directive )
		{
			// FIL should be include source
			// LIB should be include library (Not supported)
			return TokenPrefixIs(directive, "LIB", "FIL");
		}

		public override bool IsTitle( string directive )
		{
			// This format needs auto-title
			return false;
		}

		public override bool IsDB( string directive )
		{
			return TokenPrefixIs(directive, "BYTE", "BYT");
		}

		public override bool IsDW( string directive )
		{
			return TokenPrefixIs(directive, "WORD", "WOR");
		}

		public override PsuedoOp IsDBDSDWInfix
		(
			string symbol,
			string directive
		)
		{
			// Since DB / DS has been handled above, this only
			// does a quick check for DS in the code with 
			// non DS in the token.

			// Could also include must have character
			// at start of line, cannot have token with
			// trailing :, leading ?

			if (symbol.Length < 1)
			{
				return PsuedoOp.None;
			}


			if (directive.EqualsICNoCase("DS"))
			{
				return PsuedoOp.DS;
			}

			if (directive.EqualsICNoCase("BYTE") ||
				directive.EqualsICNoCase("BYT")
			)
			{
				return PsuedoOp.DB;
			}

			if (directive.EqualsICNoCase("WORD")||
				directive.EqualsICNoCase("WOR"))
			{
				return PsuedoOp.DW;
			}

			return PsuedoOp.None;
		}

	}
}
