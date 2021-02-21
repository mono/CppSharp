using System;
using CppSharp.AST;
using WebIdlCSharp;

namespace CppSharp.WebIDL
{
    public class WebIDLGenerator
    {
        public static Declaration ConvertWebIDLMemberNode(WebIdlMemberDefinition type)
        {
            switch (type.Type)
            {
                case "attribute":
                {
                    var prop = new Property();
                    return prop;
                }
                case "constructor":
                {
                    var ctor = new Method {Kind = CXXMethodKind.Constructor};
                    return ctor;
                }
                case "operation":
                {
                    var method = new Method {Kind = CXXMethodKind.Normal};
                    return method;
                }
                case "const":
                {
                    var @const = new Variable();
                    return @const;
                }
                case "iterable":
                {
                    var @const = new Variable();
                    return @const;
                }
                default:
                    throw new Exception($"Unknown member type definition: {type.Type}");
            }
        }

        public static Declaration ConvertWebIDLNode(WebIdlTypeDefinition type)
        {
            Declaration decl;

            switch (type.Type)
            {
                case "enum":
                {
                    decl = new Enumeration();
                    break;
                }
                case "interface":
                {
                    decl = new Class();
                    foreach (var member in type.Members)
                        ConvertWebIDLMemberNode(member);
                    break;
                }
                case "dictionary":
                {
                    decl = new Class();
                    break;
                }
                case "callback":
                {
                    decl = new Class();
                    break;
                }
                case "callback interface":
                {
                    decl = new Class();
                    break;
                }
                case "interface mixin":
                {
                    decl = new Class();
                    break;
                }
                case "includes":
                {
                    decl = new Class();
                    break;
                }
                default:
                    throw new Exception($"Unknown type definition: {type.Type}");
            }

            return decl;
        }

        public static void Main(string[] args)
        {
            var domPath = "/home/joao/dev/CppSharp/tests2/webidl/dom.json";
            var types = WebIdlParser.LoadTypesFromFile(domPath);

            var translationUnit = new TranslationUnit();

            foreach (var type in types)
            {
                var decl = ConvertWebIDLNode(type);
            }
        }
    }
}