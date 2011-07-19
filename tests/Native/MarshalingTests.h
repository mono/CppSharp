
class ClassWithCopyCtor {
	int x;

public:
	ClassWithCopyCtor(int xarg) {
		x = xarg;
	}

	ClassWithCopyCtor(const ClassWithCopyCtor& f);

	static ClassWithCopyCtor Return (int x);

	int GetX ();
};

class ClassWithDtor {
	int x;

public:
	ClassWithDtor(int xarg) {
		x = xarg;
	}

	~ClassWithDtor () {
	}

	static ClassWithDtor Return (int x);

	int GetX ();
};


class ClassWithoutCopyCtor {
	int x;

public:
	ClassWithoutCopyCtor(int xarg) {
		x = xarg;
	}

	static ClassWithoutCopyCtor Return (int x);

	int GetX ();
};

class Class {
	int x;

public:
	Class (int xarg) {
		x = xarg;
	}

	void CopyFromValue (Class c) {
		x = c.x;
	}

	void CopyTo (Class *c) {
		c->x = x;
	}

	bool IsNull (Class *c) {
		return !c ? true : false;
	}

	int GetX () {
		return x;
	}

	int& GetXRef () {
		return x;
	}
};

		
		
