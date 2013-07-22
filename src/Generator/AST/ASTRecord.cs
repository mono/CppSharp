using System;
using System.Collections.Generic;
using CppSharp.AST;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.AST
{
    public class ASTRecord
    {
        public ASTRecord Parent;
        public object Object;

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

        // Pushes ancestors into the stack until it is found one of type T.
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
    }

    static class ASTRecordExtensions
    {
        public static bool IsBaseClass(this ASTRecord record)
        {
            Class decl;
            return record.GetParent(out decl) && decl.BaseClass == record.Object;
        }

        public static bool IsFieldValueType(this ASTRecord record)
        {
            var ancestors = new Stack<object>();
            if(!record.GetAncestors<Field>(ref ancestors))
                return false;

            var field = (Field)ancestors.Pop();
                
            Class decl;
            if (!field.Type.Desugar().IsTagDecl(out decl))
                return false;

            return decl.IsValueType;
        }
    }

    public class RecordCollector : AstVisitor
    {
        public readonly ISet<ASTRecord<Declaration>> Declarations;
        private readonly ASTRecordStack recordStack;

        public TranslationUnit translationUnit;

        public ISet<object> Visited2 { get; private set; }
        public bool AlreadyVisited2(object o)
        {
            return !Visited2.Add(o);
        }

        public RecordCollector(TranslationUnit translationUnit)
        {
            this.translationUnit = translationUnit;
            Declarations = new HashSet<ASTRecord<Declaration>>();
            recordStack = new ASTRecordStack();
            Visited2 = new HashSet<object>();
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if(translationUnit.FileName.Contains("Font"))
                Console.Write("");

            if (decl.IsIncomplete && decl.CompleteDeclaration != null)
                decl = decl.CompleteDeclaration;

            if(AlreadyVisited2(decl)) 
                return ShouldVisitChilds(decl);
            Visited.Remove(decl); // So Class can be revisited

            Declarations.Add(recordStack.Push(decl));
            decl.Visit(this);
            recordStack.Pop();

            return false;
        }

        public override bool VisitType(Type type, TypeQualifiers quals)
        {
            type = type.Desugar();

            if(AlreadyVisited2(type)) 
                return true;

            recordStack.Push(type);
            type.Visit(this);
            recordStack.Pop();

            return false;
        }

        public bool ShouldVisitChilds(Declaration decl)
        {
            if(decl == translationUnit)
                return true;

            if (decl is TranslationUnit)
                return false;

            if (decl.Namespace == null)
                return true;

            // No need to continue visiting after a declaration of 
            // another translation unit is encountered.
            return decl.Namespace.TranslationUnit == translationUnit;
        }
     }
}
