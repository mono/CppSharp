using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.AST
{
    public class ASTRecord
    {
        public ASTRecord Parent;
        public object Object;
        public bool Visited;

        public bool GetParent<T>(out T @out)
        {
            @out = default(T);

            if (Parent == null)
                return false;

            var v = Parent.Object;
            if (!(v is T))
                return false;

            @out = (T) v;
            return true;
        }

        // Finds the first ancestor of type T.
        public ASTRecord FindAncestor<T>()
        {
            if (Parent == null)
                return null;

            if (Parent.Object is T)
                return Parent;

            return Parent.FindAncestor<T>();
        }

        // Pushes ancestors into the stack until it has found one of type T.
        public bool GetAncestors<T>(ref Stack<object> ancestors)
        {
            ancestors.Push(this);

            T value;
            if (GetParent(out value))
            {
                ancestors.Push(value);
                return true;
            }

            return Parent != null 
                && Parent.GetAncestors<T>(ref ancestors);
        }
    }

    public class ASTRecord<T> : ASTRecord
    {
        public T Value
        {
            get { return (T) Object; }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class ASTRecordStack
    {
        private readonly Stack<ASTRecord> recordStack;

        public ASTRecordStack()
        {
            recordStack = new Stack<ASTRecord>();
        }

        public ASTRecord<T> Push<T>(T value)
        {
            ASTRecord parent = null;

            if (recordStack.Count > 0)
                parent = recordStack.Peek();

            var record = new ASTRecord<T>()
            {
                Parent = parent,
                Object = value
            };

            recordStack.Push(record);
            return record;
        }

        public void Pop()
        {
            recordStack.Pop();
        }

        public bool Contains(object decl)
        {
            return recordStack.Any(rec => decl == rec.Object);
        }

        public bool Contains(Func<ASTRecord, bool> func)
        {
            return recordStack.Any(func);
        }

        public bool IsBeingVisited(Declaration decl)
        {
            var record = recordStack.FirstOrDefault(rec => decl == rec.Object);

            if (record != null)
            {
                var isBeingVisited = record.Visited;
                record.Visited = true;
                return isBeingVisited;
            }

            return false;
        }
    }

    static class ASTRecordExtensions
    {
        public static bool IsBaseClass(this ASTRecord record)
        {
            Class decl;
            if (!record.GetParent(out decl))
                return false;

            var recordDecl = record.Object as Class;
            return recordDecl != null && recordDecl == decl.BaseClass;
        }

        public static bool IsFieldValueType(this ASTRecord record)
        {
            var ancestors = new Stack<object>();
            if(!record.GetAncestors<Field>(ref ancestors))
                return false;

            var field = (Field)ancestors.Pop();
                
            Class decl;
            return field.Type.Desugar().TryGetClass(out decl) && decl.IsValueType;
        }

        public static bool IsDelegate(this ASTRecord record)
        {
            var typedef = record.Object as TypedefDecl;
            return typedef != null && typedef.Type.GetPointee() is FunctionType;
        }
    }

    public class RecordCollector : AstVisitor
    {
        public readonly ISet<ASTRecord<Declaration>> Declarations;
        private readonly ASTRecordStack recordStack;

        private readonly TranslationUnit translationUnit;

        public RecordCollector(TranslationUnit translationUnit)
        {
            this.translationUnit = translationUnit;
            Declarations = new HashSet<ASTRecord<Declaration>>();
            recordStack = new ASTRecordStack();
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (decl.IsIncomplete && decl.CompleteDeclaration != null)
                decl = decl.CompleteDeclaration;

            if (recordStack.Contains(decl))
                return ShouldVisitChilds(decl);

            Declarations.Add(recordStack.Push(decl));
            decl.Visit(this);
            recordStack.Pop();

            return false;
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            if (recordStack.Contains(type))
                return true;

            recordStack.Push(type);
            type.Visit(this);
            recordStack.Pop();

            return false;
        }

        public bool ShouldVisitChilds(Declaration decl)
        {
            if (decl == translationUnit)
                return true;

            if (decl is TranslationUnit)
                return false;

            if (recordStack.Contains(record => record.Object is Type))
                return false;

            if (recordStack.IsBeingVisited(decl))
                return false;

            if (decl.Namespace == null)
                return true;

            // No need to continue visiting after a declaration of another
            // translation unit is encountered.
            return decl.Namespace.TranslationUnit == translationUnit;
        }
     }
}
