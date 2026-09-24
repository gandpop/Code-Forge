using System;
using UnityEngine;

namespace CodeForge.Combat
{
    public class RoomProgressionManager : MonoBehaviour
    {
        public static RoomProgressionManager Instance { get; private set; }

        [Header("Progression State")]
        [SerializeField] private int currentRoomIndex = 1;
        public int CurrentRoomIndex => currentRoomIndex;

        public bool IsClassFieldsUnlocked => currentRoomIndex >= 2;
        public bool IsOnTakeDamageUnlocked => currentRoomIndex >= 3;
        public bool IsFullRoguelikeUnlocked => currentRoomIndex >= 4;

        public event Action<int> OnRoomChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        public void SetRoom(int room)
        {
            currentRoomIndex = Mathf.Max(1, room);
            OnRoomChanged?.Invoke(currentRoomIndex);
        }

        public void AdvanceRoom()
        {
            SetRoom(currentRoomIndex + 1);
        }

        public void ResetProgression()
        {
            SetRoom(1);
        }
    }
}
