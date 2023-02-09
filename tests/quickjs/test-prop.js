class A
{
    get foo() { return 1; }
}

class B extends A
{

}

let a = new A();
console.log(a.foo)

let b = new B();
console.log(b.foo)
