using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTyper
{
    /// <summary>
    /// Advanced keyboard simulator with humanization and anti-detection features
    /// </summary>
    public static class HumanizedKeyboardSimulator
    {
        private static readonly Random random = new Random();
        private static readonly object lockObj = new object();

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_RETURN = 0x0D;
        private const ushort VK_SPACE = 0x20;
        private const ushort VK_SHIFT = 0x10;
        private const ushort VK_BACK = 0x08;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public enum TypingProfile
        {
            Fast,           // 30-50ms, no humanization
            Natural,        // 50-80ms, 2% typos, variable speed
            SuperSafe,      // 80-150ms, full humanization
            Custom
        }

        public class TypingSettings
        {
            public int BaseDelay { get; set; } = 50;
            public int DelayVariance { get; set; } = 15;
            public double TypoRate { get; set; } = 0.02; // 2%
            public bool EnableHumanization { get; set; } = true;
            public bool EnableTypos { get; set; } = true;
            public TypingProfile Profile { get; set; } = TypingProfile.Natural;

            public static TypingSettings GetProfile(TypingProfile profile)
            {
                return profile switch
                {
                    TypingProfile.Fast => new TypingSettings
                    {
                        BaseDelay = 35,
                        DelayVariance = 10,
                        TypoRate = 0,
                        EnableHumanization = false,
                        EnableTypos = false,
                        Profile = TypingProfile.Fast
                    },
                    TypingProfile.Natural => new TypingSettings
                    {
                        BaseDelay = 60,
                        DelayVariance = 20,
                        TypoRate = 0.02,
                        EnableHumanization = true,
                        EnableTypos = true,
                        Profile = TypingProfile.Natural
                    },
                    TypingProfile.SuperSafe => new TypingSettings
                    {
                        BaseDelay = 100,
                        DelayVariance = 30,
                        TypoRate = 0.03,
                        EnableHumanization = true,
                        EnableTypos = true,
                        Profile = TypingProfile.SuperSafe
                    },
                    _ => new TypingSettings()
                };
            }
        }

        /// <summary>
        /// Validates if window handle is still valid
        /// </summary>
        public static bool ValidateWindowHandle(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && IsWindow(hwnd);
        }

        /// <summary>
        /// Types text with humanization and anti-detection
        /// </summary>
        public static async Task TypeTextAsync(
            string text, 
            TypingSettings settings, 
            CancellationToken cancellationToken,
            IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            int totalChars = text.Length;
            int processedChars = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                char c = text[i];

                // Skip \r
                if (c == '\r')
                    continue;

                // Simulate typo
                if (settings.EnableTypos && ShouldMakeTypo(settings.TypoRate))
                {
                    char typo = GenerateTypo(c);
                    await SendCharAsync(typo, settings);
                    await Task.Delay(GetHumanizedDelay(settings, isCorrection: false), cancellationToken);
                    
                    // Backspace to correct
                    SendKey(VK_BACK);
                    await Task.Delay(GetHumanizedDelay(settings, isCorrection: true), cancellationToken);
                }

                // Send actual character
                await SendCharAsync(c, settings);

                // Calculate delay based on context
                int delay = CalculateContextualDelay(text, i, settings);
                await Task.Delay(delay, cancellationToken);

                processedChars++;
                progress?.Report((int)((double)processedChars / totalChars * 100));
            }
        }

        /// <summary>
        /// Calculates delay based on character context (punctuation, word boundaries, etc.)
        /// </summary>
        private static int CalculateContextualDelay(string text, int index, TypingSettings settings)
        {
            if (!settings.EnableHumanization)
                return settings.BaseDelay;

            char current = text[index];
            char? next = index + 1 < text.Length ? text[index + 1] : null;
            char? prev = index > 0 ? text[index - 1] : null;

            int baseDelay = settings.BaseDelay;

            // Longer pause after punctuation
            if (IsPunctuation(current))
            {
                baseDelay = (int)(baseDelay * 2.5); // 250% slower
            }
            // Pause after space (between words)
            else if (current == ' ')
            {
                baseDelay = (int)(baseDelay * 1.3); // 30% slower
            }
            // Slower at start of word
            else if (prev == ' ' || prev == null)
            {
                baseDelay = (int)(baseDelay * 1.2); // 20% slower
            }
            // Faster in middle of word
            else if (prev != ' ' && next != ' ' && next != null)
            {
                baseDelay = (int)(baseDelay * 0.9); // 10% faster
            }

            // Add Gaussian randomness
            return GetGaussianDelay(baseDelay, settings.DelayVariance);
        }

        /// <summary>
        /// Generates delay with Gaussian distribution (more human-like)
        /// </summary>
        private static int GetGaussianDelay(int mean, int stdDev)
        {
            lock (lockObj)
            {
                // Box-Muller transform for Gaussian distribution
                double u1 = 1.0 - random.NextDouble();
                double u2 = 1.0 - random.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                double randNormal = mean + stdDev * randStdNormal;
                
                // Clamp to reasonable values
                return Math.Max(10, Math.Min(500, (int)randNormal));
            }
        }

        /// <summary>
        /// Gets humanized delay with context awareness
        /// </summary>
        private static int GetHumanizedDelay(TypingSettings settings, bool isCorrection)
        {
            if (isCorrection)
            {
                // Faster correction (panic mode)
                return GetGaussianDelay(settings.BaseDelay / 2, settings.DelayVariance / 2);
            }

            return GetGaussianDelay(settings.BaseDelay, settings.DelayVariance);
        }

        /// <summary>
        /// Determines if a typo should be made
        /// </summary>
        private static bool ShouldMakeTypo(double typoRate)
        {
            lock (lockObj)
            {
                return random.NextDouble() < typoRate;
            }
        }

        /// <summary>
        /// Generates a realistic typo (adjacent key on keyboard)
        /// </summary>
        private static char GenerateTypo(char original)
        {
            // QWERTY keyboard layout adjacency map
            var adjacentKeys = new Dictionary<char, char[]>
            {
                {'a', new[] {'q', 's', 'z'}},
                {'b', new[] {'v', 'g', 'h', 'n'}},
                {'c', new[] {'x', 'd', 'f', 'v'}},
                {'d', new[] {'s', 'e', 'r', 'f', 'c', 'x'}},
                {'e', new[] {'w', 'r', 'd', 's'}},
                {'f', new[] {'d', 'r', 't', 'g', 'v', 'c'}},
                {'g', new[] {'f', 't', 'y', 'h', 'b', 'v'}},
                {'h', new[] {'g', 'y', 'u', 'j', 'n', 'b'}},
                {'i', new[] {'u', 'o', 'k', 'j'}},
                {'j', new[] {'h', 'u', 'i', 'k', 'm', 'n'}},
                {'k', new[] {'j', 'i', 'o', 'l', 'm'}},
                {'l', new[] {'k', 'o', 'p'}},
                {'m', new[] {'n', 'j', 'k'}},
                {'n', new[] {'b', 'h', 'j', 'm'}},
                {'o', new[] {'i', 'p', 'l', 'k'}},
                {'p', new[] {'o', 'l'}},
                {'q', new[] {'w', 'a'}},
                {'r', new[] {'e', 't', 'f', 'd'}},
                {'s', new[] {'a', 'w', 'e', 'd', 'x', 'z'}},
                {'t', new[] {'r', 'y', 'g', 'f'}},
                {'u', new[] {'y', 'i', 'j', 'h'}},
                {'v', new[] {'c', 'f', 'g', 'b'}},
                {'w', new[] {'q', 'e', 's', 'a'}},
                {'x', new[] {'z', 's', 'd', 'c'}},
                {'y', new[] {'t', 'u', 'h', 'g'}},
                {'z', new[] {'a', 's', 'x'}},
            };

            char lower = char.ToLower(original);
            
            if (adjacentKeys.ContainsKey(lower))
            {
                var adjacent = adjacentKeys[lower];
                lock (lockObj)
                {
                    char typo = adjacent[random.Next(adjacent.Length)];
                    return char.IsUpper(original) ? char.ToUpper(typo) : typo;
                }
            }

            return original;
        }

        /// <summary>
        /// Checks if character is punctuation
        /// </summary>
        private static bool IsPunctuation(char c)
        {
            return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':';
        }

        /// <summary>
        /// Sends a character asynchronously
        /// </summary>
        private static async Task SendCharAsync(char c, TypingSettings settings)
        {
            await Task.Run(() => SendCharSync(c));
        }

        /// <summary>
        /// Sends a character synchronously (actual SendInput call)
        /// </summary>
        private static void SendCharSync(char c)
        {
            if (c == '\r')
                return;

            if (c == '\n')
            {
                SendShiftEnter();
                return;
            }

            if (c == ' ')
            {
                SendKey(VK_SPACE);
                return;
            }

            INPUT[] inputs = new INPUT[2];

            // Key down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendShiftEnter()
        {
            INPUT[] inputs = new INPUT[4];

            // Shift down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_SHIFT,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Enter down
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_RETURN,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Enter up
            inputs[2] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_RETURN,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Shift up
            inputs[3] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_SHIFT,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendKey(ushort vkCode)
        {
            INPUT[] inputs = new INPUT[2];

            // Key down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vkCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vkCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
