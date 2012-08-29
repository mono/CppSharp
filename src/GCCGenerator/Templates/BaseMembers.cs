using System;
using System.Collections.Generic;

namespace Templates {

	public interface ITemplate<T> {
		Generator Generator { get; set; }
		T Current { get; set; }
		bool Nested { get; set; }
		string TransformText ();
	}

	public partial class LibsBase : Base, ITemplate<ICollection<Lib>> {

		public Generator Generator { get; set; }
		public ICollection<Lib> Current { get; set; }
		public bool Nested { get; set; }
		public ICollection<Lib> Libs { get { return Current; } set { Current = value; } }
	}

	public partial class ClassBase : Base, ITemplate<Class> {

		public Generator Generator { get; set; }
		public Class Current { get; set; }
		public bool Nested { get; set; }
		public Class Class { get { return Current; } set { Current = value; } }
	}

	public partial class EnumBase : Base, ITemplate<Enumeration> {

		public Generator Generator { get; set; }
		public Enumeration Current { get; set; }
		public bool Nested { get; set; }
		public Enumeration Enum { get { return Current; } set { Current = value; } }
	}
}
