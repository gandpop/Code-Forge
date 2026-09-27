using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeForge.UI
{
    public class BreakpointManager : MonoBehaviour
    {
        private static BreakpointManager instance;
        public static BreakpointManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<BreakpointManager>();
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        private HashSet<int> activeBreakpoints = new HashSet<int>();
        public bool IsPaused { get; private set; } = false;
        private bool stepRequested = false;

        public event Action<int, bool> OnBreakpointToggled; // line, isActive
        public event Action<int, string, Dictionary<string, string>> OnBreakpointHit;
        public event Action OnResumed;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        public bool HasBreakpoint(int line)
        {
            return activeBreakpoints.Contains(line);
        }

        public bool ToggleBreakpoint(int line)
        {
            bool nowActive;
            if (activeBreakpoints.Contains(line))
            {
                activeBreakpoints.Remove(line);
                nowActive = false;
            }
            else
            {
                activeBreakpoints.Add(line);
                nowActive = true;
            }

            OnBreakpointToggled?.Invoke(line, nowActive);
            ConsoleLogUI.Log($"[Debugger] Breakpoint on Line {line:D2} {(nowActive ? "SET" : "REMOVED")}.");
            return nowActive;
        }

        public void Resume()
        {
            stepRequested = false;
            IsPaused = false;
            OnResumed?.Invoke();
        }

        public void Step()
        {
            stepRequested = true;
            IsPaused = false;
            OnResumed?.Invoke();
        }

        public IEnumerator CheckBreakpoint(int line, string lineDescription, Dictionary<string, string> locals)
        {
            if (!activeBreakpoints.Contains(line) && !stepRequested)
            {
                yield break;
            }

            IsPaused = true;
            stepRequested = false;

            ConsoleLogUI.Log($"<color=#E5C07B>[Debugger] Breakpoint Hit at Line {line:D2}: {lineDescription}</color>");
            OnBreakpointHit?.Invoke(line, lineDescription, locals);

            while (IsPaused)
            {
                yield return null;
            }
        }
    }
}
