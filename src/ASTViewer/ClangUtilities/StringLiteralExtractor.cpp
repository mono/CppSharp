#include "StringLiteralExtractor.h"

#pragma warning (push)
#pragma warning (disable:4100 4127 4800 4512 4245 4291 4510 4610 4324 4267 4244 4996 4146)
#include <clang/AST/Expr.h>
#include <clang/Lex/Lexer.h>
#include <clang/Lex/LiteralSupport.h>
#include <clang/Frontend/FrontendActions.h>
#include <clang/Basic/SourceLocation.h>
#include <clang/Basic/TargetInfo.h>
#include <clang/Basic/CharInfo.h>
#include <llvm/ADT/StringExtras.h>
#include <llvm/Support/ConvertUTF.h>
#pragma warning (pop)

using namespace llvm;
using namespace clang;

namespace clang_utilities {

// This function is an direct adaptation from ProcessCharEscape in llvm-3.7.1\src\tools\clang\lib\Lex\LiteralSupport.cpp (just removed the diag part)
unsigned ProcessCharEscape(const char *ThisTokBegin,
    const char *&ThisTokBuf,
    const char *ThisTokEnd, bool &HadError,
    FullSourceLoc Loc, unsigned CharWidth,
    const LangOptions &Features)
{
    // Skip the '\' char.
    ++ThisTokBuf;

    // We know that this character can't be off the end of the buffer, because
    // that would have been \", which would not have been the end of string.
    unsigned ResultChar = *ThisTokBuf++;
    switch (ResultChar) {
        // These map to themselves.
    case '\\': case '\'': case '"': case '?': break;

        // These have fixed mappings.
    case 'a':
        // TODO: K&R: the meaning of '\\a' is different in traditional C
        ResultChar = 7;
        break;
    case 'b':
        ResultChar = 8;
        break;
    case 'e':
        ResultChar = 27;
        break;
    case 'E':
        ResultChar = 27;
        break;
    case 'f':
        ResultChar = 12;
        break;
    case 'n':
        ResultChar = 10;
        break;
    case 'r':
        ResultChar = 13;
        break;
    case 't':
        ResultChar = 9;
        break;
    case 'v':
        ResultChar = 11;
        break;
    case 'x': { // Hex escape.
        ResultChar = 0;
        if (ThisTokBuf == ThisTokEnd || !isHexDigit(*ThisTokBuf)) {
            HadError = 1;
            break;
        }

        // Hex escapes are a maximal series of hex digits.
        bool Overflow = false;
        for (; ThisTokBuf != ThisTokEnd; ++ThisTokBuf) {
            int CharVal = llvm::hexDigitValue(ThisTokBuf[0]);
            if (CharVal == -1) break;
            // About to shift out a digit?
            if (ResultChar & 0xF0000000)
                Overflow = true;
            ResultChar <<= 4;
            ResultChar |= CharVal;
        }

        // See if any bits will be truncated when evaluated as a character.
        if (CharWidth != 32 && (ResultChar >> CharWidth) != 0) {
            Overflow = true;
            ResultChar &= ~0U >> (32 - CharWidth);
        }

        // Check for overflow.
        break;
    }
    case '0': case '1': case '2': case '3':
    case '4': case '5': case '6': case '7': {
        // Octal escapes.
        --ThisTokBuf;
        ResultChar = 0;

        // Octal escapes are a series of octal digits with maximum length 3.
        // "\0123" is a two digit sequence equal to "\012" "3".
        unsigned NumDigits = 0;
        do {
            ResultChar <<= 3;
            ResultChar |= *ThisTokBuf++ - '0';
            ++NumDigits;
        } while (ThisTokBuf != ThisTokEnd && NumDigits < 3 &&
            ThisTokBuf[0] >= '0' && ThisTokBuf[0] <= '7');

        // Check for overflow.  Reject '\777', but not L'\777'.
        if (CharWidth != 32 && (ResultChar >> CharWidth) != 0) {
            ResultChar &= ~0U >> (32 - CharWidth);
        }
        break;
    }

              // Otherwise, these are not valid escapes.
    case '(': case '{': case '[': case '%':
        // GCC accepts these as extensions.  We warn about them as such though.
        break;
    default:
        break;
    }
    return ResultChar;
}


// This function is an direct adaptation from ProcessUCNEscape in llvm-3.7.1\src\tools\clang\lib\Lex\LiteralSupport.cpp (just removed the diag part)
bool ProcessUCNEscape(const char *ThisTokBegin, const char *&ThisTokBuf,
    const char *ThisTokEnd,
    uint32_t &UcnVal, unsigned short &UcnLen,
    FullSourceLoc Loc,
    const LangOptions &Features,
    bool in_char_string_literal) {
    const char *UcnBegin = ThisTokBuf;

    // Skip the '\u' char's.
    ThisTokBuf += 2;

    if (ThisTokBuf == ThisTokEnd || !isHexDigit(*ThisTokBuf)) {
        return false;
    }
    UcnLen = (ThisTokBuf[-1] == 'u' ? 4 : 8);
    unsigned short UcnLenSave = UcnLen;
    for (; ThisTokBuf != ThisTokEnd && UcnLenSave; ++ThisTokBuf, UcnLenSave--) {
        int CharVal = llvm::hexDigitValue(ThisTokBuf[0]);
        if (CharVal == -1) break;
        UcnVal <<= 4;
        UcnVal |= CharVal;
    }
    // If we didn't consume the proper number of digits, there is a problem.
    if (UcnLenSave) {
        return false;
    }

    // Check UCN constraints (C99 6.4.3p2) [C++11 lex.charset p2]
    if ((0xD800 <= UcnVal && UcnVal <= 0xDFFF) || // surrogate codepoints
        UcnVal > 0x10FFFF) {                      // maximum legal UTF32 value
        return false;
    }

    // C++11 allows UCNs that refer to control characters and basic source
    // characters inside character and string literals
    if (UcnVal < 0xa0 &&
        (UcnVal != 0x24 && UcnVal != 0x40 && UcnVal != 0x60)) {  // $, @, `
        bool IsError = (!Features.CPlusPlus11 || !in_char_string_literal);
        if (IsError)
            return false;
    }
    return true;
}

// This function is an direct adaptation from ProcessUCNEscape in llvm-3.7.1\src\tools\clang\lib\Lex\LiteralSupport.cpp (just removed the diag part)
int MeasureUCNEscape(const char *ThisTokBegin, const char *&ThisTokBuf,
    const char *ThisTokEnd, unsigned CharByteWidth,
    const LangOptions &Features, bool &HadError) {
    // UTF-32: 4 bytes per escape.
    if (CharByteWidth == 4)
        return 4;

    uint32_t UcnVal = 0;
    unsigned short UcnLen = 0;
    FullSourceLoc Loc;

    if (!ProcessUCNEscape(ThisTokBegin, ThisTokBuf, ThisTokEnd, UcnVal,
        UcnLen, Loc, Features, true)) {
        HadError = true;
        return 0;
    }

    // UTF-16: 2 bytes for BMP, 4 bytes otherwise.
    if (CharByteWidth == 2)
        return UcnVal <= 0xFFFF ? 2 : 4;

    // UTF-8.
    if (UcnVal < 0x80)
        return 1;
    if (UcnVal < 0x800)
        return 2;
    if (UcnVal < 0x10000)
        return 3;
    return 4;
}



// This function is just the same as convertUTF16ToUTF8String, but adapted to UTF32, since it did not exist in clang
bool convertUTF32ToUTF8String(ArrayRef<char> SrcBytes, std::string &Out) {
    assert(Out.empty());

    // Error out on an uneven byte count.
    if (SrcBytes.size() % 4)
        return false;

    // Avoid OOB by returning early on empty input.
    if (SrcBytes.empty())
        return true;

    const UTF32 *Src = reinterpret_cast<const UTF32 *>(SrcBytes.begin());
    const UTF32 *SrcEnd = reinterpret_cast<const UTF32 *>(SrcBytes.end());

    // Byteswap if necessary.
    // Ignore any potential BOM: We won't have the here...

    // Just allocate enough space up front.  We'll shrink it later.  Allocate
    // enough that we can fit a null terminator without reallocating.
    Out.resize(SrcBytes.size() * UNI_MAX_UTF8_BYTES_PER_CODE_POINT + 1);
    UTF8 *Dst = reinterpret_cast<UTF8 *>(&Out[0]);
    UTF8 *DstEnd = Dst + Out.size();

    ConversionResult CR =
        ConvertUTF32toUTF8(&Src, SrcEnd, &Dst, DstEnd, strictConversion);
    assert(CR != targetExhausted);

    if (CR != conversionOK) {
        Out.clear();
        return false;
    }

    Out.resize(reinterpret_cast<char *>(Dst)-&Out[0]);
    Out.push_back(0);
    Out.pop_back();
    return true;
}


// This function is an adaptation from StringLiteral::getLocationOfByte in llvm-3.7.1\src\tools\clang\lib\AST\Expr.cpp
std::vector<std::string>
splitStringLiteral(clang::StringLiteral *S, const SourceManager &SM, const LangOptions &Features, const TargetInfo &Target)
{
    // Loop over all of the tokens in this string until we find the one that
    // contains the byte we're looking for.
    unsigned TokNo = 0;

    std::vector<std::string> result;
    for (TokNo = 0; TokNo < S->getNumConcatenated(); ++TokNo)
    {
        SourceLocation StrTokLoc = S->getStrTokenLoc(TokNo);

        // Get the spelling of the string so that we can get the data that makes up
        // the string literal, not the identifier for the macro it is potentially
        // expanded through.
        SourceLocation StrTokSpellingLoc = SM.getSpellingLoc(StrTokLoc);

        // Re-lex the token to get its length and original spelling.
        std::pair<FileID, unsigned> LocInfo = SM.getDecomposedLoc(StrTokSpellingLoc);
        bool Invalid = false;
        StringRef Buffer = SM.getBufferData(LocInfo.first, &Invalid);
        if (Invalid)
            continue; // We ignore this part

        const char *StrData = Buffer.data() + LocInfo.second;

        // Create a lexer starting at the beginning of this token.
        Lexer TheLexer(SM.getLocForStartOfFile(LocInfo.first), Features,
            Buffer.begin(), StrData, Buffer.end());
        Token TheTok;
        TheLexer.LexFromRawLexer(TheTok);
        if (TheTok.isAnyIdentifier())
        {
            // It should not be, since we are parsing inside a string literal, but it can happen with special macros such as __func__
            // of __PRETTY_FUNCTION__ that are not resolved at this time. In that case, we just ignore them...
            continue;
        }
        // Get the spelling of the token.
        SmallString<32> SpellingBuffer;
        SpellingBuffer.resize(TheTok.getLength());

        bool StringInvalid = false;
        const char *SpellingPtr = &SpellingBuffer[0];
        unsigned TokLen = Lexer::getSpelling(TheTok, SpellingPtr, SM, Features, &StringInvalid);
        if (StringInvalid)
            continue;

        const char *SpellingStart = SpellingPtr;
        const char *SpellingEnd = SpellingPtr + TokLen;
        result.push_back(std::string(SpellingStart, SpellingEnd));

    }
    return result;
}

} // namespace clang_utilities
