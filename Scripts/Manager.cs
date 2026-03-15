
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ChuChuGimmicks.DocoCounter
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Manager : UdonSharpBehaviour
    {
        [SerializeField] private GameObject[] colliders;
        [SerializeField] private Board[] boards;

        private const float UPDATE_INTERVAL_SECONDS = 2.0f;
        private const int DELAY_FRAMES = 1;
        private const int MAX_COUNT = 160;
        private const float HEIGHT_OFFSET = 0.1f;

        VRCPlayerApi[] players = new VRCPlayerApi[MAX_COUNT];
        private int[] counts = null;
        private int totalCount = 0;
        private bool isPending = false;
        private int currentIndex = 0;




        private void OnEnable()
        {
            if (isPending) { return; }
            isPending = true;

            if (!Utilities.IsValid(colliders) || colliders.Length == 0) { return; }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (!Utilities.IsValid(colliders[i])) { continue; }
                if (!colliders[i].activeSelf) { continue; }

                colliders[i].SetActive(false);
            }

            InitializeBoard();
            StartCountCycle();
        }


        private void InitializeBoard()
        {
            for (int i = 0; i < boards.Length; i++)
            {
                if (!Utilities.IsValid(boards[i])) { continue; }
                boards[i].SetName(colliders);
            }
        }


        public void StartCountCycle()
        {
            if (!this.gameObject.activeInHierarchy)
            {
                isPending = false;
                return;
            }

            if (!Utilities.IsValid(counts))
            {
                counts = new int[colliders.Length];
            }

            // プレイヤー情報を更新
            VRCPlayerApi.GetPlayers(players);
            totalCount = VRCPlayerApi.GetPlayerCount();

            currentIndex = 0;

            ProcessNextCollider();
        }


        public void ProcessNextCollider()
        {
            if (!this.gameObject.activeInHierarchy)
            {
                isPending = false;
                return;
            }

            counts[currentIndex] = GetPlayerCountInCollider(currentIndex);
            currentIndex = (currentIndex + 1) % colliders.Length;

            if (currentIndex > 0)
            {
                SendCustomEventDelayedFrames(nameof(ProcessNextCollider), DELAY_FRAMES);
            }
            else
            {
                UpdateUI();
                SendCustomEventDelayedSeconds(nameof(StartCountCycle), UPDATE_INTERVAL_SECONDS);
            }
        }


        private int GetPlayerCountInCollider(int index)
        {
            if (!Utilities.IsValid(colliders[index])) { return -1; }

            int count = 0;
            Transform colliderTransform = colliders[index].transform;

            for (int j = 0; j < totalCount; j++)
            {
                if (!Utilities.IsValid(players[j]) || !players[j].IsValid()) { continue; }

                if (IsInCollider(colliderTransform, players[j]))
                {
                    if (players[j].isLocal)
                    {
                        count += 1001;
                    }
                    else
                    {
                        count++;
                    }
                }
            }

            return count;
        }


        private bool IsInCollider(Transform collider, VRCPlayerApi player)
        {
            Vector3 colliderPos    = collider.position;
            Quaternion colliderRot = collider.rotation;

            Vector3 colliderSize   = collider.localScale; // lossyScaleは使わず運用でカバー

            Vector3 playerPos = player.GetPosition();
            playerPos.y += HEIGHT_OFFSET;

            // ワールド空間のプレイヤー座標をコライダーのローカル空間に変換
            Vector3 localPlayerPos = Quaternion.Inverse(colliderRot) * (playerPos - colliderPos);

            // ローカル空間でAABBチェック（y座標は少し上を基準に）
            return Mathf.Abs(localPlayerPos.x) <= colliderSize.x / 2 &&
                   Mathf.Abs(localPlayerPos.y) <= colliderSize.y / 2 &&
                   Mathf.Abs(localPlayerPos.z) <= colliderSize.z / 2;
        }


        private void UpdateUI()
        {
            for (int i = 0; i < boards.Length; i++)
            {
                if (!Utilities.IsValid(boards[i])) { continue; }
                boards[i].UpdateUI(counts, totalCount);
            }
        }
    }
}
