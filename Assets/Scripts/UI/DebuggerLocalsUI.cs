using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CodeForge.UI
{
    public class DebuggerLocalsUI : MonoBehaviour
    {
        private static DebuggerLocalsUI instance;
        public static DebuggerLocalsUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DebuggerLocalsUI>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        [Header("UI Containers")]
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI codeLineText;
        [SerializeField] private TextMeshProUGUI localsListText;

        [Header("Action Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button stepButton;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(() =>
                {
                    BreakpointManager.Instance?.Resume();
                });
            }

            if (stepButton != null)
            {
                stepButton.onClick.AddListener(() =>
                {
                    BreakpointManager.Instance?.Step();
                });
            }

            if (BreakpointManager.Instance != null)
            {
                BreakpointManager.Instance.OnBreakpointHit += HandleBreakpointHit;
                BreakpointManager.Instance.OnResumed += HandleResumed;
            }

            if (rootContainer != null)
            {
                rootContainer.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (BreakpointManager.Instance != null)
            {
                BreakpointManager.Instance.OnBreakpointHit -= HandleBreakpointHit;
                BreakpointManager.Instance.OnResumed -= HandleResumed;
            }
        }

        private void Update()
        {
            if (rootContainer != null && rootContainer.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    BreakpointManager.Instance?.Resume();
                }
                else if (Input.GetKeyDown(KeyCode.F10))
                {
                    BreakpointManager.Instance?.Step();
                }
            }
        }

        private void HandleBreakpointHit(int line, string lineDescription, Dictionary<string, string> locals)
        {
            if (rootContainer != null)
            {
                rootContainer.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = $"<color=#E5C07B>● PAUSED AT BREAKPOINT</color> <color=#858585>|</color> Line {line:D2}";
            }

            if (codeLineText != null)
            {
                codeLineText.text = $"<color=#6A9955>// Execution Line {line:D2}</color>\n<color=#D4D4D4>{lineDescription}</color>";
            }

            if (localsListText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<b><color=#569CD6>Locals & Watch Expressions:</color></b>");
                if (locals != null && locals.Count > 0)
                {
                    foreach (var kvp in locals)
                    {
                        sb.AppendLine($"  <color=#9CDCFE>{kvp.Key}</color>: <color=#CE9178>{kvp.Value}</color>");
                    }
                }
                else
                {
                    sb.AppendLine("  <i><color=#858585>(No local variables in this scope)</color></i>");
                }
                localsListText.text = sb.ToString();
            }
        }

        private void HandleResumed()
        {
            if (rootContainer != null)
            {
                rootContainer.SetActive(false);
            }
        }
    }
}
