﻿using System;
using System.Collections.Generic;

namespace RTextNppPlugin.RTextEditor.StateEngine
{
    /**
    * \enum    ProcessState
    *
    * \brief   Values that represent ProcessState.
    */
    public enum ProcessState
    {
        Closed,
        Connecting,
        Connected,
        Busy
    }
    /**
     * \enum    Command
     *
     * \brief   Values that represent Command.
     */
    public enum Command
    {
        Connect,
        Execute,
        ExecuteFinished,
        Disconnected
    }

    public class StateMachine
    {
        internal class StateTransition
        {
            readonly ProcessState mCurrentState;
            readonly Command mCommand;

            public StateTransition(ProcessState currentState, Command command)
            {
                mCurrentState = currentState;
                mCommand = command;
            }

            public override int GetHashCode()
            {
                return 17 + 31 * mCurrentState.GetHashCode() + 31 * mCommand.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                StateTransition other = obj as StateTransition;
                return other != null && this.mCurrentState == other.mCurrentState && this.mCommand == other.mCommand;
            }
        }

        internal struct ProcessStateWithAction
        {
            private ProcessState mTargetState;  //!< State of the target
            private Action mTransitionAction;   //!< The transition action
            private Func<bool> mGuard;          //!< The guard for this transition

            /**
             * @fn  public ProcessStateWithAction(ProcessState targetState, Action transitionAction = null)
             *
             * @brief   Constructor.
             *
             * @author  Stefanos Anastasiou
             * @date    15.12.2012
             *
             * @param   targetState         State of the target.
             * @param   transitionAction    (optional) the transition action.
             */
            public ProcessStateWithAction(ProcessState targetState, Action transitionAction = null, Func<bool> guard = null)
            {
                mTargetState = targetState;
                mTransitionAction = transitionAction;
                mGuard = guard;
            }

            /**
             * @property    public ProcessState TargetState
             *
             * @brief   Gets or sets the state of the target.
             *
             * @return  The target state.
             */
            public ProcessState TargetState { get { return this.mTargetState; } private set { this.mTargetState = value; } }

            /**
             * @property    public Action TransitionAction
             *
             * @brief   Gets or sets the transition action.
             *
             * @return  The transition action.
             */
            public Action TransitionAction { get { return this.mTransitionAction; } private set { this.mTransitionAction = value; } }

            /**
             * @property    public Func<bool> Guard
             *
             * @brief   Gets or sets the guard.
             *
             * @return  The guard.
             */
            public Func<bool> Guard { get { return this.mGuard; } private set { this.mGuard = value; } }
        }

        private Dictionary<StateTransition, ProcessStateWithAction> mTransitions;

        private readonly Connector mConnector;

        internal ProcessState CurrentState { get; private set; }

        public StateMachine(Connector connector)
        {
            CurrentState = ProcessState.Closed;
            mTransitions = new Dictionary<StateTransition, ProcessStateWithAction>();
            mConnector   = connector;
        }

        /**
         * @fn  internal void addStateTransition(StateTransition transition,
         *      ProcessStateWithAction processState)
         *
         * @brief   Adds a state transition to the FSM.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @param   transition      The transition.
         * @param   processState    State of the process.
         */
        internal void addStateTransition(StateTransition transition, ProcessStateWithAction processState)
        {
            if (!mTransitions.ContainsKey(transition))
            {
                mTransitions.Add(transition, processState);
            }
        }

        /**
         * @fn  internal ProcessState GetNext(Command command)
         *
         * @brief   Gets the next state but doesn't transition to it.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @exception   Exception   Thrown when an exception error condition occurs.
         *
         * @param   command The command.
         *
         * @return  The next.
         */
        internal ProcessState GetNext(Command command)
        {
            StateTransition transition = new StateTransition(CurrentState, command);
            ProcessStateWithAction nextState;
            if (!mTransitions.TryGetValue(transition, out nextState))
                throw new Exception("Invalid transition: " + CurrentState + " -> " + command);
            if (nextState.Guard == null)
            {
                if (nextState.TransitionAction != null)
                {
                    nextState.TransitionAction();
                }
                mConnector.OnFsmTransition(nextState.TargetState);
                return nextState.TargetState;
            }
            else if ( nextState.Guard())
            {
                if (nextState.TransitionAction != null)
                {
                    nextState.TransitionAction();
                }
                mConnector.OnFsmTransition(nextState.TargetState);
                return nextState.TargetState;
            }
            else
            {
                return CurrentState;
            }
        }

        /**
         * @fn  internal ProcessState MoveNext(Command command)
         *
         * @brief   Moves to the next state.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @param   command The command.
         *
         * @return  The next state.
         */
        internal ProcessState MoveNext(Command command)
        {
            CurrentState = GetNext(command);
            return CurrentState;
        }
    }
}