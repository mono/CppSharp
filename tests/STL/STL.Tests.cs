using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class STLTests
{
    [Test]
    public void TestVectors()
    {
        var vectors = new STL.TestVectors();

        var sum = vectors.SumIntVector(new List<int> { 1, 2, 3 });
        Assert.AreEqual(sum, 6);

        var list = vectors.GetIntVector();
        Assert.True(list.SequenceEqual(new List<int> { 2, 3, 4 }));
    }
}
 