using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace Engine
{
    public class InputSystem : GameSystem
    {
        private readonly Sdl2Window _window;

        private readonly HashSet<Key> _currentlyPressedKeys = [];
        private readonly HashSet<Key> _newKeysThisFrame = [];

        private readonly HashSet<MouseButton> _currentlyPressedMouseButtons = [];
        private readonly HashSet<MouseButton> _newMouseButtonsThisFrame = [];

        private readonly List<Action<InputSystem>> _callbacks = [];

        private Vector2 _previousSnapshotMousePosition;

        public Vector2 MousePosition
        {
            get
            {
                return CurrentSnapshot.MousePosition;
            }
            set
            {
                // TODO: Rewrite, this can be skipped since this won't get used much

                //if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                //{
                //    Point screenPosition = _window.ClientToScreen(new Point((int)value.X, (int)value.Y));
                //    Mouse.SetPosition(screenPosition.X, screenPosition.Y);
                //    var cursorState = Mouse.GetCursorState();
                //    Point windowPoint = _window.ScreenToClient(new Point(cursorState.X, cursorState.Y));
                //    _previousSnapshotMousePosition = new Vector2(windowPoint.X / _window.ScaleFactor.X, windowPoint.Y / _window.ScaleFactor.Y);
                //}
                //else
                //{
                //    Point screenPosition = new Point((int)value.X, (int)value.Y);
                //    Mouse.SetPosition(screenPosition.X, screenPosition.Y);
                //    var cursorState = Mouse.GetCursorState();
                //    _previousSnapshotMousePosition = new Vector2(cursorState.X / _window.ScaleFactor.X, cursorState.Y / _window.ScaleFactor.Y);
                //}
            }
        }

        public Vector2 MouseDelta { get; private set; }

        public InputSnapshot CurrentSnapshot { get; private set; }

        public InputSystem(Sdl2Window window)
        {
            _window = window;
            window.FocusGained += WindowFocusGained;
            window.FocusLost += WindowFocusLost;
        }

        /// <summary>
        /// Registers an anonmyous callback which is invoked every time the InputSystem is updated.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        public void RegisterCallback(Action<InputSystem> callback)
        {
            _callbacks.Add(callback);
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            var snapshot = _window.PumpEvents();
            UpdateFrameInput(snapshot);
            foreach (var callback in _callbacks)
                callback(this);
        }

        public void WindowFocusLost()
        {
            ClearState();
        }

        public void WindowFocusGained()
        {
            ClearState();
        }

        protected override void OnNewSceneLoadedCore()
        {
            ClearState();
        }

        private void ClearState()
        {
            _currentlyPressedKeys.Clear();
            _newKeysThisFrame.Clear();
            _currentlyPressedMouseButtons.Clear();
            _newMouseButtonsThisFrame.Clear();
        }

        public bool GetKey(Key Key)
        {
            return _currentlyPressedKeys.Contains(Key);
        }

        public bool GetKeyDown(Key Key)
        {
            return _newKeysThisFrame.Contains(Key);
        }

        public bool GetMouseButton(MouseButton button)
        {
            return _currentlyPressedMouseButtons.Contains(button);
        }

        public bool GetMouseButtonDown(MouseButton button)
        {
            return _newMouseButtonsThisFrame.Contains(button);
        }

        public void UpdateFrameInput(InputSnapshot snapshot)
        {
            CurrentSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();

            MouseDelta = CurrentSnapshot.MousePosition - _previousSnapshotMousePosition;
            _previousSnapshotMousePosition = CurrentSnapshot.MousePosition;

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent ke = keyEvents[i];
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            IReadOnlyList<MouseEvent> mouseEvents = snapshot.MouseEvents;
            for (int i = 0; i < mouseEvents.Count; i++)
            {
                MouseEvent me = mouseEvents[i];
                if (me.Down)
                {
                    MouseDown(me.MouseButton);
                }
                else
                {
                    MouseUp(me.MouseButton);
                }
            }
        }

        private void MouseUp(MouseButton MouseButton)
        {
            _currentlyPressedMouseButtons.Remove(MouseButton);
            _newMouseButtonsThisFrame.Remove(MouseButton);
        }

        private void MouseDown(MouseButton MouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(MouseButton))
            {
                _newMouseButtonsThisFrame.Add(MouseButton);
            }
        }

        private void KeyUp(Key Key)
        {
            _currentlyPressedKeys.Remove(Key);
            _newKeysThisFrame.Remove(Key);
        }

        private void KeyDown(Key Key)
        {
            if (_currentlyPressedKeys.Add(Key))
            {
                _newKeysThisFrame.Add(Key);
            }
        }
    }
}
