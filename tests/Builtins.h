#include <cstddef>
#include <cstdint>

void            ReturnsVoid () { }

std::nullptr_t  ReturnsNullptr () { return nullptr; }
std::nullptr_t  PassAndReturnsNullptr (std::nullptr_t t) { return t; }

bool            ReturnsBool () { return true; }
bool            PassAndReturnsBool (bool v) { return v; }

// Character types
char            ReturnsChar   () { return 'a'; }
signed char     ReturnsSChar  () { return 'a'; }
unsigned char   ReturnsUChar  () { return 'a'; }
char            PassAndReturnsChar   (char v)          { return v; }
signed char     PassAndReturnsSChar  (signed char v)   { return v; }
unsigned char   PassAndReturnsUChar  (unsigned char v) { return v; }


wchar_t         ReturnsWChar  () { return 'a'; }
#if __cplusplus > 201703L
char8_t         ReturnsChar8  () { return 'a'; }
#endif
char16_t        ReturnsChar16 () { return 'a'; }
char32_t        ReturnsChar32 () { return 'a'; }

// Floating-point types
float           ReturnsFloat      () { return  5.0; }
double          ReturnsDouble     () { return -5.0; }
long double     ReturnsLongDouble () { return -5.0; }

float           PassAndReturnsFloat      (float  v)      { return  v; }
double          PassAndReturnsDouble     (double v)      { return v; }
long double     PassAndReturnsLongDouble (long double v) { return v; }

// Integer types
int8_t          ReturnsInt8   () { return -5; }
uint8_t         ReturnsUInt8  () { return  5; }
int16_t         ReturnsInt16  () { return -5; }
uint16_t        ReturnsUInt16 () { return  5; }
int32_t         ReturnsInt32  () { return -5; }
uint32_t        ReturnsUInt32 () { return  5; }
#if !defined(__EMSCRIPTEN__)
int64_t         ReturnsInt64  () { return -5; }
uint64_t        ReturnsUInt64 () { return  5; }
#endif

int8_t          PassAndReturnsInt8   (int8_t   v) { return v; }
uint8_t         PassAndReturnsUInt8  (uint8_t  v) { return v; }
int16_t         PassAndReturnsInt16  (int16_t  v) { return v; }
uint16_t        PassAndReturnsUInt16 (uint16_t v) { return v; }
int32_t         PassAndReturnsInt32  (int32_t  v) { return v; }
uint32_t        PassAndReturnsUInt32 (uint32_t v) { return v; }
int64_t         PassAndReturnsInt64  (int64_t  v) { return v; }
uint64_t        PassAndReturnsUInt64 (uint64_t v) { return v; }

// Pointer types
const char* ReturnsConstCharPtr() { return "Hello"; }
const char* PassAndReturnsConstCharPtr(const char* ptr) { return ptr; }


