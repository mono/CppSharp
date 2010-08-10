//
// Mono.VisualC.Interop.Util.IEnumerableTransform.cs: Rule-based transformation for IEnumerable
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Mono.VisualC.Interop.Util {


	public static class IEnumerableTransform {

		// Transforms an IEnumerable into another by specific rules.

		public static IEnumerable<TOut> Transform<TIn, TOut> (this IEnumerable<TIn> input, params EmitterFunc<TIn, TOut> [] rules)
		{
			CloneableEnumerator<TIn> enumerator = new CloneableEnumerator<TIn> (input, 0);

			while (enumerator.MoveNext ()) {
				InputData<TIn> inputData = new InputData<TIn> (input, enumerator);

				foreach (var rule in rules) {
					TOut output;
					if (rule (inputData.NewContext (), out output) == RuleResult.MatchEmit)
						yield return output;

				}
			}

		}

		public static IRule<TIn> And<TIn> (this IRule<TIn> previousRules, IRule<TIn> parentheticalRules)
		{
			return new AndRule<TIn> (previousRules, parentheticalRules);
		}
		public static RuleCompound<TIn> And<TIn> (this IRule<TIn> previousRules)
		{
			return new RuleCompound<TIn> ((subsequentRules) => {
				return And<TIn> (previousRules, subsequentRules);
			});
		}

		public static IRule<TIn> Or<TIn> (this IRule<TIn> previousRules, IRule<TIn> parentheticalRules)
		{
			return new OrRule<TIn> (previousRules, parentheticalRules);
		}
		public static RuleCompound<TIn> Or<TIn> (this IRule<TIn> previousRules)
		{
			return new RuleCompound<TIn> ((subsequentRules) => {
				return Or<TIn> (previousRules, subsequentRules);
			});
		}

		public static IRule<TIn> After<TIn> (this IRule<TIn> previousRules, IRule<TIn> parentheticalRules)
		{
			return new AndRule<TIn> (new AfterRule<TIn> (parentheticalRules), previousRules);
		}
		public static RuleCompound<TIn> After<TIn> (this IRule<TIn> previousRules)
		{
			return new RuleCompound<TIn> ((subsequentRules) => {
				return After<TIn> (previousRules, subsequentRules);
			});
		}

		public static IRule<TIn> AtEnd<TIn> (this IRule<TIn> previousRules)
		{
			return new AtEndRule<TIn> (previousRules);
		}

		public static EmitterFunc<TIn, TOut> Emit<TIn, TOut> (this IRule<TIn> rule, Func<TIn, TOut> result)
		{
			return delegate (InputData<TIn> input, out TOut output) {
				output = default (TOut);
				TIn value = input.Value;
				RuleResult ruleResult = rule.SatisfiedBy (input);

				if (ruleResult != RuleResult.NoMatch) {
					input.MatchedRules.Add (rule);
					output = result (value);
					return ruleResult;
				}
				return RuleResult.NoMatch;
			};
		}
		public static EmitterFunc<TIn, TOut> Emit<TIn, TOut> (this IRule<TIn> rule, TOut result)
		{
			return Emit (rule, (input) => result);
		}

		// helpers:

		public static IEnumerable<T> With<T> (this IEnumerable<T> current, T additionalValue)
		{
			foreach (var output in current)
				yield return output;
			yield return additionalValue;
		}


		public static IEnumerable<T> Without<T> (this IEnumerable<T> current, T unwantedValue)
		{
			foreach (var output in current)
				if (!output.Equals (unwantedValue))
					yield return output;
		}

		public static int SequenceHashCode<T> (this IEnumerable<T> sequence)
		{
			int hash = 0;
			foreach (var item in sequence)
				hash ^= item.GetHashCode ();

			return hash;
		}

		// FIXME: Faster way to do this?
		public static void AddFirst<T> (this List<T> list, IEnumerable<T> items)
		{
			T [] temp = new T [list.Count];
			list.CopyTo (temp, 0);
			list.Clear ();
			list.AddRange (items);
			list.AddRange (temp);
	        }


	}

	public enum RuleResult {
		NoMatch,
		MatchEmit,
		MatchNoEmit
	}

	public delegate RuleResult EmitterFunc<TIn, TOut> (InputData<TIn> input, out TOut output);

	public struct InputData<TIn> {
		public IEnumerable<TIn> AllValues;
		public CloneableEnumerator<TIn> Enumerator;
		public List<IRule<TIn>> MatchedRules;
		public InputData (IEnumerable<TIn> input,  CloneableEnumerator<TIn> enumerator)
		{
			this.AllValues = input;
			this.Enumerator = enumerator;
			this.MatchedRules = new List<IRule<TIn>> ();
		}

		public InputData<TIn> NewContext ()
		{
			InputData<TIn> nctx = new InputData<TIn> ();
			nctx.AllValues = this.AllValues;
			nctx.Enumerator = this.Enumerator.Clone ();
			nctx.MatchedRules = this.MatchedRules;
			return nctx;
		}

		public TIn Value {
			get { return Enumerator.Current; }
		}
	}

	#region Rules
	public interface IRule<TIn> {
		RuleResult SatisfiedBy (InputData<TIn> input);
	}

	// yields all inputs indescriminately
	public class AnyRule<TIn> : IRule<TIn> {
		public AnyRule ()
		{
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			return RuleResult.MatchEmit;
		}
	}

	// yields all inputs that haven't satisfied
	//  any other rule
	public class UnmatchedRule<TIn> : IRule<TIn> {
		public UnmatchedRule ()
		{
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			return !input.MatchedRules.Any ()? RuleResult.MatchEmit : RuleResult.NoMatch;
		}
	}

	// yields input if it hasn't been satisfied before
	public class FirstRule<TIn> : IRule<TIn> {
		protected bool triggered = false;
		public FirstRule ()
		{
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			if (!triggered) {
				triggered = true;
				return RuleResult.MatchEmit;
			}
			return RuleResult.NoMatch;
		}
	}

	// yields input if it is the last that would satisfy
	// all its matched rules
	public class LastRule<TIn> : IRule<TIn> {
		public LastRule ()
		{
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			throw new NotImplementedException ();
		}
	}

	// yields input if the specified previous rule has already been satisfied
	public class AfterRule<TIn> : IRule<TIn> {
		protected IRule<TIn> previousRule;
		protected bool satisfied = false;
		public AfterRule (IRule<TIn> previousRule)
		{
			this.previousRule = previousRule;
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			if (satisfied)
				return RuleResult.MatchEmit;

			satisfied = previousRule.SatisfiedBy (input) != RuleResult.NoMatch;

			return RuleResult.NoMatch;
		}
	}

	// yields all inputs found in the specified set
	public class InSetRule<TIn> : IRule<TIn> {
		protected IEnumerable<TIn> conditions;

		public InSetRule (params TIn [] conditions)
		{
			this.conditions = conditions;
		}
		public InSetRule (IEnumerable<TIn> conditions)
		{
			this.conditions = conditions;
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			return conditions.Contains (input.Value)? RuleResult.MatchEmit : RuleResult.NoMatch;
		}
	}

	public class PredicateRule<TIn> : IRule<TIn> {
		protected Func<TIn, bool> predicate;

		public PredicateRule (Func<TIn, bool> predicate)
		{
			this.predicate = predicate;
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			return predicate (input.Value)? RuleResult.MatchEmit : RuleResult.NoMatch;
		}
	}

	// is satisfied by any of the specified items as long the
	//  item appears adjacent with all the other items
	public class AnyOrderRule<TIn> : IRule<TIn> {
		protected IEnumerable<TIn> conditions;

		protected Queue<TIn> verifiedItems = new Queue<TIn> ();
		protected bool verified = false;


		public AnyOrderRule (params TIn [] conditions)
		{
			this.conditions = conditions;
		}
		public AnyOrderRule (IEnumerable<TIn> conditions)
		{
			this.conditions = conditions;
		}
		public RuleResult SatisfiedBy (InputData<TIn> inputData)
		{
			if (verified && verifiedItems.Count > 0) {
				verifiedItems.Dequeue ();
				return RuleResult.MatchNoEmit;
			} else
				verified = false;

			IEnumerator<TIn> input = inputData.Enumerator;

			while (conditions.Contains (input.Current) && !verifiedItems.Contains (input.Current)) {
				verifiedItems.Enqueue (input.Current);
				if (!input.MoveNext ()) break;
			}

			if (verifiedItems.Count == conditions.Count ()) {
				verified = true;
				verifiedItems.Dequeue ();
				return RuleResult.MatchEmit;
			} else
				verifiedItems.Clear ();

			return RuleResult.NoMatch;
		}
	}

	public class InOrderRule<TIn> : IRule<TIn> {
		protected IEnumerable<TIn> conditions;

		protected bool verified = false;
		protected int verifiedCount = 0;

		public InOrderRule (params TIn [] conditions)
		{
			this.conditions = conditions;
		}
		public InOrderRule (IEnumerable<TIn> conditions)
		{
			this.conditions = conditions;
		}
		public RuleResult SatisfiedBy (InputData<TIn> inputData)
		{
			if (verified && verifiedCount > 0) {
				verifiedCount--;
				return RuleResult.MatchNoEmit;
			} else
				verified = false;

			IEnumerator<TIn> condition = conditions.GetEnumerator ();
			IEnumerator<TIn> input = inputData.Enumerator;

			while (condition.MoveNext () && condition.Equals (input.Current)) {
				verifiedCount++;
				if (!input.MoveNext ()) break;
			}
			if (verifiedCount == conditions.Count ()) {
				verified = true;
				verifiedCount--;
				return RuleResult.MatchEmit;
			} else
				verifiedCount = 0;

			return RuleResult.NoMatch;
		}
	}

	// yields all inputs that match all specified rules
	public class AndRule<TIn> : IRule<TIn> {
		protected IEnumerable<IRule<TIn>> rules;

		public AndRule (IEnumerable<IRule<TIn>> rules)
		{
			this.rules = rules;
		}
		public AndRule (params IRule<TIn>[] rules)
		{
			this.rules = rules;
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			RuleResult finalResult = RuleResult.NoMatch;
			foreach (var rule in rules) {
				RuleResult result = rule.SatisfiedBy (input.NewContext ());
				if (result == RuleResult.NoMatch)
					return RuleResult.NoMatch;
				if (finalResult != RuleResult.MatchNoEmit)
					finalResult = result;

				input.MatchedRules.Add (rule);
			}
			return finalResult;
		}
	}

	// yields all inputs that match any specified rules
	public class OrRule<TIn> : IRule<TIn> {
		protected IEnumerable<IRule<TIn>> rules;

		public OrRule (IEnumerable<IRule<TIn>> rules)
		{
			this.rules = rules;
		}
		public OrRule (params IRule<TIn>[] rules)
		{
			this.rules = rules;
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			foreach (var rule in rules) {
				RuleResult result = rule.SatisfiedBy (input.NewContext ());
				if (result != RuleResult.NoMatch) {
					input.MatchedRules.Add (rule);
					return result;
				}
			}

			return RuleResult.NoMatch;
		}
	}

	public class AtEndRule<TIn> : IRule<TIn> {
		protected IRule<TIn> rule;

		public AtEndRule (IRule<TIn> rule)
		{
			this.rule = rule;
		}
		public RuleResult SatisfiedBy (InputData<TIn> input)
		{
			RuleResult rr = rule.SatisfiedBy (input);
			if (!input.Enumerator.MoveNext ())
				return rr;
			return RuleResult.NoMatch;
		}
	}
	#endregion

	// the base point for building up rules
	//  All rules start For...
	public static class For {

		public static IRule<TIn> AnyInputIn<TIn> (params TIn [] input)
		{
			return new InSetRule<TIn> (input);
		}
		public static IRule<TIn> AnyInputIn<TIn> (IEnumerable<TIn> input)
		{
			return new InSetRule<TIn> (input);
		}

		public static SequenceQualifier<TIn> AllInputsIn<TIn> (params TIn [] input)
		{
			return new SequenceQualifier<TIn> (input, null);
		}
		public static SequenceQualifier<TIn> AllInputsIn<TIn> (IEnumerable<TIn> input)
		{
			return new SequenceQualifier<TIn> (input, null);
		}

		public static RuleCompound<TIn> First<TIn> ()
		{
			return new RuleCompound<TIn> ((subsequentRules) => {
				return new AndRule<TIn> (subsequentRules, new FirstRule<TIn> ());
			});
		}

		public static RuleCompound<TIn> Last<TIn> ()
		{
			return new RuleCompound<TIn> ((subsequentRules) => {
				return new AndRule<TIn> (subsequentRules, new LastRule<TIn> ());
			});
		}

		public static IRule<TIn> InputsWhere<TIn> (Func<TIn, bool> predicate)
		{
			return new PredicateRule<TIn> (predicate);
		}

		public static IRule<TIn> AnyInput<TIn> ()
		{
			return new AnyRule<TIn> ();
		}
		public static IRule<TIn> UnmatchedInput<TIn> ()
		{
			return new UnmatchedRule<TIn> ();
		}
	}

	public class RuleCompound<TIn> {
		protected Func<IRule<TIn>,IRule<TIn>> additionalRuleCallback;

		public RuleCompound (Func<IRule<TIn>,IRule<TIn>> additionalRuleCallback)
		{
			this.additionalRuleCallback = additionalRuleCallback;
		}

		public SequenceQualifier<TIn> AllInputsIn (params TIn [] input)
		{
			return new SequenceQualifier<TIn> (input, additionalRuleCallback);
		}
		public SequenceQualifier<TIn> AllInputsIn (IEnumerable<TIn> input)
		{
			return new SequenceQualifier<TIn> (input, additionalRuleCallback);
		}

		public IRule<TIn> AnyInputIn (params TIn[] input)
		{
			return additionalRuleCallback (new InSetRule<TIn> (input));
		}
		public IRule<TIn> AnyInputIn (IEnumerable<TIn> input)
		{
			return additionalRuleCallback (new InSetRule<TIn> (input));
		}

		public IRule<TIn> InputsWhere (Func<TIn, bool> predicate)
		{
			return additionalRuleCallback (new PredicateRule<TIn> (predicate));
		}

		public IRule<TIn> AnyInput ()
		{
			return additionalRuleCallback (new AnyRule<TIn> ());
		}
	}

	public class SequenceQualifier<TIn> {
		protected IEnumerable<TIn> sequence;
		protected Func<IRule<TIn>, IRule<TIn>> additionalRuleCallback;

		public SequenceQualifier (IEnumerable<TIn> sequence, Func<IRule<TIn>, IRule<TIn>> additionalRuleCallback)
		{
			this.sequence = sequence;
			this.additionalRuleCallback = additionalRuleCallback ?? (rul => rul);
		}

		public IRule<TIn> InThatOrder ()
		{
			return additionalRuleCallback (new InOrderRule<TIn> (sequence));
		}

		public IRule<TIn> InAnyOrder ()
		{
			return additionalRuleCallback (new AnyOrderRule<TIn> (sequence));
		}
	}

	public static class Choose {

		public static EmitterFunc<TIn, TOut> TopOne<TIn, TOut> (params EmitterFunc<TIn, TOut> [] rules)
		{
			return delegate (InputData<TIn> input, out TOut output) {
				output = default (TOut);
				foreach (var rule in rules) {
					RuleResult result = rule (input.NewContext (), out output);
					if (result != RuleResult.NoMatch)
						return result;
				}
				return RuleResult.NoMatch;
			};
		}
	}

	public class CloneableEnumerator<T> : IEnumerator<T> {
		private IEnumerable<T> wrappedEnumerable;
		private IEnumerator<T> wrappedEnumerator;

		private int position;

		public CloneableEnumerator (IEnumerable<T> enumerable, int position)
		{
			this.wrappedEnumerable = enumerable;
			this.position = position;

			this.wrappedEnumerator = wrappedEnumerable.GetEnumerator ();
			for (int i = 0; i < position; i++)
				wrappedEnumerator.MoveNext ();
		}

		public CloneableEnumerator<T> Clone () {
			return new CloneableEnumerator<T> (this.wrappedEnumerable, this.position);
		}

		public void Reset ()
		{
			wrappedEnumerator.Reset ();
		}

		public bool MoveNext ()
		{
			position++;
			return wrappedEnumerator.MoveNext ();
		}

		public T Current {
			get { return wrappedEnumerator.Current; }
		}
		object IEnumerator.Current {
			get { return this.Current; }
		}
		public void Dispose ()
		{
		}
	}
}

