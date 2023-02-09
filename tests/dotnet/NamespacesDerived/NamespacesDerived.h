#include "../Tests.h"
#include "../NamespacesBase/NamespacesBase.h"
#include "Independent.h"
#include <string>
#include <unordered_map>
#include <vector>

// Namespace clashes with NamespacesBase.OverlappingNamespace
// Test whether qualified names turn out right.
namespace OverlappingNamespace
{
    class DLL_API InDerivedLib
    {
    public:
        InDerivedLib();
        Base parentNSComponent;
        ColorsEnum color;
    };
}


// Using a type imported from a different library.
class DLL_API Derived : public Base2
{
public:
    Derived();

    Base baseComponent;
    Base getBase();
    void setBase(Base);

    void parent(int i);

    OverlappingNamespace::InBaseLib nestedNSComponent;
    OverlappingNamespace::InBaseLib getNestedNSComponent();
    void setNestedNSComponent(OverlappingNamespace::InBaseLib);

    OverlappingNamespace::ColorsEnum color;

private:
    int d;
};

// For reference: using a type derived in the same library
class Base3
{
};

template <typename T> class TemplateClass;

class DLL_API Derived2 : public Base3
{
public:
    Base3 baseComponent;
    Base3 getBase();
    void setBase(Base3);

    OverlappingNamespace::InDerivedLib nestedNSComponent;
    OverlappingNamespace::InDerivedLib getNestedNSComponent();
    void setNestedNSComponent(OverlappingNamespace::InDerivedLib);
    void defaultEnumValueFromDependency(OverlappingNamespace::ColorsEnum c = OverlappingNamespace::ColorsEnum::black);

    TemplateClass<int> getTemplate();
    IndependentFields<int> getIndependentSpecialization();
    IndependentFields<void*> getPointerOnlySpecialization() { return IndependentFields<void*>(); }
    typedef DependentFields<int> LocalTypedefSpecialization;
    LocalTypedefSpecialization getLocalTypedefSpecialization();
    Abstract* getAbstract();
private:
    TemplateClass<int> t;
    TemplateClass<Derived> d;
    TemplateClass<DependentFields<Derived>> nestedSpecialization;
    IndependentFields<int> independentSpecialization;
    IndependentFields<Derived> independentExternalSpecialization;
    IndependentFields<Derived*> independentExternalSpecializationPointer;
    IndependentFields<Derived>::Nested nestedInExternalSpecialization;
    std::unordered_map<int, Derived> externalSpecializationOnly;
};

class DLL_API HasVirtualInDependency : public HasVirtualInCore
{
public:
    HasVirtualInDependency* managedObject;
    int callManagedOverride();
};

class DLL_API DerivedFromExternalSpecialization : public DependentFields<Derived>
{
public:
    DerivedFromExternalSpecialization(int i,
                                      DependentFields<HasVirtualInDependency> defaultExternalSpecialization =
                                          DependentFields<HasVirtualInDependency>());
    DependentFields<Base3> returnExternalSpecialization();
};

class DLL_API DerivedFromSecondaryBaseInDependency : public Derived, public SecondaryBase
{
};

template<typename T>
class CustomAllocator
{
public:
    typedef T value_type;

    T* allocate(size_t cnt, const void* = 0) { return 0; }
    void deallocate(T* p, size_t cnt) {}
    bool operator==(const CustomAllocator&) { return true; }
};

class DLL_API Ignored
{
private:
    std::basic_string<char, std::char_traits<char>, CustomAllocator<char>> customAllocatedString;
};

template<class T, class Alloc = CustomAllocator<T>>
using vector = ::std::vector<T, Alloc>;

class DLL_API StdFields
{
private:
   vector<unsigned int, CustomAllocator<unsigned int>> customAllocatedVector;
};

DLL_API bool operator<<(const Base& b, const char* str);

namespace NamespacesBase
{
    class DLL_API ClassInNamespaceNamedAfterDependency
    {
    private:
        Base base;
    };
}

/// Hash set/map base class.
/** Note that to prevent extra memory use due to vtable pointer, %HashBase intentionally does not declare a virtual destructor
and therefore %HashBase pointers should never be used.
*/
class DLL_API TestComments
{
public:
    //----------------------------------------------------------------------
    /// Get the string that needs to be written to the debugger stdin file
    /// handle when a control character is typed.
    ///
    /// Some GUI programs will intercept "control + char" sequences and want
    /// to have them do what normally would happen when using a real
    /// terminal, so this function allows GUI programs to emulate this
    /// functionality.
    ///
    /// @param[in] ch
    ///     The character that was typed along with the control key
    ///
    /// @return
    ///     The string that should be written into the file handle that is
    ///     feeding the input stream for the debugger, or NULL if there is
    ///     no string for this control key.
    //----------------------------------------------------------------------
    const char* GetIOHandlerControlSequence(char ch);

    //------------------------------------------------------------------
    /// Attach to a process by name.
    ///
    /// This function implies that a future call to SBTarget::Attach(...)
    /// will be synchronous.
    ///
    /// @param[in] path
    ///     A full or partial name for the process to attach to.
    ///
    /// @param[in] wait_for
    ///     If \b false, attach to an existing process whose name matches.
    ///     If \b true, then wait for the next process whose name matches.
    //------------------------------------------------------------------
    int SBAttachInfo(const char* path, bool wait_for);

    /*! @brief Destroys the specified window and its context.
    *
    *  This function destroys the specified window and its context.  On calling
    *  this function, no further callbacks will be called for that window.
    *
    *  If the context of the specified window is current on the main thread, it is
    *  detached before being destroyed.
    *
    *  @param[in] window The window to destroy.
    *
    *  @note The context of the specified window must not be current on any other
    *  thread when this function is called.
    *
    *  @reentrancy This function must not be called from a callback.
    *
    *  @thread_safety This function must only be called from the main thread.
    *
    *  @since Added in version 3.0.  Replaces `glfwCloseWindow`.
    */
    void glfwDestroyWindow(int* window);

    /**
     * <sip:alice@example.net>
     */
    class LinphoneAddress {};
};

DLL_API void forceUseSpecializations(ForwardedInIndependentHeader value);
