using System.Collections.Generic;
using CppSharp.Utils;
using NUnit.Framework;
using CppSharp.Parser;

namespace CppSharp.Generator.Tests
{
    [TestFixture]
    public class ReadNativeSymbolsTest
    {
        [Test]
        public void TestReadSymbolsWindows()
        {
            var symbols = GetSymbols("libexpat-windows");
            Assert.AreEqual("XML_DefaultCurrent", symbols[0]);
            Assert.AreEqual("XML_ErrorString", symbols[1]);
            Assert.AreEqual("XML_ExpatVersion", symbols[2]);
            Assert.AreEqual("XML_ExpatVersionInfo", symbols[3]);
            Assert.AreEqual("XML_ExternalEntityParserCreate", symbols[4]);
            Assert.AreEqual("XML_FreeContentModel", symbols[5]);
            Assert.AreEqual("XML_GetBase", symbols[6]);
            Assert.AreEqual("XML_GetBuffer", symbols[7]);
            Assert.AreEqual("XML_GetCurrentByteCount", symbols[8]);
            Assert.AreEqual("XML_GetCurrentByteIndex", symbols[9]);
            Assert.AreEqual("XML_GetCurrentColumnNumber", symbols[10]);
            Assert.AreEqual("XML_GetCurrentLineNumber", symbols[11]);
            Assert.AreEqual("XML_GetErrorCode", symbols[12]);
            Assert.AreEqual("XML_GetFeatureList", symbols[13]);
            Assert.AreEqual("XML_GetIdAttributeIndex", symbols[14]);
            Assert.AreEqual("XML_GetInputContext", symbols[15]);
            Assert.AreEqual("XML_GetParsingStatus", symbols[16]);
            Assert.AreEqual("XML_GetSpecifiedAttributeCount", symbols[17]);
            Assert.AreEqual("XML_MemFree", symbols[18]);
            Assert.AreEqual("XML_MemMalloc", symbols[19]);
            Assert.AreEqual("XML_MemRealloc", symbols[20]);
            Assert.AreEqual("XML_Parse", symbols[21]);
            Assert.AreEqual("XML_ParseBuffer", symbols[22]);
            Assert.AreEqual("XML_ParserCreate", symbols[23]);
            Assert.AreEqual("XML_ParserCreateNS", symbols[24]);
            Assert.AreEqual("XML_ParserCreate_MM", symbols[25]);
            Assert.AreEqual("XML_ParserFree", symbols[26]);
            Assert.AreEqual("XML_ParserReset", symbols[27]);
            Assert.AreEqual("XML_ResumeParser", symbols[28]);
            Assert.AreEqual("XML_SetAttlistDeclHandler", symbols[29]);
            Assert.AreEqual("XML_SetBase", symbols[30]);
            Assert.AreEqual("XML_SetCdataSectionHandler", symbols[31]);
            Assert.AreEqual("XML_SetCharacterDataHandler", symbols[32]);
            Assert.AreEqual("XML_SetCommentHandler", symbols[33]);
            Assert.AreEqual("XML_SetDefaultHandler", symbols[34]);
            Assert.AreEqual("XML_SetDefaultHandlerExpand", symbols[35]);
            Assert.AreEqual("XML_SetDoctypeDeclHandler", symbols[36]);
            Assert.AreEqual("XML_SetElementDeclHandler", symbols[37]);
            Assert.AreEqual("XML_SetElementHandler", symbols[38]);
            Assert.AreEqual("XML_SetEncoding", symbols[39]);
            Assert.AreEqual("XML_SetEndCdataSectionHandler", symbols[40]);
            Assert.AreEqual("XML_SetEndDoctypeDeclHandler", symbols[41]);
            Assert.AreEqual("XML_SetEndElementHandler", symbols[42]);
            Assert.AreEqual("XML_SetEndNamespaceDeclHandler", symbols[43]);
            Assert.AreEqual("XML_SetEntityDeclHandler", symbols[44]);
            Assert.AreEqual("XML_SetExternalEntityRefHandler", symbols[45]);
            Assert.AreEqual("XML_SetExternalEntityRefHandlerArg", symbols[46]);
            Assert.AreEqual("XML_SetHashSalt", symbols[47]);
            Assert.AreEqual("XML_SetNamespaceDeclHandler", symbols[48]);
            Assert.AreEqual("XML_SetNotStandaloneHandler", symbols[49]);
            Assert.AreEqual("XML_SetNotationDeclHandler", symbols[50]);
            Assert.AreEqual("XML_SetParamEntityParsing", symbols[51]);
            Assert.AreEqual("XML_SetProcessingInstructionHandler", symbols[52]);
            Assert.AreEqual("XML_SetReturnNSTriplet", symbols[53]);
            Assert.AreEqual("XML_SetSkippedEntityHandler", symbols[54]);
            Assert.AreEqual("XML_SetStartCdataSectionHandler", symbols[55]);
            Assert.AreEqual("XML_SetStartDoctypeDeclHandler", symbols[56]);
            Assert.AreEqual("XML_SetStartElementHandler", symbols[57]);
            Assert.AreEqual("XML_SetStartNamespaceDeclHandler", symbols[58]);
            Assert.AreEqual("XML_SetUnknownEncodingHandler", symbols[59]);
            Assert.AreEqual("XML_SetUnparsedEntityDeclHandler", symbols[60]);
            Assert.AreEqual("XML_SetUserData", symbols[61]);
            Assert.AreEqual("XML_SetXmlDeclHandler", symbols[62]);
            Assert.AreEqual("XML_StopParser", symbols[63]);
            Assert.AreEqual("XML_UseForeignDTD", symbols[64]);
            Assert.AreEqual("XML_UseParserAsHandlerArg", symbols[65]);
            Assert.AreEqual("XmlGetUtf16InternalEncoding", symbols[66]);
            Assert.AreEqual("XmlGetUtf16InternalEncodingNS", symbols[67]);
            Assert.AreEqual("XmlGetUtf8InternalEncoding", symbols[68]);
            Assert.AreEqual("XmlGetUtf8InternalEncodingNS", symbols[69]);
            Assert.AreEqual("XmlInitEncoding", symbols[70]);
            Assert.AreEqual("XmlInitEncodingNS", symbols[71]);
            Assert.AreEqual("XmlInitUnknownEncoding", symbols[72]);
            Assert.AreEqual("XmlInitUnknownEncodingNS", symbols[73]);
            Assert.AreEqual("XmlParseXmlDecl", symbols[74]);
            Assert.AreEqual("XmlParseXmlDeclNS", symbols[75]);
            Assert.AreEqual("XmlPrologStateInit", symbols[76]);
            Assert.AreEqual("XmlPrologStateInitExternalEntity", symbols[77]);
            Assert.AreEqual("XmlSizeOfUnknownEncoding", symbols[78]);
            Assert.AreEqual("XmlUtf16Encode", symbols[79]);
            Assert.AreEqual("XmlUtf8Encode", symbols[80]);
            Assert.AreEqual("align_limit_to_full_utf8_characters", symbols[81]);
        }

        [Test]
        public void TestReadSymbolsLinux()
        {
            var symbols = GetSymbols("libexpat-linux");
            var expectedSymbols = new []
                {
                    ".init",
                    "free",
                    "_ITM_deregisterTMCloneTable",
                    "getpid",
                    "__stack_chk_fail",
                    "gettimeofday",
                    "__assert_fail",
                    "memset",
                    "memcmp",
                    "__gmon_start__",
                    "memcpy",
                    "malloc",
                    "realloc",
                    "memmove",
                    "_Jv_RegisterClasses",
                    "_ITM_registerTMCloneTable",
                    "__cxa_finalize",
                    "XmlInitUnknownEncoding",
                    "XML_FreeContentModel",
                    "XML_SetEndDoctypeDeclHandler",
                    "XML_GetParsingStatus",
                    "XmlGetUtf16InternalEncoding",
                    "XML_MemRealloc",
                    "XmlInitEncoding",
                    "XML_ExpatVersion",
                    "XML_SetHashSalt",
                    "XML_SetStartDoctypeDeclHandler",
                    "XML_ExternalEntityParserCreate",
                    "XML_GetBuffer",
                    "XML_GetCurrentColumnNumber",
                    "XML_SetEndCdataSectionHandler",
                    "XML_SetStartCdataSectionHandler",
                    "XML_GetCurrentByteCount",
                    "XML_DefaultCurrent",
                    "XmlInitUnknownEncodingNS",
                    "XML_ExpatVersionInfo",
                    "XmlUtf16Encode",
                    "XML_GetInputContext",
                    "XML_SetExternalEntityRefHandler",
                    "XML_GetSpecifiedAttributeCount",
                    "XML_SetUserData",
                    "XML_ErrorString",
                    "XML_SetElementHandler",
                    "XML_SetNamespaceDeclHandler",
                    "_fini",
                    "XmlSizeOfUnknownEncoding",
                    "XML_GetIdAttributeIndex",
                    "XML_SetAttlistDeclHandler",
                    "XML_SetReturnNSTriplet",
                    "XML_SetUnknownEncodingHandler",
                    "XML_SetCdataSectionHandler",
                    "XmlParseXmlDeclNS",
                    "XML_SetDoctypeDeclHandler",
                    "XML_SetDefaultHandler",
                    "_init",
                    "XmlPrologStateInitExternalEntity",
                    "XML_SetCharacterDataHandler",
                    "XML_ParserCreate",
                    "XmlGetUtf8InternalEncodingNS",
                    "XML_SetParamEntityParsing",
                    "XML_MemFree",
                    "XML_SetElementDeclHandler",
                    "XML_MemMalloc",
                    "XML_SetStartNamespaceDeclHandler",
                    "XmlGetUtf16InternalEncodingNS",
                    "XML_ParseBuffer",
                    "XML_UseForeignDTD",
                    "XML_SetEncoding",
                    "XML_UseParserAsHandlerArg",
                    "XML_SetEndNamespaceDeclHandler",
                    "XML_SetEndElementHandler",
                    "XML_GetCurrentLineNumber",
                    "XML_SetXmlDeclHandler",
                    "XML_SetProcessingInstructionHandler",
                    "XmlUtf8Encode",
                    "XML_SetStartElementHandler",
                    "XML_SetSkippedEntityHandler",
                    "XML_ResumeParser",
                    "XML_SetEntityDeclHandler",
                    "XML_ParserFree",
                    "XML_SetNotStandaloneHandler",
                    "XML_ParserCreate_MM",
                    "XML_ParserCreateNS",
                    "_edata",
                    "XML_SetUnparsedEntityDeclHandler",
                    "XML_SetBase",
                    "XML_GetBase",
                    "XmlGetUtf8InternalEncoding",
                    "XML_SetExternalEntityRefHandlerArg",
                    "XmlPrologStateInit",
                    "_end",
                    "XML_SetCommentHandler",
                    "XmlParseXmlDecl",
                    "XML_StopParser",
                    "XML_GetErrorCode",
                    "XML_GetFeatureList",
                    "XML_SetDefaultHandlerExpand",
                    "XML_Parse",
                    "XmlInitEncodingNS",
                    "XML_ParserReset",
                    "XML_SetNotationDeclHandler",
                    "__bss_start",
                    "XML_GetCurrentByteIndex"
                };

            for (int i = 0; i < symbols.Count; i++)
            {
                Assert.That(symbols[i], Is.EqualTo(expectedSymbols[i]));
            }
        }

        [Test]
        public void TestReadSymbolsOSX()
        {
            var symbols = GetSymbols("libexpat-osx");
            Assert.AreEqual("_XML_ParserCreate_MM", symbols[0]);
            Assert.AreEqual("_XML_ParserCreateNS", symbols[1]);
            Assert.AreEqual("_XML_ParserCreate", symbols[2]);
            Assert.AreEqual("_XML_ParserReset", symbols[3]);
            Assert.AreEqual("_XML_ParserFree", symbols[4]);
            Assert.AreEqual("_XML_ParseBuffer", symbols[5]);
            Assert.AreEqual("_XML_Parse", symbols[6]);
            Assert.AreEqual("_XML_SetEncoding", symbols[7]);
            Assert.AreEqual("_XML_SetEndElementHandler", symbols[8]);
            Assert.AreEqual("_XML_SetEndCdataSectionHandler", symbols[9]);
            Assert.AreEqual("_XML_SetEndDoctypeDeclHandler", symbols[10]);
            Assert.AreEqual("_XML_SetEndNamespaceDeclHandler", symbols[11]);
            Assert.AreEqual("_XML_SetEntityDeclHandler", symbols[12]);
            Assert.AreEqual("_XML_SetElementHandler", symbols[13]);
            Assert.AreEqual("_XML_SetElementDeclHandler", symbols[14]);
            Assert.AreEqual("_XML_SetExternalEntityRefHandlerArg", symbols[15]);
            Assert.AreEqual("_XML_SetExternalEntityRefHandler", symbols[16]);
            Assert.AreEqual("_XML_SetReturnNSTriplet", symbols[17]);
            Assert.AreEqual("_XML_SetUserData", symbols[18]);
            Assert.AreEqual("_XML_SetUnparsedEntityDeclHandler", symbols[19]);
            Assert.AreEqual("_XML_SetUnknownEncodingHandler", symbols[20]);
            Assert.AreEqual("_XML_SetBase", symbols[21]);
            Assert.AreEqual("_XML_SetStartElementHandler", symbols[22]);
            Assert.AreEqual("_XML_SetStartCdataSectionHandler", symbols[23]);
            Assert.AreEqual("_XML_SetStartDoctypeDeclHandler", symbols[24]);
            Assert.AreEqual("_XML_SetStartNamespaceDeclHandler", symbols[25]);
            Assert.AreEqual("_XML_SetSkippedEntityHandler", symbols[26]);
            Assert.AreEqual("_XML_SetCharacterDataHandler", symbols[27]);
            Assert.AreEqual("_XML_SetCommentHandler", symbols[28]);
            Assert.AreEqual("_XML_SetCdataSectionHandler", symbols[29]);
            Assert.AreEqual("_XML_SetProcessingInstructionHandler", symbols[30]);
            Assert.AreEqual("_XML_SetParamEntityParsing", symbols[31]);
            Assert.AreEqual("_XML_SetDefaultHandlerExpand", symbols[32]);
            Assert.AreEqual("_XML_SetDefaultHandler", symbols[33]);
            Assert.AreEqual("_XML_SetDoctypeDeclHandler", symbols[34]);
            Assert.AreEqual("_XML_SetNotationDeclHandler", symbols[35]);
            Assert.AreEqual("_XML_SetNotStandaloneHandler", symbols[36]);
            Assert.AreEqual("_XML_SetNamespaceDeclHandler", symbols[37]);
            Assert.AreEqual("_XML_SetAttlistDeclHandler", symbols[38]);
            Assert.AreEqual("_XML_SetXmlDeclHandler", symbols[39]);
            Assert.AreEqual("_XML_SetHashSalt", symbols[40]);
            Assert.AreEqual("_XML_StopParser", symbols[41]);
            Assert.AreEqual("_XML_ExternalEntityParserCreate", symbols[42]);
            Assert.AreEqual("_XML_ExpatVersionInfo", symbols[43]);
            Assert.AreEqual("_XML_ExpatVersion", symbols[44]);
            Assert.AreEqual("_XML_ErrorString", symbols[45]);
            Assert.AreEqual("_XML_UseParserAsHandlerArg", symbols[46]);
            Assert.AreEqual("_XML_UseForeignDTD", symbols[47]);
            Assert.AreEqual("_XML_GetBase", symbols[48]);
            Assert.AreEqual("_XML_GetBuffer", symbols[49]);
            Assert.AreEqual("_XML_GetSpecifiedAttributeCount", symbols[50]);
            Assert.AreEqual("_XML_GetIdAttributeIndex", symbols[51]);
            Assert.AreEqual("_XML_GetInputContext", symbols[52]);
            Assert.AreEqual("_XML_GetParsingStatus", symbols[53]);
            Assert.AreEqual("_XML_GetErrorCode", symbols[54]);
            Assert.AreEqual("_XML_GetCurrentByteIndex", symbols[55]);
            Assert.AreEqual("_XML_GetCurrentByteCount", symbols[56]);
            Assert.AreEqual("_XML_GetCurrentLineNumber", symbols[57]);
            Assert.AreEqual("_XML_GetCurrentColumnNumber", symbols[58]);
            Assert.AreEqual("_XML_GetFeatureList", symbols[59]);
            Assert.AreEqual("_XML_ResumeParser", symbols[60]);
            Assert.AreEqual("_XML_FreeContentModel", symbols[61]);
            Assert.AreEqual("_XML_MemMalloc", symbols[62]);
            Assert.AreEqual("_XML_MemRealloc", symbols[63]);
            Assert.AreEqual("_XML_MemFree", symbols[64]);
            Assert.AreEqual("_XML_DefaultCurrent", symbols[65]);
            Assert.AreEqual("_XmlUtf8Encode", symbols[66]);
            Assert.AreEqual("_XmlUtf16Encode", symbols[67]);
            Assert.AreEqual("_XmlSizeOfUnknownEncoding", symbols[68]);
            Assert.AreEqual("_XmlInitUnknownEncodingNS", symbols[69]);
            Assert.AreEqual("_XmlInitUnknownEncoding", symbols[70]);
            Assert.AreEqual("_XmlInitEncodingNS", symbols[71]);
            Assert.AreEqual("_XmlInitEncoding", symbols[72]);
            Assert.AreEqual("_XmlGetUtf8InternalEncodingNS", symbols[73]);
            Assert.AreEqual("_XmlGetUtf8InternalEncoding", symbols[74]);
            Assert.AreEqual("_XmlGetUtf16InternalEncodingNS", symbols[75]);
            Assert.AreEqual("_XmlGetUtf16InternalEncoding", symbols[76]);
            Assert.AreEqual("_XmlParseXmlDeclNS", symbols[77]);
            Assert.AreEqual("_XmlParseXmlDecl", symbols[78]);
            Assert.AreEqual("_XmlPrologStateInitExternalEntity", symbols[79]);
            Assert.AreEqual("_XmlPrologStateInit", symbols[80]);
        }

        private static IList<string> GetSymbols(string library)
        {
            var parserOptions = new ParserOptions();
            parserOptions.AddLibraryDirs(GeneratorTest.GetTestsDirectory("Native"));
            var driverOptions = new DriverOptions();
            var module = driverOptions.AddModule("Test");
            module.Libraries.Add(library);
            var driver = new Driver(driverOptions)
            {
                ParserOptions = parserOptions
            };
            driver.Setup();
            Assert.IsTrue(driver.ParseLibraries());
            var symbols = driver.Context.Symbols.Libraries[0].Symbols;
            return symbols;
        }
    }
}
