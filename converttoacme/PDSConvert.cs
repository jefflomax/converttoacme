using System;
using System.Collections.Generic;
using System.Text;

namespace acmeconvert
{
	public class PDSConvert : Convert
	{
		// [] in operands illegal, currently replaced with () but that isn't working
		// Verify: for RAMP.ASM SBI	LDA #-8, verify ACME makes 2's complement

		public PDSConvert
		(
			string filePath,
			Settings settings
		) : base(filePath, settings)
		{
			_line = new PDSLine();

			// PDS Manual says the local symbol is !, but Rampage
			// source looks like it is : (and even with that, has some issues)
			base.localSymbolPrefix = ':';
		}

		/// <summary>
		/// MADS directives to skip
		/// </summary>
		private readonly string[] SkipDirectives =
		{
			"BLOCK",
			"EXEC",
			"MSW"
		};


		public override bool IsSkippedDirective( string directive )
		{
			return TokenIs(directive, SkipDirectives);
		}

		public override bool HasEQU( string directive )
		{
			return ContainsICNoCase(directive, "EQU");
		}

		public override bool IsHEX( string directive )
		{
			return TokenPrefixIs(directive, "HEX");
		}

		public override bool IsTitle( string directive )
		{
			// This format needs auto-title
			return false;
		}

		public override bool IsBeginMacro( string directive )
		{
			return TokenIs(directive, "MACRO");
		}

		public override bool IsEndMacro( string directive )
		{
			return TokenIs(directive, "ENDM");
		}

		public override bool RewriteBeginMacro
		(
			string symbol,
			string directive,
			string comment
		)
		{
			if (!_macros.Contains(symbol))
			{
				_macros.Add(symbol);
			}

			Write($"!macro {symbol} {{", comment);
			return true;
		}

		public override bool RewriteEndMacro
		(
			string symbol,
			string directive,
			string comment
		)
		{
			Write($"}}", comment);
			return true;
		}
	}
}
