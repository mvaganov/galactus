using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS.StateMachine {
	public class State {
		public string desc;
		public virtual void Enter(IStateRunner sr) { sr.AdvanceStateTree(); }
		public virtual void Execute(IStateRunner sr) { }
		public virtual void Exit(IStateRunner sr) { }
	}
	public class StateKeeper : State {
		protected State currentState;
		public State CurrentState {
			get {
				StateKeeper ns = currentState as StateKeeper;
				return ns != null ? ns.CurrentState : currentState;
			}
		}
		public void SetState(State s, IStateRunner sr) {
			if(currentState != null) { currentState.Exit(sr); }
			currentState = s;
			if(currentState != null) { currentState.Enter(sr); }
		}
		public virtual bool HasStateToAdvance(IStateRunner sr) {
			if(currentState == null) return false;
			StateKeeper ns = currentState as StateKeeper;
			return ns != null && ns.HasStateToAdvance(sr);
		}
		public virtual void Advance(IStateRunner sr) {
			(currentState as StateKeeper).Advance(sr);
		}
		public virtual State PeekNextState(IStateRunner sr) {
			StateKeeper ns = currentState as StateKeeper;
			return ns != null && ns.HasStateToAdvance(sr) ? ns.PeekNextState(sr) : null;
		}
		public override void Enter(IStateRunner sr) { }
		public override void Execute(IStateRunner sr) {
			if(currentState != null) { currentState.Execute(sr); }
		}
		public override void Exit(IStateRunner sr) { }
	}
	public class Branch : StateKeeper {
		public string name;
		public List<State> list;
		public class Vars { public int index; }
		protected Vars vars;

		public override bool HasStateToAdvance(IStateRunner sr) {
			return vars.index < list.Count;
		}
		public override State PeekNextState(IStateRunner sr) {
			State nextState = base.PeekNextState(sr);
			if(nextState == null) {
				if(vars == null) {
					nextState = list[0];
				} else if(vars.index < list.Count - 1) {
					nextState = list[vars.index + 1];
				}
				StateKeeper ns = nextState as StateKeeper;
				if(ns != null) {
					ns.PeekNextState(sr);
				}
			}
			return nextState;
		}
		public override void Advance(IStateRunner sr) {
			State s = list[vars.index];
			StateKeeper ns = s as StateKeeper;
			bool advancedChild = false;
			if(ns != null) {
				if(ns.HasStateToAdvance(sr)) {
					ns.Advance(sr);
					advancedChild = true;
				}
			}
			if(!advancedChild) {
				vars.index++;
				if(vars.index < list.Count) { SetState(list[vars.index], sr); } else { SetState(null, sr); }
			}
		}
		public override void Enter(IStateRunner sr) {
			vars = new Vars();
			if(list.Count > 0) { SetState(list[0], sr); } else { sr.AdvanceStateTree(); }
		}
		public override void Exit(IStateRunner sr) { vars = null; }
	}
}