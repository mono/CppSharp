
#include "ManglingTests.h"
#include <stdio.h>

void Compression::Test1 (const Compression* a1, const char* a2, const Compression* a3, const char* a4)
{
	printf ("Compression::Test1");
}

void Ns1::Namespaced::Test1 ()
{
	printf ("Ns1::Namespaced::Test1");
}

void Ns1::Namespaced::Test2 (const Compression* a1)
{
	printf ("Ns1::Namespaced::Test2");
}

Ns1::Ns2::Namespaced2::Namespaced2 ()
{
	printf ("Ns1::Ns2::Namespaced2::Namespaced2");
}

void Ns1::Ns2::Namespaced2::Test1 ()
{
	printf ("Ns1::Ns2::Namespaced2::Test1");
}

Ns1::Ns2::Namespaced2* Ns1::Ns2::Namespaced2::Test2 (Compression* a1)
{
	printf ("Ns1::Ns2::Namespaced2::Test2");
}