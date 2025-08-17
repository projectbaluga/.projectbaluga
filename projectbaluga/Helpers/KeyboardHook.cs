using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace projectbaluga.Helpers
{
    public class KeyboardHook
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(int hookHandle);

        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int hookHandle, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public const int WH_KEYBOARD_LL = 13;
        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;
        public const int VK_MENU = 0x12;
        public const int VK_SHIFT = 0x10;
        public const int VK_TAB = 0x09;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_F1 = 0x70;
        public const int VK_F2 = 0x71;
        public const int VK_F3 = 0x72;
        public const int VK_F4 = 0x73;
        public const int VK_F5 = 0x74;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_F10 = 0x79;
        public const int VK_F11 = 0x7A;
        public const int VK_F12 = 0x7B;
        public const int VK_CONTROL = 0x11;
        private const int VK_SPACE = 0x20;

        public delegate int LowLevelKeyboardProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public uint dwExtraInfo;
        }

        private int _hookHandle = 0;
        private LowLevelKeyboardProc _proc;

        public void StartHook()
        {
            _proc = new LowLevelKeyboardProc(HookCallback);
            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle("user32.dll"), 0);
        }

        public void StopHook()
        {
            UnhookWindowsHookEx(_hookHandle);
        }

        private int HookCallback(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (nCode >= 0)
            {
                if (IsUnlockShortcutPressed(lParam.vkCode, wParam))
                {
                    UnlockApplication();
                    return 1;
                }

                if (IsBlockedKey(lParam.vkCode))
                {
                    return 1;
                }
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, ref lParam);
        }

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        private bool IsUnlockShortcutPressed(uint vkCode, int wParam)
        {
            if (wParam != 256) return false;

            if (vkCode == 0x55)
            {
                bool isCtrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                bool isAltPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0;

                if (isCtrlPressed && isAltPressed)
                {
                    return true;
                }
            }

            return false;
        }

        private void UnlockApplication()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                (Application.Current.MainWindow as MainWindow)?.Unlock();
            });
        }

        private bool IsBlockedKey(uint vkCode)
        {
            return vkCode == VK_LWIN || vkCode == VK_RWIN || vkCode == VK_MENU ||
                   vkCode == VK_SHIFT || vkCode == VK_TAB || vkCode == VK_ESCAPE ||
                   vkCode == VK_F1 || vkCode == VK_F2 || vkCode == VK_F3 || vkCode == VK_F4 ||
                   vkCode == VK_F5 || vkCode == VK_F6 || vkCode == VK_F7 || vkCode == VK_F8 ||
                   vkCode == VK_F9 || vkCode == VK_F10 || vkCode == VK_F11 || vkCode == VK_F12 ||
                   vkCode == VK_SPACE || vkCode == VK_CONTROL;
        }
    }
}
