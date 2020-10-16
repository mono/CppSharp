#pragma once

class ClassUsingUnion {
public:
	union {
		float arr[2];
		struct {
			float a, b;
		};
	};
};
