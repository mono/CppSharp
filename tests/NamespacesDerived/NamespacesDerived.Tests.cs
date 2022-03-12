using System.IO;
using System.Xml.Linq;
using System.Linq;
using System;
using System.Reflection;
using NUnit.Framework;
using NamespacesDerived;

[TestFixture]
public class NamespaceDerivedTests
{
    [Test]
    public void TestCodeGeneration()
    {
        using (new DerivedFromSecondaryBaseInDependency()) { }
        using (var der2 = new Derived2())
        using (der2.LocalTypedefSpecialization) { }
    }

    [Test]
    public void TestNonRenamedMethod()
    {
        using (var derived = new Derived())
        {
            var parent = derived.Parent;
            derived.parent(0);
        }
        using (var derived2 = new Derived2())
        {
            var template = derived2.Template;
            template.Field = 5;
            Assert.That(template.Field, Is.EqualTo(5));
        }
    }

    [Test]
    public void TestOverrideMethodFromDependency()
    {
        using (var overrideMethodFromDependency = new OverrideMethodFromDependency())
        {
            using (var managedObject = new OverrideMethodFromDependency())
            {
                overrideMethodFromDependency.ManagedObject = managedObject;
                Assert.That(overrideMethodFromDependency.CallManagedOverride(), Is.EqualTo(2));
            }
        }
    }

    [Test]
    public void TestComments()
    {
        Type testCommentsType = typeof(TestComments);
        Assembly assembly = testCommentsType.Assembly;
        string dir = Path.GetDirectoryName(assembly.Location);
        string xml = Path.ChangeExtension(Path.GetFileName(assembly.Location), ".xml");
        XDocument xmlDoc = XDocument.Load(Path.Combine(dir, xml));
        XElement members = xmlDoc.Root.Element("members");

        TestClassComment(testCommentsType, members);
        TestGetIOHandlerControlSequence(testCommentsType, members);
        TestSBAttachInfo(testCommentsType, members);
        TestGlfwDestroyWindow(testCommentsType, members);
    }

    private static void TestClassComment(Type testCommentsType, XElement members)
    {
        string testCommentsName = $"T:{testCommentsType.FullName}";
        XElement testComments = members.Elements().Single(
            m => m.Attribute("name").Value == testCommentsName);
        Assert.That(testComments.Element("summary").Elements().Select(p => p.Value), Is.EquivalentTo(
            new[]
            {
                "Hash set/map base class.",
                "Note that to prevent extra memory use due to vtable pointer, %HashBase intentionally does not declare a virtual destructor",
                "and therefore %HashBase pointers should never be used."
            }));
    }

    private static void TestGetIOHandlerControlSequence(Type testCommentsType, XElement members)
    {
        const string name = "GetIOHandlerControlSequence";
        XElement method = FindMethod(testCommentsType, members, name);
        Assert.That(method.Element("summary").Elements("para").Select(p => p.Value),
            Is.EquivalentTo(
                new[]
                {
                    "Get the string that needs to be written to the debugger stdin file",
                    "handle when a control character is typed."
                }));
        Assert.That(method.Element("returns").Elements("para").Select(p => p.Value),
            Is.EquivalentTo(
                new[]
                {
                    "The string that should be written into the file handle that is",
                    "feeding the input stream for the debugger, or NULL if there is",
                    "no string for this control key."
                }));
        Assert.That(method.Element("remarks").Elements("para").Select(p => p.Value),
            Is.EquivalentTo(
                new[]
                {
                    "Some GUI programs will intercept \"control + char\" sequences and want",
                    "to have them do what normally would happen when using a real",
                    "terminal, so this function allows GUI programs to emulate this",
                    "functionality."
                }));
        XElement ch = method.Element("param");
        Assert.That(ch.Attribute("name").Value,
            Is.EqualTo(testCommentsType.GetMethod(name).GetParameters()[0].Name));
        Assert.That(ch.Value, Is.EqualTo("The character that was typed along with the control key"));
    }

    private static void TestSBAttachInfo(Type testCommentsType, XElement members)
    {
        const string name = "SBAttachInfo";
        XElement method = FindMethod(testCommentsType, members, name);
        Assert.That(method.Element("remarks").Elements("para").Select(p => p.Value),
            Is.EquivalentTo(
                new[]
                {
                    "This function implies that a future call to SBTarget::Attach(...)",
                    "will be synchronous."
                }));

        ParameterInfo[] @params = testCommentsType.GetMethod(name).GetParameters();

        XElement path = method.Element("param");
        Assert.That(path.Attribute("name").Value,
            Is.EqualTo(@params[0].Name));
        Assert.That(path.Value, Is.EqualTo("A full or partial name for the process to attach to."));

        XElement wait_for = (XElement)path.NextNode;
        Assert.That(wait_for.Attribute("name").Value, Is.EqualTo(@params[1].Name));
        Assert.That(wait_for.Elements("para").Select(p => string.Concat(p.Nodes())),
            Is.EquivalentTo(
                new[]
                {
                    "If <c>false,</c> attach to an existing process whose name matches.",
                    "If <c>true,</c> then wait for the next process whose name matches."
                }));
    }

    private static void TestGlfwDestroyWindow(Type testCommentsType, XElement members)
    {
        const string name = "GlfwDestroyWindow";
        XElement method = FindMethod(testCommentsType, members, name);
        Assert.That(method.Element("summary").Value,
            Is.EqualTo("Destroys the specified window and its context."));
        Assert.That(method.Element("remarks").Elements("para").Select(p => p.Value),
            Is.EquivalentTo(
                new[]
                {
                    "This function destroys the specified window and its context.  On calling",
                    "this function, no further callbacks will be called for that window.",
                    "If the context of the specified window is current on the main thread, it is",
                    "detached before being destroyed.",
                    "The context of the specified window must not be current on any other",
                    "thread when this function is called.",
                    "This function must not be called from a callback.",
                    "_safety This function must only be called from the main thread.",
                    "Added in version 3.0.  Replaces `glfwCloseWindow`."
                }));
        XElement window = method.Element("param");
        Assert.That(window.Attribute("name").Value,
            Is.EqualTo(testCommentsType.GetMethod(name).GetParameters()[0].Name));
        Assert.That(window.Value, Is.EqualTo("The window to destroy."));
    }

    private static XElement FindMethod(Type testCommentsType, XElement members, string name)
    {
        string fullName = $"M:{testCommentsType.FullName}.{name}";
        return members.Elements().Single(
            m =>
            {
                string name = m.Attribute("name").Value;
                return name.Substring(0, Math.Max(name.IndexOf('('), 0)) == fullName;
            });
    }

    private class OverrideMethodFromDependency : HasVirtualInDependency
    {
        public override int VirtualInCore(int parameter) => 2;
    }
}