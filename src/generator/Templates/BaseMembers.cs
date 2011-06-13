using System;

namespace Templates {

	public interface ITemplate {
		Generator Generator { get; set; }
		Class Class { get; set; }
		string TransformText ();
	}

	public partial class Base : ITemplate {

		public Generator Generator { get; set; }
		public Class Class { get; set; }
	}
}
