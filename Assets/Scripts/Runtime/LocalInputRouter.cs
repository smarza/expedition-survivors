using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectExpedition
{
    /// <summary>
    /// Stable local-device ownership for gameplay and menus. In two-player
    /// sessions an active gamepad is claimed once and remains attached to that
    /// player. With one gamepad, keyboard is P1 and the gamepad is P2. Solo and
    /// online instances accept any active local gamepad.
    /// </summary>
    public static class LocalInputRouter
    {
        private const float MovementDeadzone = 0.2f;
        private static readonly int[] AssignedDeviceIds = { -1, -1 };
        private static int _sessionPlayerCount = 1;

        public static void BeginSession(int playerCount)
        {
            _sessionPlayerCount = Mathf.Clamp(playerCount, 1, 2);
            AssignedDeviceIds[0] = -1;
            AssignedDeviceIds[1] = -1;
            if (_sessionPlayerCount == 2 && Gamepad.all.Count == 1)
                AssignedDeviceIds[1] = Gamepad.all[0].deviceId;
        }

        public static Vector2 ReadMovement(int playerIndex, int playerCount)
        {
            EnsureSession(playerCount);
            var move = ReadKeyboard(playerIndex, playerCount);
            if (playerCount <= 1)
            {
                var gamepadMove = StrongestGamepadMovement();
                if (gamepadMove.sqrMagnitude > move.sqrMagnitude) move = gamepadMove;
            }
            else
            {
                RefreshAssignments(playerCount);
                var gamepad = AssignedGamepad(playerIndex);
                if (gamepad != null) move += ApplyMovementDeadzone(gamepad.leftStick.ReadValue());
            }
            return Vector2.ClampMagnitude(move, 1f);
        }

        public static bool UltimatePressed(int playerIndex, int playerCount)
        {
            EnsureSession(playerCount);
            var keyboard = Keyboard.current;
            var keyboardPressed = keyboard != null && (playerIndex == 0
                ? keyboard.spaceKey.wasPressedThisFrame
                : keyboard.enterKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame);
            if (keyboardPressed) return true;
            if (playerCount <= 1) return AnyGamepadUltimatePressed();
            RefreshAssignments(playerCount);
            return GamepadUltimatePressed(AssignedGamepad(playerIndex));
        }

        public static bool RushPressed(int playerIndex, int playerCount) => UltimatePressed(playerIndex, playerCount);

        public static bool PausePressed()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            for (var i = 0; i < Gamepad.all.Count; i++)
                if (Gamepad.all[i].startButton.wasPressedThisFrame) return true;
            return false;
        }

        public static bool DetailsPressed()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) return true;
            for (var i = 0; i < Gamepad.all.Count; i++)
                if (Gamepad.all[i].selectButton.wasPressedThisFrame) return true;
            return false;
        }

        public static bool MetricsPressed()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) return true;
            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var gamepad = Gamepad.all[i];
                if (gamepad.selectButton.wasPressedThisFrame && gamepad.leftShoulder.isPressed) return true;
            }
            return false;
        }

        public static int MenuHorizontalPressed(int playerIndex, int playerCount)
        {
            EnsureSession(playerCount);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (playerIndex == 0)
                {
                    if (keyboard.aKey.wasPressedThisFrame || playerCount == 1 && keyboard.leftArrowKey.wasPressedThisFrame) return -1;
                    if (keyboard.dKey.wasPressedThisFrame || playerCount == 1 && keyboard.rightArrowKey.wasPressedThisFrame) return 1;
                }
                else
                {
                    if (keyboard.leftArrowKey.wasPressedThisFrame) return -1;
                    if (keyboard.rightArrowKey.wasPressedThisFrame) return 1;
                }
            }
            var gamepad = playerCount <= 1 ? MostRecentlyActiveGamepad() : AssignedForMenu(playerIndex, playerCount);
            return HorizontalFrom(gamepad);
        }

        public static int MenuVerticalPressed(int playerIndex, int playerCount)
        {
            EnsureSession(playerCount);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (playerIndex == 0)
                {
                    if (keyboard.wKey.wasPressedThisFrame || playerCount == 1 && keyboard.upArrowKey.wasPressedThisFrame) return -1;
                    if (keyboard.sKey.wasPressedThisFrame || playerCount == 1 && keyboard.downArrowKey.wasPressedThisFrame) return 1;
                }
                else
                {
                    if (keyboard.upArrowKey.wasPressedThisFrame) return -1;
                    if (keyboard.downArrowKey.wasPressedThisFrame) return 1;
                }
            }
            var gamepad = playerCount <= 1 ? MostRecentlyActiveGamepad() : AssignedForMenu(playerIndex, playerCount);
            return VerticalFrom(gamepad);
        }

        public static bool MenuSubmitPressed(int playerIndex, int playerCount)
        {
            EnsureSession(playerCount);
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (playerIndex == 0 && (keyboard.spaceKey.wasPressedThisFrame || playerCount == 1 && keyboard.enterKey.wasPressedThisFrame)) return true;
                if (playerIndex == 1 && (keyboard.enterKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame)) return true;
            }
            if (playerCount <= 1) return AnyGamepadSubmitPressed();
            RefreshAssignments(playerCount);
            var gamepad = AssignedGamepad(playerIndex);
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }

        public static int AnyMenuHorizontalPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) return -1;
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) return 1;
            }
            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var value = HorizontalFrom(Gamepad.all[i]);
                if (value != 0) return value;
            }
            return 0;
        }

        public static int AnyMenuVerticalPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) return -1;
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) return 1;
            }
            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var value = VerticalFrom(Gamepad.all[i]);
                if (value != 0) return value;
            }
            return 0;
        }

        public static bool AnyMenuSubmitPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) return true;
            return AnyGamepadSubmitPressed();
        }

        public static bool MenuBackPressed()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            for (var i = 0; i < Gamepad.all.Count; i++)
                if (Gamepad.all[i].buttonEast.wasPressedThisFrame) return true;
            return false;
        }

        public static int LevelChoicePressed(int playerIndex, int playerCount)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && playerIndex == 0)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 0;
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 1;
                if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 2;
                if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 3;
            }
            var gamepad = playerCount <= 1 ? MostRecentlyActiveGamepad() : AssignedForMenu(playerIndex, playerCount);
            if (gamepad == null) return -1;
            if (gamepad.buttonWest.wasPressedThisFrame) return 0;
            if (gamepad.buttonNorth.wasPressedThisFrame) return 1;
            if (gamepad.buttonEast.wasPressedThisFrame) return 2;
            if (gamepad.rightShoulder.wasPressedThisFrame) return 3;
            return -1;
        }

        public static int LevelChoicePressed()
        {
            for (var i = 0; i < Mathf.Max(1, _sessionPlayerCount); i++)
            {
                var choice = LevelChoicePressed(i, _sessionPlayerCount);
                if (choice >= 0) return choice;
            }
            return -1;
        }

        public static bool StartPressed() => AnyMenuSubmitPressed();

        public static string AssignmentLabel(int playerIndex, int playerCount)
        {
            if (playerCount <= 1) return Gamepad.all.Count > 0 ? "KEYBOARD + ACTIVE GAMEPAD" : "KEYBOARD";
            RefreshAssignments(playerCount);
            var gamepad = AssignedGamepad(playerIndex);
            if (gamepad != null) return gamepad.displayName.ToUpperInvariant();
            return playerIndex == 0 ? "KEYBOARD / MOVE A GAMEPAD TO CLAIM" : "ARROWS / MOVE A GAMEPAD TO CLAIM";
        }

        private static void EnsureSession(int playerCount)
        {
            playerCount = Mathf.Clamp(playerCount, 1, 2);
            if (_sessionPlayerCount != playerCount) BeginSession(playerCount);
        }

        private static void RefreshAssignments(int playerCount)
        {
            if (playerCount <= 1) return;
            for (var player = 0; player < 2; player++)
            {
                if (AssignedDeviceIds[player] >= 0 && FindGamepad(AssignedDeviceIds[player]) == null)
                    AssignedDeviceIds[player] = -1;
            }
            if (Gamepad.all.Count == 1)
            {
                AssignedDeviceIds[0] = -1;
                AssignedDeviceIds[1] = Gamepad.all[0].deviceId;
                return;
            }
            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var gamepad = Gamepad.all[i];
                if (!HasMeaningfulActivity(gamepad) || IsAssigned(gamepad.deviceId)) continue;
                if (AssignedDeviceIds[0] < 0) AssignedDeviceIds[0] = gamepad.deviceId;
                else if (AssignedDeviceIds[1] < 0) AssignedDeviceIds[1] = gamepad.deviceId;
            }
        }

        private static Gamepad AssignedForMenu(int playerIndex, int playerCount)
        {
            RefreshAssignments(playerCount);
            return AssignedGamepad(playerIndex);
        }

        private static Gamepad AssignedGamepad(int playerIndex) =>
            playerIndex >= 0 && playerIndex < AssignedDeviceIds.Length ? FindGamepad(AssignedDeviceIds[playerIndex]) : null;

        private static Gamepad FindGamepad(int deviceId)
        {
            if (deviceId < 0) return null;
            for (var i = 0; i < Gamepad.all.Count; i++)
                if (Gamepad.all[i].deviceId == deviceId) return Gamepad.all[i];
            return null;
        }

        private static bool IsAssigned(int deviceId) =>
            AssignedDeviceIds[0] == deviceId || AssignedDeviceIds[1] == deviceId;

        private static bool HasMeaningfulActivity(Gamepad gamepad)
        {
            if (gamepad == null) return false;
            return gamepad.leftStick.ReadValue().sqrMagnitude > 0.16f ||
                   gamepad.rightStick.ReadValue().sqrMagnitude > 0.16f ||
                   gamepad.dpad.ReadValue().sqrMagnitude > 0.1f ||
                   gamepad.buttonSouth.isPressed || gamepad.buttonNorth.isPressed ||
                   gamepad.buttonWest.isPressed || gamepad.buttonEast.isPressed ||
                   gamepad.leftShoulder.isPressed || gamepad.rightShoulder.isPressed ||
                   gamepad.leftTrigger.isPressed || gamepad.rightTrigger.isPressed;
        }

        private static Vector2 ReadKeyboard(int playerIndex, int playerCount)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            var move = Vector2.zero;
            if (playerIndex == 0)
            {
                if (keyboard.aKey.isPressed) move.x--;
                if (keyboard.dKey.isPressed) move.x++;
                if (keyboard.sKey.isPressed) move.y--;
                if (keyboard.wKey.isPressed) move.y++;
                if (playerCount <= 1)
                {
                    if (keyboard.leftArrowKey.isPressed) move.x--;
                    if (keyboard.rightArrowKey.isPressed) move.x++;
                    if (keyboard.downArrowKey.isPressed) move.y--;
                    if (keyboard.upArrowKey.isPressed) move.y++;
                }
            }
            else
            {
                if (keyboard.leftArrowKey.isPressed) move.x--;
                if (keyboard.rightArrowKey.isPressed) move.x++;
                if (keyboard.downArrowKey.isPressed) move.y--;
                if (keyboard.upArrowKey.isPressed) move.y++;
            }
            return move;
        }

        private static Vector2 StrongestGamepadMovement()
        {
            var strongest = Vector2.zero;
            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var value = ApplyMovementDeadzone(Gamepad.all[i].leftStick.ReadValue());
                if (value.sqrMagnitude > strongest.sqrMagnitude) strongest = value;
            }
            return strongest;
        }

        private static Vector2 ApplyMovementDeadzone(Vector2 value)
        {
            var magnitude = value.magnitude;
            if (magnitude <= MovementDeadzone) return Vector2.zero;
            var normalizedMagnitude = Mathf.Clamp01((magnitude - MovementDeadzone) / (1f - MovementDeadzone));
            return value.normalized * normalizedMagnitude;
        }

        private static Gamepad MostRecentlyActiveGamepad()
        {
            if (Gamepad.current != null) return Gamepad.current;
            for (var i = 0; i < Gamepad.all.Count; i++)
                if (HasMeaningfulActivity(Gamepad.all[i])) return Gamepad.all[i];
            return Gamepad.all.Count > 0 ? Gamepad.all[0] : null;
        }

        private static bool AnyGamepadUltimatePressed()
        {
            for (var i = 0; i < Gamepad.all.Count; i++)
                if (GamepadUltimatePressed(Gamepad.all[i])) return true;
            return false;
        }

        private static bool GamepadUltimatePressed(Gamepad gamepad) =>
            gamepad != null && (gamepad.rightShoulder.wasPressedThisFrame || gamepad.rightTrigger.wasPressedThisFrame);

        private static bool AnyGamepadSubmitPressed()
        {
            for (var i = 0; i < Gamepad.all.Count; i++)
                if (Gamepad.all[i].buttonSouth.wasPressedThisFrame) return true;
            return false;
        }

        private static int HorizontalFrom(Gamepad gamepad)
        {
            if (gamepad == null) return 0;
            if (gamepad.dpad.left.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame) return -1;
            if (gamepad.dpad.right.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame) return 1;
            return 0;
        }

        private static int VerticalFrom(Gamepad gamepad)
        {
            if (gamepad == null) return 0;
            if (gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame) return -1;
            if (gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame) return 1;
            return 0;
        }
    }
}
