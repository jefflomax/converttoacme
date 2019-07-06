using NUnit;
using NUnit.Framework;
using Should;
using acmeconvert;
using System.Reflection;
using System.Collections.Generic;

namespace NUnitTests
{
	public class PDSTests
	{
		private Line _line;

		[SetUp]
		public void Setup()
		{
			_line = new PDSLine();
		}

		[TestCase("	ORG &7800 ", "", "", "ORG", " $7800 ")]
		[TestCase("BL	EQU 252	; BLANK SPRITE &FF00", "BL", "", "EQU", " 252	")]
		public void PDSTestOperand( string line, string symbol, string op, string drctv, string operand )
		{
			_line.Parse(line);
			_line.Symbol.ShouldEqual(symbol);
			_line.OpCode.ShouldEqual(op);
			_line.Directive.ShouldEqual(drctv);
			_line.Operand.ShouldEqual(operand);
		}

		[TestCase("NOPP	MACRO", "NOPP", "", "MACRO")]
		[TestCase("	DB &2C", "", "", "DB")]
		[TestCase("BLOCKS		EQU &1000","BLOCKS","","EQU")]
		[TestCase("	EXEC MAIN       	","","","EXEC")]
		[TestCase(":LESS", ":LESS", "", "")]
		public void PDSTest( string line, string symbol, string op, string drctv )
		{
			_line.Parse(line);
			_line.Symbol.ShouldEqual(symbol);
			_line.OpCode.ShouldEqual(op);
			_line.Directive.ShouldEqual(drctv);
		}

		[TestCase("	DSECT", "DSECT", "", "","DSECT")]
		public void PDSTestCode( string line, string symbol, string op, string drctv, string code )
		{
			_line.Parse(line);
			_line.Symbol.ShouldEqual(symbol);
			_line.OpCode.ShouldEqual(op);
			_line.Directive.ShouldEqual(drctv);
			_line.Code.ShouldEqual(code);
		}

	}
}
