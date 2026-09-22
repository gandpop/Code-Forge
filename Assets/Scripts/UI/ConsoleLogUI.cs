using System.Text;
using UnityEngine;
using TMPro;

namespace CodeForge.UI
{
    public class ConsoleLogUI : MonoBehaviour
    {
        private static ConsoleLogUI instance;
        [SerializeField] private TextMeshProUGUI logTextDisplay;
        private StringBuilder logHistory = new StringBuilder();
        private const int MaxLogLines = 25;
        private int currentLineCount = 0;

        private void Awake()
        {
            instance = this;
        }

        public static void Log(string message)
        {
            if (instance == null) return;
            instance.AppendMessage(message);
        }

        private void AppendMessage(string message)
        {
            if (currentLineCount >= MaxLogLines)
            {
                logHistory.Clear();
                currentLineCount = 0;
            }

            logHistory.AppendLine(message);
            currentLineCount++;
            if (logTextDisplay != null)
            {
                logTextDisplay.text = logHistory.ToString();
            }
        }
    }
}
