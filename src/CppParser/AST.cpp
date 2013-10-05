/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#include "AST.h"
#include <algorithm>
#include <string>
#include <vector>

template<typename T>
static std::vector<T> split(const T & str, const T & delimiters) {
    std::vector<T> v;
    typename T::size_type start = 0;
    auto pos = str.find_first_of(delimiters, start);
    while(pos != T::npos) {
        if(pos != start) // ignore empty tokens
            v.emplace_back(str, start, pos - start);
        start = pos + 1;
        pos = str.find_first_of(delimiters, start);
    }
    if(start < str.length()) // ignore trailing delimiter
       // add what's left of the string
        v.emplace_back(str, start, str.length() - start);
    return v;
}

namespace CppSharp { namespace CppParser {

Declaration* DeclarationContext::FindAnonymous(uint64_t key)
{
    auto it = Anonymous.find(key);
    return (it != Anonymous.end()) ? it->second : 0;
}

Namespace* DeclarationContext::FindNamespace(const std::string& Name)
{
    auto namespaces = split<std::string>(Name, "::");
    return FindNamespace(namespaces);
}

Namespace*
DeclarationContext::FindNamespace(const std::vector<std::string>& Namespaces)
{
    auto currentNamespace = this;
    for (auto I = Namespaces.begin(), E = Namespaces.end(); I != E; ++I)
    {
        auto& _namespace = *I;

        auto childNamespace = std::find_if(currentNamespace->Namespaces.begin(),
            currentNamespace->Namespaces.end(),
            [&](CppSharp::CppParser::Namespace* ns) {
                return ns->Name == _namespace;
        });

        if (childNamespace == currentNamespace->Namespaces.end())
            return nullptr;

        currentNamespace = *childNamespace;
    }

    return (CppSharp::CppParser::Namespace*) currentNamespace;
}

Namespace* DeclarationContext::FindCreateNamespace(const std::string& Name)
{
    auto _namespace = FindNamespace(Name);

    if (!_namespace)
    {
        _namespace = new Namespace();
        _namespace->Name = Name;
        _namespace->_Namespace = this;

        Namespaces.push_back(_namespace);
    }

    return _namespace;
}

Class* DeclarationContext::FindClass(const std::string& Name)
{
    if (Name.empty()) return nullptr;

    auto entries = split<std::string>(Name, "::");

    if (entries.size() == 1)
    {
        auto _class = std::find_if(Classes.begin(), Classes.end(),
            [&](Class* klass) { return klass->Name == Name; });

        return _class != Classes.end() ? *_class : nullptr;
    }

    auto className = entries[entries.size() - 1];

    std::vector<std::string> namespaces;
    std::copy_n(entries.begin(), entries.size() - 1, std::back_inserter(namespaces));

    auto _namespace = FindNamespace(namespaces);
    if (!_namespace)
        return nullptr;

    return _namespace->FindClass(className);
}

Class* DeclarationContext::CreateClass(std::string Name, bool IsComplete)
{
    auto _class = new Class();
    _class->Name = Name;
    _class->_Namespace = this;
    _class->IsIncomplete = !IsComplete;

    return _class;
}

Class* DeclarationContext::FindClass(const std::string& Name, bool IsComplete,
        bool Create)
{
    auto _class = FindClass(Name);

    if (!_class)
    {
        if (Create)
        {
            _class = CreateClass(Name, IsComplete);
            Classes.push_back(_class);
        }
        
        return _class;
    }

    if (_class->IsIncomplete == !IsComplete)
        return _class;

    if (!Create)
        return nullptr;

    auto newClass = CreateClass(Name, IsComplete);

    // Replace the incomplete declaration with the complete one.
    if (_class->IsIncomplete)
    {
        _class->CompleteDeclaration = newClass;

        std::replace_if(Classes.begin(), Classes.end(),
            [&](Class* klass) { return klass == _class; }, newClass);
    }

    return newClass;
}

Enumeration* DeclarationContext::FindEnum(const std::string& Name, bool Create)
{
    if (Name.empty()) return nullptr;

    auto entries = split<std::string>(Name, "::");

    if (entries.size() == 1)
    {
        auto foundEnum = std::find_if(Enums.begin(), Enums.end(),
            [&](Enumeration* _enum) { return _enum->Name == Name; });

        if (foundEnum != Enums.end())
            return *foundEnum;

        if (!Create)
            return nullptr;

        auto _enum = new Enumeration();
        _enum->Name = Name;
        _enum->_Namespace = this;
        Enums.push_back(_enum);
        return _enum;
    }

    auto enumName = entries[entries.size() - 1];

    std::vector<std::string> namespaces;
    std::copy_n(entries.begin(), entries.size() - 1, std::back_inserter(namespaces));

    auto _namespace = FindNamespace(namespaces);
    if (!_namespace)
        return nullptr;

    return _namespace->FindEnum(enumName, Create);
}

Function* DeclarationContext::FindFunction(const std::string& Name, bool Create)
{
    auto foundFunction = std::find_if(Functions.begin(), Functions.end(),
            [&](Function* func) { return func->Name == Name; });

    if (foundFunction != Functions.end())
        return *foundFunction;

    if (!Create)
        return nullptr;
     
    auto function = new Function();
    function->Name = Name;
    function->_Namespace = this;
    Functions.push_back(function);

    return function;
}

TypedefDecl* DeclarationContext::FindTypedef(const std::string& Name, bool Create)
{
    auto foundTypedef = std::find_if(Typedefs.begin(), Typedefs.end(),
            [&](TypedefDecl* tdef) { return tdef->Name == Name; });

    if (foundTypedef != Typedefs.end())
        return *foundTypedef;

    if (!Create)
        return nullptr;
     
    auto tdef = new TypedefDecl();
    tdef->Name = Name;
    tdef->_Namespace = this;
    Typedefs.push_back(tdef);

    return tdef;
}

TranslationUnit* Library::FindOrCreateModule(const std::string& File)
{
    auto existingUnit = std::find_if(TranslationUnits.begin(),
        TranslationUnits.end(), [&](TranslationUnit* unit) {
            return unit && unit->FileName == File;
    });

    if (existingUnit != TranslationUnits.end())
        return *existingUnit;

    auto unit = new TranslationUnit();
    unit->FileName = File;
    TranslationUnits.push_back(unit);

    return unit;
}

NativeLibrary* Library::FindOrCreateLibrary(const std::string& File)
{
    auto existingLib = std::find_if(Libraries.begin(),
        Libraries.end(), [&](NativeLibrary* lib) {
            return lib && lib->FileName == File;
    });

    auto lib = new NativeLibrary();
    lib->FileName = File;
    Libraries.push_back(lib);

    return lib;
}

} }