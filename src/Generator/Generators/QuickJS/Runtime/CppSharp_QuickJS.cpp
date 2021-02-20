#include "CppSharp_QuickJS.h"

extern "C"
{

extern void register_signal(JSContext *ctx, JSModuleDef *m, bool set, int phase);

void register_CppSharp_QuickJS(JSContext *ctx, JSModuleDef *m, bool set, int phase)
{
    register_signal(ctx, m, set, phase);
}

} // extern "C"
