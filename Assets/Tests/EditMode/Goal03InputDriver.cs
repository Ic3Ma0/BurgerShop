using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BurgerShop.Tests.EditMode
{
    // EditMode test coroutines run on editor ticks. Inject on each game frame so
    // background/batch execution exercises PlayerMotor with a continuously held key.
    public sealed class Goal03InputDriver : IDisposable
    {
        public Keyboard Keyboard;
        public KeyboardState State;

        public Goal03InputDriver(Keyboard keyboard)
        {
            Keyboard = keyboard;
            InputSystem.onAfterUpdate += InjectState;
        }

        void InjectState()
        {
            if (InputState.currentUpdateType == InputUpdateType.Dynamic && Keyboard != null && Keyboard.added)
                InputState.Change(Keyboard, State);
        }

        public void Dispose() => InputSystem.onAfterUpdate -= InjectState;
    }
}
