using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using StandardLib;

[TestFixture]
public class StandardLibTests
{
    [Test]
    public void TestVectors()
    {
        var vectors = new StandardLib.TestVectors();

        var sum = vectors.SumIntVector(new List<int> { 1, 2, 3 });
        Assert.AreEqual(sum, 6);

        var list = vectors.IntVector;
        Assert.True(list.SequenceEqual(new List<int> { 2, 3, 4 }));

        var ptrList = vectors.IntPtrVector;
        var intList = ptrList.Select(ptr => Marshal.ReadInt32(ptr));
        Assert.True(intList.SequenceEqual(new List<int> { 2, 3, 4 }));

        var wrapperList = new List<IntWrapper>();
        for (int i = 0; i < 3; i++)
            wrapperList.Add(new IntWrapper() { Value = i });
        vectors.IntWrapperVector = wrapperList;
        wrapperList = vectors.IntWrapperVector;
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(i, wrapperList[i].Value);

        for (int i = 0; i < 3; i++)
            wrapperList[i].Value += i;

        vectors.IntWrapperPtrVector = wrapperList;
        wrapperList = vectors.IntWrapperPtrVector;
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(i * 2, wrapperList[i].Value);

        var valueTypeWrapperList = new List<IntWrapperValueType>();
        for (int i = 0; i < 3; i++)
            valueTypeWrapperList.Add(new IntWrapperValueType() { Value = i });
        vectors.IntWrapperValueTypeVector = valueTypeWrapperList;
        valueTypeWrapperList = vectors.IntWrapperValueTypeVector;
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(i, valueTypeWrapperList[i].Value);
    }

    [Test]
    public void TestOStream()
    {
        const string testString = "hello wörld";
        var stringWriter = new StringWriter();
        OStreamTest.WriteToOStream(stringWriter, testString);
        Assert.AreEqual(testString, stringWriter.ToString());
    }
}
