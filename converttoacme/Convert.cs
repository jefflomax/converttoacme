using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Console;

namespace acmeconvert
{
	public class Convert
	{
		// Convert began as the 2500AD converter, and is now
		// the base class overridable by other converters

		private string FileName
		{
			get;
		}
		protected StreamReader file;
		// TODO: Consider code lines, comment lines, etc.
		protected int lineCounter = 0;
		protected int codeLineCounter = 0;

		/// <summary>
		/// In 2500 AD and PDS, any non-local symbol cancels all local symbols
		/// </summary>
		protected bool localSymbolsActive = false;
		protected char localSymbolPrefix = '?';

		/// <summary>
		/// Since ACME has no linker, or support for PUBLIC/EXTERN
		/// some files use the same symbol and collide.  Config
		/// specifies which to uniquify by adding a prefix abbreviation
		/// </summary>
		protected bool moduleHasGlobalSymbolsToRename;
		protected string[] globalSymbolRenames;
		protected Module module;

		protected bool ZoneSet = false;
		protected bool emitOriginalLineCommented = false;
		protected bool bit7Enabled = false;

		public enum PsuedoOp
		{
			None,
			DB,
			DS,
			DW,
			HEX
		};

		// Just for readability
		protected const char COMMENT = ';';
		protected const char TICK = '\'';
		protected const char QUOTE = '"';

		protected HashSet<string> _macros;
		protected Dictionary<string, SymbolInfo> _localSymbols;
		protected Dictionary<string, SymbolReference> _publicSymbols;
		protected Dictionary<string, SymbolReference> _externalSymbols;

		protected Line _line;

		public Convert
		(
			string filePath,
			StreamReader streamReader,
			Settings settings
		)
		{
			emitOriginalLineCommented = settings.EmitOriginal;
			moduleHasGlobalSymbolsToRename = false;
			ZoneSet = ! settings.AutoSetZone;
			globalSymbolRenames = new string[0];
			var fileNameAndExtension = Path.GetFileName
			(
				filePath
			);
			FileName = Path.GetFileNameWithoutExtension(filePath);

			foreach (var m in settings.Modules)
			{
				if (m.File.EqualsCCNoCase(fileNameAndExtension))
				{
					moduleHasGlobalSymbolsToRename = true;
					module = m;
					break;
				}
			}

			_macros = new HashSet<string>(settings.Macros);
			_localSymbols = new Dictionary<string, SymbolInfo>();
			_publicSymbols = new Dictionary<string, SymbolReference>();
			_externalSymbols = new Dictionary<string, SymbolReference>();

			file = streamReader; // new StreamReader(filePath);
		}

		public void Process()
		{
			string line;

			// Read the file and display it line by line.
			while ((line = file.ReadLine()) != null)
			{
				lineCounter++;

				_line.Parse(line);

				if (localSymbolsActive)
				{
					// The presence of a non local symbol
					// cancels all current locals
					// Skip entirely commented lines
					if (_line.HasCode)
					{
						if (HasGlobalSymbol(_line.HasSymbol, _line.Symbol))
						{
							foreach (var localSymbol in _localSymbols)
							{
								localSymbol.Value.Name = string.Empty;
							}
							localSymbolsActive = false;
						}
					}
				}

				if (_line.HasCode)
				{
					// The actual code line is the output
					codeLineCounter++;

					// MADS Auto-Set of Title
					if (!ZoneSet && !IsTitle(_line.Directive))
					{
						Zone(FileName, string.Empty, string.Empty);
					}
				}

				Substitute();
			}

			file.Close();

			foreach (var key in _publicSymbols.Keys)
			{
				WriteLine($"; PUBLIC {key}");
			}
			foreach (var key in _externalSymbols.Keys)
			{
				WriteLine($"; EXTERN {key}");
			}
		}

		public virtual bool HasGlobalSymbol( bool hasSymbol, string symbol )
		{
			// Global symbols may not begin with a ?
			if (!hasSymbol)
			{
				return false;
			}

			return !symbol.StartsWith(localSymbolPrefix);
		}

		public bool Substitute()
		{
#if false
				if (_line.OriginalLine.Contains(":LESS"))
				{
					var dummy = 0;
				}
#endif

			if (IsSkippedDirective(_line.Directive))
			{
				if (_line.HasComment)
				{
					WriteLine(_line.Comment);
				}
				return true;
			}

			// Uniquify colliding global symbol
			// Typically, choose the file not declaring the symbol PUBLIC
			// unless it's a file from a group linked together
			if (moduleHasGlobalSymbolsToRename)
			{
				var matchSymbol = MatchSymbol(_line.Symbol);
				var gst = HasGlobalSymbolToRename
				(
					matchSymbol,
					_line.Operand,
					out var globalSymbol
				);
				if (gst != GlobalSymbolType.None)
				{
					var newSymbol = $"{module.Abr}_{globalSymbol}";

					// Replace retains trailing :
					_line.Code = _line.Code.Replace(globalSymbol, newSymbol);
					if (gst == GlobalSymbolType.Operand)
					{
						_line.Operand = _line.Operand.Replace(globalSymbol, newSymbol);
					}
					if (gst == GlobalSymbolType.Symbol)
					{
						_line.Symbol = _line.Symbol.Replace(globalSymbol, newSymbol);
					}
				}
			}

			if (HasLocalSymbol(_line.Symbol, _line.Operand, out var operandSymbol))
			{
				// returns a new code
				_line.Code = RewriteLocalSymbol
				(
					_line.Code,
					_line.HasSymbol,
					_line,
					operandSymbol,
					_localSymbols
				);
			}

			// In ACME, calling a macro requires prefixing with +
			if (IsBeginMacro(_line.Directive))
			{
				if (RewriteBeginMacro(_line.Symbol, _line.Directive, _line.Comment))
				{
					return true;
				}
			}
			else if (IsMacroRewrite(_line.HasSymbol, _line.Symbol, _macros))
			{
				RewriteMacro(_line);
			}

			if (IsEndMacro(_line.Directive))
			{
				if (RewriteEndMacro(_line.Symbol, _line.Directive, _line.Comment))
				{
					return true;
				}
			}

			if (HasEQU(_line.Directive))
			{
				var rewritten = RewriteEqu(_line.Code, _line.Comment);
				if (rewritten)
				{
					return true;
				}
			}

			if (IsInclude(_line.Directive))
			{
				Source(_line.Operand, _line.Comment);
				return true;
			}

			if (IsTitle(_line.Directive))
			{
				Zone(FileName, _line.Operand, _line.Comment);
				return true;
			}

			// TODO: DB/DW... have a regular and infix version
			// from before the line was better parsed 
			// and should be combined
			if (!_line.HasSymbol && IsDBDW(_line.Directive))
			{
				DB(_line.Directive, _line.Operand, _line.Comment);
				return true;
			}

			// At start of line variation
			if (!_line.HasSymbol && IsDS(_line.Directive))
			{
				DS(_line.Code, _line.Comment);
				return true;
			}

			if (IsOrg(_line.Directive))
			{
				RewriteOrigin(_line.Code, _line.Comment);
				return true;
			}

			if (IsPublic(_line.Directive))
			{
				CacheSymbolReference
				(
					_line.Directive,
					_line.Code,
					_publicSymbols
				);
				return true;
			}

			if (IsExtern(_line.Directive))
			{
				CacheSymbolReference
				(
					_line.Directive,
					_line.Code,
					_externalSymbols
				);
				return true;
			}

			if (IsBeginConditional(_line.Directive))
			{
				RewriteStartConditional(_line.Directive, _line.Operand, _line.Comment);
				return true;
			}

			if (IsElseConditional(_line.Directive))
			{
				RewriteElseConditional(_line.Directive, _line.Operand, _line.Comment);
				return true;
			}

			if (IsEndConditional(_line.Directive))
			{
				RewriteEndConditional(_line.Comment);
				return true;
			}

			if (IsEnd(_line.Directive))
			{
				RewriteEnd(_line.Comment);
				return true;
			}

			if (IsBit7(_line.Directive))
			{
				// This feature is in both 2500AD, PDS
				// There is also DC, which sets the high bit of the
				// last char in a string (not implemented)
				ManageBit7(_line.Operand, _line.Code, _line.Comment);
				return true;
			}

			if (IsXor(_line.OpCode))
			{
				RewriteXor(_line.Code, _line.Comment);
				return true;
			}

			var psuedoOp = IsDBDSDWInfix(_line.Symbol, _line.Directive);
			if (psuedoOp == PsuedoOp.DB ||
				psuedoOp == PsuedoOp.DS ||
				psuedoOp == PsuedoOp.DW ||
				psuedoOp == PsuedoOp.HEX )
			{
				var replaced = DBDSDWInfix
				(
					_line.Symbol,
					_line.Directive,
					_line.Operand,
					_line.Comment,
					psuedoOp
				);
				if (replaced)
				{
					return true;
				}
			}

			if (IsShiftRotate(_line.OpCode, _line.Code))
			{
				var replaced = RewriteShift(_line.Code, _line.Operand, _line.Comment);
				if (replaced)
				{
					return true;
				}
			}

			Write(_line.Code, _line.Comment, allowAutoComment:false);

			return false;
		}

		public virtual void ManageBit7
		(
			string operand,
			string code,
			string comment
		)
		{
			var priorBit7 = bit7Enabled;
			if (operand.Contains("ON", StringComparison.CurrentCultureIgnoreCase))
			{
				bit7Enabled = true;
			}
			else
			{
				bit7Enabled = false;
			}
			if (priorBit7 != bit7Enabled && emitOriginalLineCommented)
			{
				Write($";BIT7 {bit7Enabled}{code}", comment);
			}
		}

		protected void Write
		(
			string code,
			string comment,
			bool allowAutoComment = true
		)
		{
			if (emitOriginalLineCommented && allowAutoComment)
			{
				WriteLine(COMMENT + _line.OriginalLine);
			}

			if (comment.Length > 0)
			{
				WriteLine($"{code}{comment}");
			}
			else
			{
				WriteLine(code);
			}
		}

		protected enum GlobalSymbolType { None, Symbol, Operand };
		protected GlobalSymbolType HasGlobalSymbolToRename
		(
			string matchSymbol,
			string operand,
			out string globalSymbol
		)
		{
			// Symbol can be in Symbol or Operand
			foreach (var s in module.Renames)
			{
				if (matchSymbol.EqualsICNoCase(s))
				{
					globalSymbol = s;
					return GlobalSymbolType.Symbol;
				}
				else if (ContainsICNoCase(operand, s))
				{
					var pattern = $@"\b{s}\b";
					var match = Regex.Match(operand, pattern, RegexOptions.IgnoreCase);
					if (match.Success)
					{
						globalSymbol = s;
						return GlobalSymbolType.Operand;
					}
				}
			}
			globalSymbol = string.Empty;
			return GlobalSymbolType.None;
		}

		public virtual PsuedoOp IsDBDSDWInfix
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
				return PsuedoOp.None;


			if (directive.EqualsICNoCase("DS"))
			{
				return PsuedoOp.DS;
			}

			if (directive.EqualsICNoCase("DB") ||
				directive.EqualsICNoCase("BYTE")
			)
			{
				return PsuedoOp.DB;
			}

			if (directive.EqualsICNoCase("DW"))
			{
				return PsuedoOp.DW;
			}

			if (directive.EqualsICNoCase("HEX"))
			{
				return PsuedoOp.HEX;
			}

			return PsuedoOp.None;
		}

		public virtual bool IsTitle( string directive )
		{
			return TokenIs(directive, "TITLE");
		}

		public virtual bool IsOrg( string directive )
		{
			return TokenPrefixIs(directive, "ORG");
		}

		public virtual bool IsBit7( string directive )
		{
			return TokenPrefixIs(directive, "BIT7");
		}

		public virtual bool IsXor( string opcode )
		{
			return TokenIs(opcode, "XOR");
		}

		public virtual bool IsPublic( string directive )
		{
			return TokenIs(directive, "PUBLIC");
		}

		public virtual bool IsExtern( string directive )
		{
			return TokenIs(directive, "EXTERN");
		}

		public virtual bool IsBeginMacro( string directive )
		{
			return false;
		}

		public virtual bool IsEndMacro( string directive )
		{
			return false;
		}

		public virtual bool IsMacroRewrite
		(
			bool hasSymbol,
			string symbol,
			HashSet<string> macros
		)
		{
			return hasSymbol &&
				macros.Count > 0 &&
				macros.Contains(symbol);
		}

		public virtual void RewriteMacro( Line line )
		{
			var codeIndex = line.Code.IndexOf(line.Symbol);
			line.Code = line.Code.Insert(codeIndex, "+");
			line.Symbol = "+" + line.Symbol;
		}


		public virtual bool IsBeginConditional( string directive )
		{
			return TokenPrefixIs
			(
				directive,
				"IF",
				"IFTRUE",
				"IFFALSE"
			);
		}

		public virtual bool IsElseConditional( string directive )
		{
			return TokenPrefixIs(directive, "ELSE");
		}

		public virtual bool IsEndConditional( string directive )
		{
			return TokenPrefixIs(directive, "ENDIF");
		}

		public virtual bool IsEnd( string directive )
		{
			return TokenPrefixIs(directive, "END");
		}

		public virtual bool IsSkippedDirective( string directive )
		{
			return false;
		}

		public virtual bool IsShiftRotate
		(
			string opcode,
			string code
		)
		{
			var tokenIsOpcode = TokenIs(opcode, "ASL", "LSR", "ROL", "ROR");
			return tokenIsOpcode;
		}

		public virtual bool IsInclude( string directive )
		{
			return TokenPrefixIs(directive, "INCLUDE");
		}

		public virtual bool IsDBDW( string directive )
		{
			return IsDB(directive) ||
					IsDW(directive) ||
					IsHEX(directive);
		}

		public virtual bool IsDB( string directive )
		{
			return TokenPrefixIs(directive, "DB", "BYTE");
		}

		public virtual bool IsHEX( string directive )
		{
			return false;
		}

		public virtual bool IsDW( string directive )
		{
			return TokenPrefixIs(directive, "DW");
		}

		public virtual bool IsDS( string directive )
		{
			return TokenPrefixIs(directive, "DS");
		}

		public virtual bool HasEQU( string directive )
		{
			return ContainsICNoCase(directive, "EQU") ||
					ContainsICNoCase(directive, "VAR");
		}

		public virtual bool HasLocalSymbol
		(
			string symbol,
			string operand,
			out string operandSymbol
		)
		{
			if (symbol.StartsWith(localSymbolPrefix))
			{
				operandSymbol = string.Empty;
				return true;
			}

			if (!operand.Contains(localSymbolPrefix))
			{
				operandSymbol = string.Empty;
				return false;
			}

			var pattern = $@"(\{localSymbolPrefix}\w+)";
			var match = Regex.Match(operand, pattern, RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				operandSymbol = string.Empty;
				return false;
			}

			operandSymbol = match.Groups[1].Value;
			return true;
		}

		protected bool TokenIs
		(
			string token,
			params string[] values
		)
		{
			return values.Any
			(
				v => token.EqualsICNoCase(v)
			);
		}

		protected bool TokenPrefixIs
		(
			string token,
			params string[] values
		)
		{
			return values.Any
			(
				v => token.EqualsICNoCase(v) ||
						token.EqualsICNoCase("." + v)
			);
		}

		protected bool ContainsICNoCase
		(
			string s,
			params string[] values
		)
		{
			return values.Any
			(
				v => s.Contains(v, StringComparison.InvariantCultureIgnoreCase)
			);
		}

		protected MatchCollection MatchesNoCase
		(
			string s,
			string pattern
		)
		{
			var matches = Regex.Matches(s, pattern, RegexOptions.IgnoreCase);
			return matches;
		}

		/// <summary>
		/// Zone doesn't permit special chars (.) or allow them
		/// in quotes.  Use just the Filename for the zone, retain
		/// the rest in a comment
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="code"></param>
		/// <param name="comment"></param>
		/// <returns></returns>
		public bool Zone
		(
			string fileName,
			string operand,
			string comment
		)
		{
			var title = operand.Trim();
			if (title.Length > 0)
			{
				var commentIndex = comment.IndexOf(COMMENT);
				comment = (comment.Length == commentIndex + 1)
					? string.Empty
					: comment.Substring(commentIndex + 1);
				if (!title.EqualsCCNoCase(fileName))
				{
					comment = COMMENT + title + " " + comment;
				}
			}

			// ACME only allows alphanumeric in zone names
			var rgx = new Regex(@"[\W]");
			var fileNameAlphaNumeric = rgx.Replace(fileName, string.Empty);

			Write($"!zone {fileNameAlphaNumeric}", comment);
			ZoneSet = true;
			return true;
		}

		public bool RewriteEqu
		(
			string code,
			string comment
		)
		{
			// MUST remove the trailing : from symbols
			// Local symbol have been converted and now 
			// have . instead of ?

			// need to extract SYMBOL[:] EQU {target}
			// (\w+) capture word chars, one or more
			// :? colon, zero or one
			// \s* whitespace, zero or more
			// EQU
			// (.*) capture all characters
			var pattern = @"(\.?\w+)(:?)(\s*)(EQU|VAR)(.*)";
			var matches = MatchesNoCase(code, pattern);
			foreach (Match match in matches)
			{
				var symbol = match.Groups[1].Value.Trim();
				var symbolDelimiter = match.Groups[2].Value;
				var postSymbolSpace = match.Groups[3].Value;
				var op = match.Groups[4].Value;
				var remaining = match.Groups[5].Value;

				if (symbolDelimiter == ":" || postSymbolSpace.Length > 0)
				{
					// If we see $+ it must change to *+
					// But in most all other cases $ (hex)
					// can't be changed to *

					var originOffsetPattern = @"(\$\s*[+-])";
					var originOffsets = MatchesNoCase(remaining, originOffsetPattern);
					foreach (Match originOffset in originOffsets)
					{
						var oOriginal = originOffset.Groups[1].Value;
						var oSwitched = oOriginal.Replace('$', '*');
						// replace with whitespace intact
						remaining = remaining.Replace(oOriginal, oSwitched);
					}
					if (op.EqualsICNoCase("VAR"))
					{
						WriteLine(";CONV VAR SUBSTITUTED with EQU");
					}
					Write($"{symbol} = {remaining}", comment);
					return true;
				}
			}
			return false;
		}

		protected void RewriteOrigin
		(
			string code,
			string comment
		)
		{
			// This checks all the terms, probaly overkill

			// .ORG $+SECTION_LENGTH-OVERLAY_SIZE
			//  ORG  $801
			// $ followed by any operator needs to become a *
			// $ followed by a number must be retained
			var orgPattern = @"^\s*(\.?ORG)\s+";
			var dollarSignPattern = @"(\$\s*[+-]|\$\s*\d+|\d+)";
			var matches = MatchesNoCase(code, orgPattern);
			foreach (Match match in matches)
			{
				var orgReplaced = code.Replace(match.Groups[1].Value, "* =");
				var parmMatches = MatchesNoCase(orgReplaced, dollarSignPattern);
				var chars = orgReplaced.ToCharArray();
				foreach (Match parmMatch in parmMatches)
				{
					var capture = parmMatch.Captures[0];
					var captureValue = capture.Value;
					var dollarIndex = captureValue.IndexOf('$');
					if (dollarIndex >= 0)
					{
						if (captureValue.Contains("+") || captureValue.Contains("-"))
						{
							if (dollarIndex >= 0)
							{
								chars[capture.Index + dollarIndex] = '*';
							}
						}
					}
				}
				InitMemoryDefaults();
				Write(new string(chars), comment);
			}
		}

		public virtual void InitMemoryDefaults()
		{
		}

		protected void CacheSymbolReference
		(
			string directive,
			string code,
			Dictionary<string, SymbolReference> dictionary
		)
		{
			var pattern = $@".*{directive}\s*([\w]+).*";
			var matches = MatchesNoCase(code, pattern);
			foreach (Match match in matches)
			{
				var symbol = match.Groups[1].Value;
				if (dictionary.ContainsKey(symbol))
				{
					WriteLine($"{COMMENT} Duplicate {directive} {symbol}");
				}
				else
				{
					var symbolRef = new SymbolReference { References = 0 };
					dictionary.Add(symbol, symbolRef);
				}
			}
		}

		protected void RewriteEnd(string comment)
		{
			Write("!eof", comment);
		}

		/// <summary>
		/// ACME doesn't support XOR, only EOR
		/// </summary>
		/// <param name="code"></param>
		/// <param name="comment"></param>
		protected void RewriteXor
		(
			string code,
			string comment
		)
		{
			Write
			(
				code.Replace
				(
					"XOR",
					"EOR",
					StringComparison.CurrentCultureIgnoreCase
				),
				comment
			);
		}

		/// <summary>
		/// ACME doesn't support SHIFTOPCODE A
		/// </summary>
		/// <param name="code"></param>
		/// <param name="comment"></param>
		/// <returns></returns>
		protected bool RewriteShift
		(
			string code,
			string operand,
			string comment
		)
		{
			// Although the " A" that is allowed in 2500AD but not
			// in ACME is in the operand, but we don't know the
			// offset of the operand in the code, and code could
			// have changed, and could have syntax from ACME and not
			// valid int 2500AD put in it, so it can't just be
			// re-evaluated.

			// Quick test for signature in Operand
			var pattern1 = @"\s*A\b";
			var m1 = Regex.Match(operand, pattern1, RegexOptions.IgnoreCase);
			if (!m1.Success)
			{
				return false;
			}

			// Capture Group inside capture group
			// We won't rewrite if we don't have the "whitespace A"
			var pattern = @"((ASL|LSR|ROL|ROR)\s*A)(.?)";
			var matches = MatchesNoCase(code, pattern);
			foreach (Match match in matches)
			{
				var opcodeAndOperand = match.Groups[1].Value;
				var opcode = match.Groups[2].Value;
				var trailing = match.Groups[3].Value;

				var trailingValid = trailing.Length == 0;
				if (!trailingValid)
				{
					trailingValid = char.IsWhiteSpace(trailing[0]);
				}

				if (trailingValid)
				{
					code = code.Replace(opcodeAndOperand, opcode);

					Write($"{code}", comment);
					return true;
				}
			}
			return false;
		}

		public virtual string FillDefaultValue()
		{
			return string.Empty;
		}

		protected bool DBDSDWInfix
		(
			string symbol,
			string directive,
			string operand,
			string comment,
			PsuedoOp op
		)
		{
			if (string.IsNullOrWhiteSpace(symbol) ||
				string.IsNullOrEmpty(directive) ||
				string.IsNullOrEmpty(operand))
			{
				WriteLine($";CONV Mis-Identified DBDSDW Infix");
				return false;
			}

			// Infix writes symbol, newline, then directive
			WriteLine(symbol);
			if (op == PsuedoOp.DS)
			{
				Write($"!fill {operand}{FillDefaultValue()}", comment);
			}
			else if (op == PsuedoOp.DB || op == PsuedoOp.DW)
			{
				// Infix DB, can be multi-value
				// operand is the expressions
				DB
				(
					directive,
					operand,
					comment
				);
			}
			return true;
		}

		protected string RewriteLocalSymbol
		(
			string code,
			bool hasSymbol,
			Line curLine,
			string operandSymbol,
			Dictionary<string, SymbolInfo> localSymbols
		)
		{
			string rewriteSymbol;
			if (hasSymbol)
			{
				var matchSymbol = MatchSymbol(curLine.Symbol);
				// All numeric symbols not allowed in ACME
				var matchSymbolNeedsPrefix = SymbolNeedsPrefix(matchSymbol);

				if (localSymbols.TryGetValue(matchSymbol, out var symbolInfo))
				{
					// If local symbols have been cleared, Name will be empty
					// And this redefinition will need a new numeric 
					// suffix to uniquify it.
					if (symbolInfo.Name.Length > 0)
					{
						// Symbol was used before definition
						rewriteSymbol = symbolInfo.Name;
						symbolInfo.LineDefined = codeLineCounter;
					}
					else
					{
						// Symbol is being redefined, append lineCounter
						var newSymbol = NewLocalSymbol
						(
							matchSymbol,
							matchSymbolNeedsPrefix,
							lineCounter
						);

						symbolInfo.Name = newSymbol;
						rewriteSymbol = symbolInfo.Name;
						symbolInfo.LineDefined = codeLineCounter;
					}
				}
				else
				{
					// Initial definition, no line number appended
					var newSymbol = NewLocalSymbol(matchSymbol, matchSymbolNeedsPrefix);
					var si = new SymbolInfo
					{
						LineDefined = lineCounter,
						Name = newSymbol
					};
					localSymbols.Add(matchSymbol, si);
					rewriteSymbol = newSymbol;
				}

				var acmeLocalSymbol = "." + rewriteSymbol + ":";
				curLine.Code = code.Replace(curLine.Symbol, acmeLocalSymbol);
				curLine.Symbol = acmeLocalSymbol;

				localSymbolsActive = true;
				return curLine.Code;
			}
			else
			{
				// Local Symbol in operand
				var symbol = operandSymbol;
				var matchSymbol = MatchSymbol(symbol);

				// All numeric symbols not allowed in ACME
				var matchSymbolNeedsPrefix = SymbolNeedsPrefix(matchSymbol);

				if (localSymbols.TryGetValue(matchSymbol, out var symbolInfo))
				{
					if (symbolInfo.Name.Length > 0)
					{
						rewriteSymbol = symbolInfo.Name;
					}
					else
					{
						// Symbol was used in previous local block
						// and is now being reused before it's new
						// declaration
						rewriteSymbol = NewLocalSymbol
						(
							matchSymbol,
							matchSymbolNeedsPrefix,
							lineCounter
						);

						symbolInfo.Name = rewriteSymbol;
					}

					symbolInfo.LineDefined = 0;
				}
				else
				{
					// New local symbol used before declaration

					// Create symbol, no line # suffix
					rewriteSymbol = NewLocalSymbol(matchSymbol, matchSymbolNeedsPrefix);
					var si = new SymbolInfo
					{
						Name = rewriteSymbol,
						LineDefined = 0
					};
					localSymbols.Add(matchSymbol, si);
				}

				var newSymbol = "." + rewriteSymbol;
				code = code.Replace(symbol, newSymbol);
				//doing this managed to assemble but crash on launch
				//no idea why it isn't safe to update Operand
				//curLine.Operand = curLine.Operand.Replace(symbol, newSymbol);
				//WriteLine($";CONVX <{oldCode}> <{oldOperand}> <{curLine.Operand.Replace(symbol, newSymbol)}>");

				localSymbolsActive = true;
				return code;
			}
			throw new Exception($"Unprocessed local symbol {lineCounter}");
		}

		protected string NewLocalSymbol
		(
			string matchSymbol,
			bool addPrefix,
			int currentLine = -1
		)
		{
			var fixNumeric = (addPrefix)
				? "NUM"
				: string.Empty;

			var newSymbol = (currentLine >= 0)
				? $"{fixNumeric}{matchSymbol}{currentLine:0000}"
				: $"{fixNumeric}{matchSymbol}";
			return newSymbol;
		}


		protected bool SymbolNeedsPrefix( string s )
		{
			if (s.Length == 0)
			{
				throw new Exception("Empty Local Symbol");
			}

			return char.IsDigit(s[0]);
		}

		protected string MatchSymbol( string symbol )
		{
			var start = 0;
			var length = symbol.Length;
			if (symbol.StartsWith(localSymbolPrefix))
			{
				start++;
				length--;
			}
			if (symbol.EndsWith(':'))
			{
				length--;
			}
			return symbol.Substring(start, length);
		}

		public void Source
		(
			string operand,
			string comment
		)
		{
			var fileName = operand.Trim();
			var extension = Path.GetExtension(fileName);
			if (extension.Length == 0)
			{
				fileName += ".ASM";
			}
			Write($"!SOURCE \"{fileName}\"", comment);
		}

		public virtual bool RewriteBeginMacro
		(
			string symbol,
			string directive,
			string comment
		)
		{
			return false;
		}

		public virtual bool RewriteEndMacro
		(
			string symbol,
			string directive,
			string comment
		)
		{
			return false;
		}


		public void RewriteStartConditional
		(
			string directive,
			string operand,
			string comment
		)
		{
			// Negate condition if .IFFALSE
			// IF, IFTRUE, .IFTRUE all the same
			// TODO: Is IFFALSE possible?
			var negate = (TokenIs(directive, "IFFALSE"))
				? "!"
				: "";

			WriteLine($"!if {negate}{operand} {{ {comment}");
		}

		public void RewriteEndConditional(string comment)
		{
			WriteLine($"}} {comment}");
		}

		public void RewriteElseConditional
		(
#pragma warning disable IDE0060 // Remove unused parameter
			string directive,
			string operand,
#pragma warning restore IDE0060 // Remove unused parameter
			string comment
		)
		{
			WriteLine($"}} else {{ {comment}");
		}

		public void DB
		(
			string directive,
			string operand,
			string comment
		)
		{
			// We need to collect each expression delimited by ,
			// If there is any string 'xx...' it must change to "xx..."
			// and if there is any string, we switch from !byte to !text
			// and by need to consider the translation table
			// DW will have some similarities, but include processing $ to *
			// In my code, there were no '' inside a 'xxx'
			// but we will assume tokens for ', including ''
			// Most importantly, we must retain any trailing comment
			// and must not consider a ; character or character in string 
			// a comment.

			if (IsHEX(directive))
			{
				// operand should contain a hex string
				// 	HEX "6021382261"
				var startQuote = operand.IndexOf('"');
				var endQuote = operand.LastIndexOf('"');
				if (startQuote == -1 || endQuote == -1)
				{
					throw new Exception($"Invalid HEX string: {operand}");
				}
				startQuote++;
				var bytes = new List<string>((endQuote - startQuote) / 2);
				for (var i = startQuote; i < endQuote; i+=2)
				{
					bytes.Add("$" + operand.Substring(i, 2));
				}
				Write($"!byte {string.Join(',', bytes)}", comment);
				return;
			}


			// TODO: No need to return trailingComment
			var (expressions, trailingComment) = CollectExpressions(operand, bit7Enabled);
			var hasString = expressions.Any(e => e.StartsWith(QUOTE));

			if (IsDW(directive))
			{
				if (hasString)
				{
					throw new Exception($"Unexpected DW {operand}");
				}
				Write($"!word {string.Join(',', expressions)}", comment);
				return;
			}


			if (hasString)
			{
				Write($"!text {string.Join(',', expressions)}", comment);
			}
			else
			{
				// ACME doesn't allow # in !byte
				var cleanedList = expressions.Any(e => e.IndexOf('#') >= 0)
					? RemoveImmediateMode(expressions)
					: expressions;
				Write($"!byte {string.Join(',', cleanedList)}", comment);
			}
		}

		public void DS
		(
			string code,
			string comment
		)
		{
			var pattern = @".*DS\s*([\w$ +-]+).*";
			var matches = MatchesNoCase(code, pattern);
			foreach (Match match in matches)
			{
				var fillBytes = match.Groups[1].Value;

				Write($"!fill {fillBytes}{FillDefaultValue()}", comment);
			}
		}

		public (List<string>, string) CollectExpressions
		(
			string line,
			bool bit7Enabled
		)
		{
			var expressions = new List<string>();
			var trailingComment = string.Empty;

			var stringOrChar = new StringBuilder();
			var expression = new StringBuilder();

			var inCharOrString = false;
			var potentialEos = false;

			for (var index = 0; index < line.Length; index++)
			{
				var ch = line[index];

				if (potentialEos)
				{
					if (ch == TICK)
					{
						continue;
					}
					else
					{
						// String or Char
						StringToExpression(TICK, QUOTE, stringOrChar, expression, bit7Enabled);
						stringOrChar.Clear();
						potentialEos = false;
						inCharOrString = false;
					}
				}


				if (!inCharOrString)
				{
					if (char.IsWhiteSpace(ch))
					{
						continue;
					}

					if (ch == ',')
					{
						expressions.Add(expression.ToString());
						expression.Clear();
						continue;
					}

					if (ch == COMMENT)
					{
						// trailing comment
						if (expression.Length > 0)
						{
							expressions.Add(expression.ToString());
							trailingComment = line.Substring(index);
							return (expressions, trailingComment);
						}
					}
				}

				if (ch == TICK)
				{
					if (inCharOrString)
					{
						// likely the string terminator
						// but could be '' in string
						potentialEos = true;
					}
					else
					{
						inCharOrString = true;
					}
				}
				else
				{
					if (!inCharOrString)
					{
						expression.Append(ch);
					}
					else
					{
						stringOrChar.Append(ch);
					}
				}
			}

			if (potentialEos)
			{
				// End of string was EOL
				StringToExpression(TICK, QUOTE, stringOrChar, expression, bit7Enabled);
			}

			if (expression.Length > 0)
			{
				expressions.Add(expression.ToString());
			}

			return (expressions, trailingComment);
		}

		protected static void StringToExpression
		(
			char tick,
			char quote,
			StringBuilder stringOrChar,
			StringBuilder expression,
			bool bit7Enabled
		)
		{
			if (bit7Enabled)
			{	// ACME enables High Bit set only on strings
				var hexWithHiBitSet = stringOrChar.ToString()
					.Select(ch => string.Format("${0:X}", ((int)ch) | 128));
				var result = string.Join(",", hexWithHiBitSet);
				expression.Append(result);
				return;
			}

			if (stringOrChar.Length > 1)
			{
				expression.Append($"{quote}{stringOrChar.ToString()}{quote}");
			}
			else
			{
				expression.Append($"{tick}{stringOrChar.ToString()}{tick}");
			}
		}

		protected IEnumerable<string> RemoveImmediateMode
		(
			IList<string> expressions
		)
		{
			// DB is not allowed parameter with # in ACME
			// unless it's in a char, '#'
			return expressions.Select
			(
				s =>
				(s.IndexOf('#') >= 0 && s.IndexOf("'#") < 0
			)
				? s.Replace("#", "")
				: s);
		}


		protected class SymbolInfo
		{
			public int LineDefined
			{
				get; set;
			}
			public string Name
			{
				get; set;
			}
		}

		protected class SymbolDefinition
		{
			public SymbolDefinition()
			{
			}
			public SymbolDefinition( int codeLine )
			{
				LineDefined = codeLine;
			}
			public int LineDefined
			{
				get; set;
			}
		}

		protected class SymbolReference
		{
			public int References
			{
				get; set;
			}
		}
	}
}
