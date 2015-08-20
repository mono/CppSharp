/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include <llvm/Object/ELF.h>

namespace CppSharp { namespace CppParser {

template<typename ELFT>
class ELFDumper {
public:
    ELFDumper(const llvm::object::ELFFile<ELFT> *Obj);

    std::vector<llvm::StringRef> getNeededLibraries() const;

private:
    typedef llvm::object::ELFFile<ELFT> ELFO;
    typedef typename ELFO::Elf_Sym Elf_Sym;
    typedef typename ELFO::Elf_Dyn Elf_Dyn;
    typedef typename ELFO::Elf_Dyn_Range Elf_Dyn_Range;
    typedef typename ELFO::Elf_Phdr Elf_Phdr;
    typedef typename ELFO::uintX_t uintX_t;

    /// \brief Represents a region described by entries in the .dynamic table.
    struct DynRegionInfo {
        DynRegionInfo() : Addr(nullptr), Size(0), EntSize(0) {}
        /// \brief Address in current address space.
        const void *Addr;
        /// \brief Size in bytes of the region.
        uintX_t Size;
        /// \brief Size of each entity in the region.
        uintX_t EntSize;
    };

    llvm::StringRef getDynamicString(uint64_t Offset) const;
    const Elf_Dyn *dynamic_table_begin() const;
    const Elf_Dyn *dynamic_table_end() const;
    Elf_Dyn_Range dynamic_table() const {
        return llvm::make_range(dynamic_table_begin(), dynamic_table_end());
    }

    const ELFO *Obj;
    DynRegionInfo DynamicRegion;
    llvm::StringRef DynamicStringTable;
};

template <typename ELFT>
ELFDumper<ELFT>::ELFDumper(const llvm::object::ELFFile<ELFT> *Obj) {

    llvm::SmallVector<const Elf_Phdr *, 4> LoadSegments;
    for (const Elf_Phdr &Phdr : Obj->program_headers()) {
        if (Phdr.p_type == llvm::ELF::PT_DYNAMIC) {
            DynamicRegion.Addr = Obj->base() + Phdr.p_offset;
            uint64_t Size = Phdr.p_filesz;
            if (Size % sizeof(Elf_Dyn))
                llvm::report_fatal_error("Invalid dynamic table size");
            DynamicRegion.Size = Phdr.p_filesz;
            continue;
        }
        if (Phdr.p_type != llvm::ELF::PT_LOAD || Phdr.p_filesz == 0)
            continue;
        LoadSegments.push_back(&Phdr);
    }

    auto toMappedAddr = [&](uint64_t VAddr) -> const uint8_t *{
        const Elf_Phdr **I = std::upper_bound(
        LoadSegments.begin(), LoadSegments.end(), VAddr, llvm::object::compareAddr<ELFT>);
        if (I == LoadSegments.begin())
            llvm::report_fatal_error("Virtual address is not in any segment");
        --I;
        const Elf_Phdr &Phdr = **I;
        uint64_t Delta = VAddr - Phdr.p_vaddr;
        if (Delta >= Phdr.p_filesz)
            llvm::report_fatal_error("Virtual address is not in any segment");
        return Obj->base() + Phdr.p_offset + Delta;
    };

    const char *StringTableBegin = nullptr;
    uint64_t StringTableSize = 0;
    for (const Elf_Dyn &Dyn : dynamic_table()) {
        switch (Dyn.d_tag) {
        case llvm::ELF::DT_STRTAB:
            StringTableBegin = (const char *)toMappedAddr(Dyn.getPtr());
            break;
        case llvm::ELF::DT_STRSZ:
            StringTableSize = Dyn.getVal();
            break;
        }
    }
    if (StringTableBegin)
        DynamicStringTable = StringRef(StringTableBegin, StringTableSize);
}

template <typename ELFT>
const typename ELFDumper<ELFT>::Elf_Dyn *
ELFDumper<ELFT>::dynamic_table_begin() const {
    return reinterpret_cast<const Elf_Dyn *>(DynamicRegion.Addr);
}

template <typename ELFT>
const typename ELFDumper<ELFT>::Elf_Dyn *
ELFDumper<ELFT>::dynamic_table_end() const {
    uint64_t Size = DynamicRegion.Size;
    return dynamic_table_begin() + Size / sizeof(Elf_Dyn);
}

template <class ELFT>
llvm::StringRef ELFDumper<ELFT>::getDynamicString(uint64_t Value) const {
    return llvm::StringRef(DynamicStringTable.data() + Value);
}

template<class ELFT>
std::vector<llvm::StringRef> ELFDumper<ELFT>::getNeededLibraries() const {
    std::vector<llvm::StringRef> Libs;

    for (const auto &Entry : dynamic_table())
        if (Entry.d_tag == llvm::ELF::DT_NEEDED)
            Libs.push_back(getDynamicString(Entry.d_un.d_val));

    return Libs;
}

} }
