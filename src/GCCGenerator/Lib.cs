using System;
using System.Collections.Generic;

using Mono.Cxxi;

public class Lib {

	public Lib ()
	{
		Namespaces = new List<Namespace> ();
	}

	public string BaseName {
		get;
		set;
	}

	public InlineMethods InlinePolicy {
		get;
		set;
	}

	public string BaseNamespace {
		get;
		set;
	}

	public IList<Namespace> Namespaces {
		get;
		set;
	}
}

