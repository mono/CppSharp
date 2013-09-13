﻿using System;
using NUnit.Framework;
using Foo = UTF16.Foo;

[TestFixture]
public class UTF16Tests
{
    [Test]
    public void TestFoo()
    {
        var foo = new Foo();
        const string georgia = "საქართველო";
        foo.Unicode = georgia;
        Assert.That(foo.Unicode, Is.EqualTo(georgia));

        // TODO: move this, it has nothing to do with Unicode, it's here only not to break the CLI branch
        Assert.That(foo[0], Is.EqualTo(5));
    }
}
