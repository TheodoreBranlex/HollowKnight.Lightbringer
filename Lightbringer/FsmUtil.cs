using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;

// The author of the original probably prefers to remain anonymous

namespace Lightbringer
{
    public static class FsmUtil
    {
        public static FsmState GetState(this PlayMakerFSM fsm, string stateName)
        {
            return (from t in fsm.FsmStates where t.Name == stateName select t).FirstOrDefault();
        }

        public static void AddState(this PlayMakerFSM fsm, string stateName)
        {
            FsmState[] states = fsm.FsmStates;
            Array.Resize(ref states, states.Length + 1);
            states[states.Length - 1] = new FsmState(fsm.Fsm) { Name = stateName };
            fsm.Fsm.States = states;
        }

        public static void RemoveState(this PlayMakerFSM fsm, string stateName)
        {
            FsmState state = fsm.GetState(stateName);
            fsm.Fsm.States = fsm.FsmStates.Where(x => x != state).ToArray();
        }

        public static FsmStateAction GetAction(this PlayMakerFSM fsm, string stateName, int index)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                if (t.Name != stateName) continue;
                return t.Actions[index];
            }
            return null;
        }

        public static T GetAction<T>(this PlayMakerFSM fsm, string stateName, int index) where T : FsmStateAction
        {
            return GetAction(fsm, stateName, index) as T;
        }

        public static void AddAction(this PlayMakerFSM fsm, string stateName, FsmStateAction action)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                if (t.Name != stateName) continue;
                FsmStateAction[] actions = t.Actions;

                Array.Resize(ref actions, actions.Length + 1);
                actions[actions.Length - 1] = action;

                t.Actions = actions;
            }
        }

        public static void AddAction(this PlayMakerFSM fsm, string stateName, Action action)
        {
            AddAction(fsm, stateName, new FsmAction(action));
        }

        public static void AddAction(this PlayMakerFSM fsm, string stateName, Func<IEnumerator> coroutine)
        {
            AddAction(fsm, stateName, () => { fsm.Fsm.Owner.StartCoroutine(coroutine()); });
        }

        public static void ReplaceAction(this PlayMakerFSM fsm, string stateName, int index, FsmStateAction action)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                if (t.Name != stateName) continue;
                t.Actions[index] = action;
            }
        }

        public static void ReplaceAction(this PlayMakerFSM fsm, string stateName, int index, Action action)
        {
            ReplaceAction(fsm, stateName, index, new FsmAction(action));
        }

        public static void ReplaceAction(this PlayMakerFSM fsm, string stateName, int index, Func<IEnumerator> coroutine)
        {
            ReplaceAction(fsm, stateName, index, () => { fsm.Fsm.Owner.StartCoroutine(coroutine()); });
        }

        public static void RemoveAction(this PlayMakerFSM fsm, string stateName, int index)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                if (t.Name != stateName) continue;
                FsmStateAction action = fsm.GetAction(stateName, index);
                t.Actions = t.Actions.Where(x => x != action).ToArray();
            }
        }

        public static void AddTransition(this PlayMakerFSM fsm, string stateName, string eventName, string toState)
        {
            FsmState targetState = fsm.GetState(toState);
            foreach (FsmState t in fsm.FsmStates)
            {
                if (t.Name != stateName) continue;
                FsmTransition[] transitions = t.Transitions;
                Array.Resize(ref transitions, transitions.Length + 1);
                transitions[transitions.Length - 1] = new FsmTransition
                {
                    FsmEvent = FsmEvent.GetFsmEvent(eventName),
                    ToState = toState,
                    ToFsmState = targetState
                };
                t.Transitions = transitions;
            }
        }

        public static void ReplaceTransition(this PlayMakerFSM fsm, string stateName, string eventName, string toState)
        {
            FsmState targetState = fsm.GetState(toState);
            foreach (FsmState state in fsm.FsmStates)
            {
                if (state.Name != stateName) continue;
                foreach (FsmTransition trans in state.Transitions)
                {
                    if (trans.EventName == eventName)
                    {
                        trans.ToState = toState;
                        trans.ToFsmState = targetState;
                    }
                }
            }
        }

        public static void RemoveTransition(this PlayMakerFSM fsm, string state, string transition)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                if (state != t.Name) continue;
                t.Transitions = t.Transitions.Where(trans => transition != trans.ToState).ToArray();
            }
        }

        public static void RemoveTransitions(this PlayMakerFSM fsm, List<string> states, List<string> transitions)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                if (!states.Contains(t.Name)) continue;
                List<FsmTransition> transList = new List<FsmTransition>();
                foreach (FsmTransition trans in t.Transitions)
                {
                    if (!transitions.Contains(trans.ToState))
                        transList.Add(trans);
                }

                t.Transitions = transList.ToArray();
            }
        }
    }

    public class FsmAction : FsmStateAction
    {
        private readonly Action action;

        public FsmAction(Action a)
        {
            action = a;
        }

        public override void OnEnter()
        {
            action?.Invoke();
            Finish();
        }
    }
}