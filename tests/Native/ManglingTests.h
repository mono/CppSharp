
class Compression {
public:
	static void Test1 (const Compression* a1, const char* a2, const Compression* a3, const char* a4);
};

namespace Ns1 {
	class Namespaced {
	public:
		static void Test1 ();
		static void Test2 (const Compression* a1);
	};
}

namespace Ns1 { namespace Ns2 {
	class Namespaced2 {
	public:
		Namespaced2 ();
		void Test1 ();
		Namespaced2* Test2 (Compression* a1);
	};
}}