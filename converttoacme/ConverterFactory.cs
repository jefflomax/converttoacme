using System;
using System.Collections.Generic;
using System.Text;

namespace acmeconvert
{
	public class ConverterFactory
	{
		public Convert CreateConverter
		(
			Formats format,
			string fileToConvert,
			Settings settings
		)
		{
			switch (format)
			{
				case Formats.AD2500:
					return new ADAConvert
					(
						fileToConvert,
						settings
					);

				case Formats.MADS:
					return new MADSConvert
					(
						fileToConvert,
						settings
					);

				case Formats.PDS:
					return new PDSConvert
					(
						fileToConvert,
						settings
					);

				default:
					throw new Exception("Invalid format requested");
			}
		}
	}
}
