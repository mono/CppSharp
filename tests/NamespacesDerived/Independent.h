class Derived;
template<typename T> class TemplateWithIndependentFields;
typedef TemplateWithIndependentFields<Derived*> ForwardedInIndependentHeader;