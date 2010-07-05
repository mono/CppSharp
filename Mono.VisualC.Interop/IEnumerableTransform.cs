using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Mono.VisualC.Interop {


	public delegate bool EmitterFunc<TIn, TOut> (InputData<TIn> input, out TOut output);

	public struct InputData<TIn> {
		public IEnumerable<TIn> AllValues;
		public LookaheadEnumerator<TIn> Enumerator;
		public List<IRule<TIn>> MatchedRules;
		public InputData (IEnumerable<TIn> input,  LookaheadEnumerator<TIn> enumerator)
		{
			this.AllValues = input;
			this.Enumerator = enumerator;
			this.MatchedRules = new List<IRule<TIn>> ();
		}

		public TIn Value {
			get { return Enumerator.Current; }
		}
	}

	#region Rules
	public interface IRule<TIn> {
		bool SatisfiedBy (InputData<TIn> input);
	}

	// yields all inputs indescriminately
	public class AnyRule<TIn> : IRule<TIn> {
		public AnyRule ()
		{
		}
		public bool SatisfiedBy (InputData<TIn> input)
		{
			return true;
		}
	}

	// yields all inputs that haven't satisfied
	//  any other rule
	public class UnmatchedRule<TIn> : IRule<TIn> {
		public UnmatchedRule ()
		{
		}
		public bool SatisfiedBy (InputData<TIn> input)
		{
			return !input.MatchedRules.Any ();
		}
	}

	// yields input if it hasn't been satisfied before
	public class FirstRule<TIn> : IRule<TIn> {
		protected bool triggered = false;
		public FirstRule ()
		{
		}
		public bool SatisfiedBy (InputData<TIn> input)
		{
			if (!triggered) {
				triggered = true;
				return true;
			}
			return false;
		}
	}

	// yields input if it is the last that would satisfy
	// all its matched rules
	public class LastRule<TIn> : IRule<TIn> {
		public LastRule ()
		{
		}
		public bool SatisfiedBy (InputData<TIn> input)
		{
			IEnumerator<TIn> item = input.Enumerator;

			while (item.MoveNext ()) {
				foreach (var prevRule in input.MatchedRules) {
					if (prevRule.SatisfiedBy (input))
						return false;
				}
			}

			return true;
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
		public bool SatisfiedBy (InputData<TIn> input)
		{
			return conditions.Contains (input.Value);
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
		public bool SatisfiedBy (InputData<TIn> inputData)
		{
			if (verified && verifiedItems.Count > 0) {
				verifiedItems.Dequeue ();
				return false;
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
				return true;
			} else
				verifiedItems.Clear ();

			return false;
		}
	}

	public class InOrderRule<TIn> : IRule<TIn> {
		protected IEnumerable<IRule<TIn>> conditions;

		protected bool verified = false;
		protected int verifiedCount = 0;

		public InOrderRule (params IRule<TIn> [] conditions)
		{
			this.conditions = conditions;
		}
		public InOrderRule (IEnumerable<IRule<TIn>> conditions)
		{
			this.conditions = conditions;
		}
		public bool SatisfiedBy (InputData<TIn> inputData)
		{
			if (verified && verifiedCount > 0) {
				verifiedCount--;
				return false;
			} else
				verified = false;

			IEnumerator<IRule<TIn>> condition = conditions.GetEnumerator ();
			IEnumerator<TIn> input = inputData.Enumerator;

			while (condition.MoveNext () && condition.Current.SatisfiedBy (inputData)) {
				verifiedCount++;
				if (!input.MoveNext ()) break;
			}
			if (verifiedCount == conditions.Count ()) {
				verified = true;
				verifiedCount--;
				return true;
			} else
				verifiedCount = 0;

			return false;
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
		public bool SatisfiedBy (InputData<TIn> input)
		{
			foreach (var rule in rules) {
				if (!rule.SatisfiedBy (input))
					return false;

				input.MatchedRules.Add (rule);
			}
			return true;
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
		public bool SatisfiedBy (InputData<TIn> input)
		{
			bool satisfied = false;
			foreach (var rule in rules) {
				if (!rule.SatisfiedBy (input))
					continue;

				satisfied = true;
				input.MatchedRules.Add (rule);
			}

			return satisfied;
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
			return additionalRuleCallback(new InSetRule<TIn> (input));
		}
		public IRule<TIn> AnyInputIn (IEnumerable<TIn> input)
		{
			return additionalRuleCallback(new InSetRule<TIn> (input));
		}
		public IRule<TIn> AnyInput ()
		{
			return additionalRuleCallback(new AnyRule<TIn> ());
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
			return additionalRuleCallback (new InOrderRule<TIn> (sequence.Select (tin => (IRule<TIn>)new InSetRule<TIn> (tin))));
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
					if (rule (input, out output))
						return true;

					input.Enumerator.Validate ();
				}
				return false;
			};
		}
	}

	public class LookaheadEnumerator<T> : IEnumerator<T> {
		private IEnumerable<T> wrappedEnumerable;
		private IEnumerator<T> wrappedEnumerator;

		private int position;
		private bool invalidated;

		public LookaheadEnumerator (IEnumerable<T> enumerable, int position)
		{
			this.wrappedEnumerable = enumerable;
			this.position = position;
			this.invalidated = true;
			Validate ();
		}

		public void Reset ()
		{
			invalidated = true;
			wrappedEnumerator.Reset ();
		}

		public void Validate ()
		{
			if (!invalidated) return;
			wrappedEnumerator = wrappedEnumerable.GetEnumerator ();
			for (int i = 0; i < position; i++)
				wrappedEnumerator.MoveNext ();

			invalidated = false;
		}

		public bool ValidMoveNext ()
		{
			Validate ();
			position++;
			return wrappedEnumerator.MoveNext ();
		}

		public bool MoveNext ()
		{
			invalidated = true;
			return wrappedEnumerator.MoveNext ();
		}

		public bool Invalidated {
			get { return invalidated; }
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

	public static class EnumerableSequenceExtensions {

		// Transforms an IEnumerable into another by specific rules.

		public static IEnumerable<TOut> Transform<TIn, TOut> (this IEnumerable<TIn> input, params EmitterFunc<TIn, TOut> [] rules)
		{
			LookaheadEnumerator<TIn> enumerator = new LookaheadEnumerator<TIn> (input, 0);

			while (enumerator.ValidMoveNext ()) {

				InputData<TIn> inputData = new InputData<TIn> (input, enumerator);
				foreach (var rule in rules) {
					TOut output;
					if (rule (inputData, out output))
						yield return output;

					enumerator.Validate ();
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


		public static IRule<TIn> FollowedBy<TIn> (this IRule<TIn> previousRules, IRule<TIn> parentheticalRules)
		{
			return new InOrderRule<TIn> (previousRules, parentheticalRules);
		}
		public static RuleCompound<TIn> FollowedBy<TIn> (this IRule<TIn> previousRules)
		{
			return new RuleCompound<TIn> ((subsequentRules) => {
				return FollowedBy<TIn> (previousRules, subsequentRules);
			});
		}

		public static EmitterFunc<TIn, TOut> Emit<TIn, TOut> (this IRule<TIn> rule, Func<TIn, TOut> result)
		{
			return delegate (InputData<TIn> input, out TOut output) {
				output = default (TOut);
				TIn value = input.Value;
				if (rule.SatisfiedBy (input)) {
					input.MatchedRules.Add (rule);
					output = result (value);
					return true;
				}
				return false;
			};
		}
		public static EmitterFunc<TIn, TOut> Emit<TIn, TOut> (this IRule<TIn> rule, TOut result)
		{
			return Emit (rule, (input) => result);
		}

	}
}

