using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Passes;
using Type = CppSharp.AST.Type;

namespace CppSharp
{
    /// <summary>
    /// This pass sets each event in the AST with a global unique ID.
    /// </summary>
    public class MarkEventsWithUniqueIdPass : TranslationUnitPass
    {
        private int eventId = 1;
        public Dictionary<Event, int> EventIds = new Dictionary<Event, int>();

        public override bool VisitEvent(Event @event)
        {
            if (AlreadyVisited(@event))
                return true;

            if (@event.GlobalId != 0)
                throw new NotSupportedException();

            @event.GlobalId = eventId++;
            return base.VisitEvent(@event);
        }
    }
}