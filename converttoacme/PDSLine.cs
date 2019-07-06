using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace acmeconvert
{
	public class PDSLine : Line
	{
		public PDSLine()
		{
			// Directives do require a leading ".", but not =
			// Not supported: ASK, BANK, QUERY, SKIP
			// May especially need to strip out BANK

			// CBM "text converted to screen codes"
			// DB IS DFB, DEFB, BYTE, EQUB, .BYTE, .ASCII, .TEXT, TEXT, ASC, STR, 
			// DEFM, DM, DFM
			// DC (Text screens will have 7 bit set)
			// DH DHIGH (High byte only)
			// DL DLOW (Low byte only)
			// DO ... LOOP ... UNTIL
			// DefineSpace DS DFS, DEFS .BLOCK
			// DW, DFW, DEFW, .WORD
			// END (can have optional address parameter)
			// EQU EQUATE .EQU DEFL =
			// EXEC START (Define execution address, for debugging, should be skipped)
			// FREE 
			// HEX HH[ ,]...
			// IF ELSE ENDIF IFS IFF Conditional Assembly
			// [.]INCLUDE FILE (Read source)
			// [.]MACRO [.]ENDM EXITM
			// [.]ORG
			// [.]RADIX
			// REPEAT
			// SEND DLOAD DOWNLOAD
			// STRING (adds zero terminator)
			// LIST (ON, OFF), LLIST, LLST (to printer)
			// PRINTER (send bytes to printer)
			// SKP (send LF to printer)
			// PAGE, EJECT
			// TITLE TTL "nnnn
			// SUBTITLE STITLE SUBTTL "nnnn
			// WIDTH
			// LISTOPT LSTOPT
			// * is also a comment!
			// Local labels begin with !LABEL, like 2500 ? they cancel on non-local

			// PDS supports an odd range check
			// expression [from, to]

			// Don't know what MSW is (MSW -1)
			// Don't understand why we see BLOCK all over, scrub it out as a directive for now
			var directives = @"BLOCK|DB|DS|DW|ENDM|END|EQU|EXEC|HEX|INCLUDE|MACRO|MSW|ORG";
			var dirNoPrefix = @"=";

			// The doc says a local label begins with !, but RAMPAGE seems to use :
			// May need to make configurable?

			var pattern = $@"(^\s*(?!(({StandardOpcodes})\b|{dirNoPrefix})|\.?({directives})\b)(?<sym>[*]|[A-Za-z:]\w*:?))?( (\b(?<op>{StandardOpcodes})\b)|  (   (    (?<dr>([=]) | (\b\.?({directives})\b)    )   )  ) )?('.*)?";

			CompiledRegex = new Regex
			(
				pattern,
				RegexOptions.Compiled |
					RegexOptions.IgnoreCase |
					RegexOptions.ExplicitCapture |
					RegexOptions.IgnorePatternWhitespace
			);
		}

		protected override string PreSyntaxFixes( string line )
		{
			if (line.IndexOf('&') == -1 &&
				line.IndexOf('[') == -1 &&
				line.IndexOf(']') == -1
			)
			{
				return line;
			}

			// IF not in a string, convert:
			// '&' to '$'
			// '[' to '('
			// ']' to ')'
			// Technically, these only apply to the Operand, but this
			// code applies to the whole line
			// [ and ] could be used in Macros, supporting those means
			// this must change

			var chars = line.ToCharArray();
			bool inString = false;
			for(int i = 0; i < chars.Length; i++)
			{
				var c = chars[i];
				if (c == TICK)
				{
					inString = !inString;
				}

				if (!inString )
				{
					switch (c)
					{
						case '&':
							chars[i] = '$';
							break;
						case '[':
							chars[i] = '(';
							break;
						case ']':
							chars[i] = ')';
							break;
					}
				}
			}
			return new string(chars);
		}

		protected override void PostSyntaxFixes()
		{
			// This should be some mistake -- likely missing directives
			// PDS doc does say symbols must start in column 1

			// In ACME, symbols not beginning in column 1 must end in ':'
			if (Symbol.Length > 0 &&
				! Symbol.EndsWith(":") &&
				! OriginalLine.StartsWith(Symbol))
			{
				var codeIndex = Code.IndexOf(Symbol);
				Code = Code.Substring(codeIndex);
			}
		}
	}
}
