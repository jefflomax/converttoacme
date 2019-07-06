using NUnit;
using NUnit.Framework;
using Should;
using acmeconvert;
using System.Reflection;
using System.Collections.Generic;

namespace NUnitTests
{
	public class AD2500Tests
	{
		private Line _line;

		[SetUp]
		public void Setup()
		{
			_line = new ADALine();
		}

		[TestCase(" INCLUDE WWSETUP.ASM", "", "", "INCLUDE")]
		[TestCase("BITMAP:    EQU $E000", "BITMAP:", "", "EQU")]
		[TestCase("LABEL:LDA#$20", "LABEL:", "LDA", "")]
		[TestCase(" TITLE FileName", "", "", "TITLE")]
		[TestCase("LABEL:LDA #$20", "LABEL:", "LDA", "")]
		[TestCase(" LDA #$20", "", "LDA", "")]
		[TestCase("?NO_WRAP:", "?NO_WRAP:", "", "")]
		[TestCase(" DB 'U'+$80, 'U'-'@'+$80    ", "", "", "DB")]
		[TestCase(".DB 'U'+$80, 'U'-'@'+$80    ", "", "", "DB")]
		[TestCase(" DB '0:A,S,R'", "", "", "DB")]
		[TestCase(".IF DEBUGGER_PRESENT", "", "", "IF")]
		[TestCase(".IFTRUE DEBUGGER_PRESENT", "", "", "IFTRUE")]
		[TestCase(".IFFALSE DEBUGGER_PRESENT", "", "", "IFFALSE")]
		[TestCase("SKIP_NEXT_CHAR_TM:	 DB 0", "SKIP_NEXT_CHAR_TM:", "", "DB")]
		[TestCase("PADDR EQUAL $DC02	", "PADDR", "", "EQUAL")]
		[TestCase("LENG: VAR KEY_RECORD_OVER-KEY_RECORD_BUFFER", "LENG:", "", "VAR")]
		[TestCase("	.BYTE	$A2		", "", "", "BYTE")]
		[TestCase(" CMP #$DB","","CMP","")]
		[TestCase(" BEQ ?EQUAL","","BEQ","")]
		public void AD2500Test(string line, string symbol, string op, string drctv)
		{
			_line.Parse(line);
			_line.Symbol.ShouldEqual(symbol);
			_line.OpCode.ShouldEqual(op);
			_line.Directive.ShouldEqual(drctv);
		}

		[TestCase("  DB 'U;' ","","","DB", " 'U;' ")]
		[TestCase(" PUBLIC END_OF_TEXT", "", "", "PUBLIC", " END_OF_TEXT")]
		[TestCase(" EXTERN KEY_RECORD_END","","","EXTERN", " KEY_RECORD_END")]
		[TestCase(" DB 'ALEXANDER, AND THE TIMEWORKS TEAM.    '","","","DB", " 'ALEXANDER, AND THE TIMEWORKS TEAM.    '")]
		public void AD2500TestOperand( string line, string symbol, string op, string drctv, string operand )
		{
			_line.Parse(line);
			_line.Symbol.ShouldEqual(symbol);
			_line.OpCode.ShouldEqual(op);
			_line.Directive.ShouldEqual(drctv);
			_line.Operand.ShouldEqual(operand);
		}

		/// <summary>
		/// Get private method to test
		/// </summary>
		/// <param name="objectUnderTest"></param>
		/// <param name="methodName"></param>
		/// <returns></returns>
		private MethodInfo GetMethod(object objectUnderTest, string methodName)
		{
			if (string.IsNullOrWhiteSpace(methodName))
				Assert.Fail("methodName cannot be null or whitespace");

			var method = objectUnderTest.GetType()
				.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

			if (method == null)
				Assert.Fail(string.Format("{0} method not found", methodName));

			return method;
		}
	}
}