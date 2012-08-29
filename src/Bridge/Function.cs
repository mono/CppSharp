using System;
using System.Collections.Generic;

namespace Cxxi
{
	public enum CallingConvention
	{
		Default,
		C,
		StdCall,
		ThisCall,
		FastCall
	}

	public enum ParameterUsage
	{
		Unknown,
		In,
		Out,
		InOut
	}

	public class Parameter : Declaration
	{
		public Parameter()
		{
			Usage = ParameterUsage.Unknown;
			HasDefaultValue = false;
		}

		public Type Type { get; set; }
		public ParameterUsage Usage { get; set; }
		public bool HasDefaultValue { get; set; }
	}

	public class Function : Declaration
	{
		public Function()
		{
			Parameters = new List<Parameter>();
			CallingConvention = CallingConvention.Default;
			IsVariadic = false;
			IsInline = false;
		}

		public Type ReturnType { get; set; }
		public List<Parameter> Parameters { get; set; }
		public CallingConvention CallingConvention { get; set; }
		public bool IsVariadic { get; set; }
		public bool IsInline { get; set; }

		// The C# name
		public string FormattedName { get; set; }
	}
}