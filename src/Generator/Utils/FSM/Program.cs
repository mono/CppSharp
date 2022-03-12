using System.Collections.Generic;

namespace CppSharp.Utils.FSM
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var Q = new List<string> { "q0", "q1" };
            var Sigma = new List<char> { '0', '1' };
            var Delta = new List<Transition>{
            new Transition("q0", '0', "q0"),
            new Transition("q0", '1', "q1"),
            new Transition("q1", '1', "q1"),
            new Transition("q1", '0', "q0")
         };
            var Q0 = new List<string> { "q0" };
            var F = new List<string> { "q0", "q1" };
            var DFSM = new DFSM(Q, Sigma, Delta, Q0, F);

            var minimizedDFSM = Minimize.MinimizeDFSM(DFSM);
        }
    }
}