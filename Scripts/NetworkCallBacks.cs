using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

namespace AutoCodeSender
{
    public class NetworkCallBacks : MonoBehaviourPunCallbacks
    {
        public string lobbyCodePast = "";
        public int playerCountPast = 0;

        void Awake()
        {
            PhotonNetwork.AddCallbackTarget(this);
            StartCoroutine(WaitUntilPhotonInit());
        }

        void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        public override void OnJoinedRoom()
        {
            Room currentRoom = PhotonNetwork.CurrentRoom;

            if (currentRoom == null)
            {
                Debug.LogWarning("No current room found after joining!");
            }
            else
            {
                string lobbyCode = currentRoom.Name;
                int playerCount = currentRoom.PlayerCount;

                lobbyCodePast = lobbyCode;
                playerCountPast = playerCount;

                Debug.Log($"Joined room: {lobbyCode} with {playerCount} players");

                if (Plugin.Instance != null)
                {
                    Plugin.Instance.OnLobbyJoin(lobbyCode, playerCount);
                }
            }
        }

        public override void OnLeftRoom()
        {
            Room currentRoom = PhotonNetwork.CurrentRoom;

            if (currentRoom == null)
            {
                Plugin.Instance.OnLobbyLeft(lobbyCodePast, playerCountPast);
                Debug.LogWarning("No current room found after leaving!"); // This is obviously gonna happen.
            }
            else
            {
                string lobbyCode = currentRoom.Name;
                int playerCount = currentRoom.PlayerCount;

                Debug.Log($"Left room: {lobbyCode} with {playerCount} remaining players");

                if (Plugin.Instance != null)
                {
                    Plugin.Instance.OnLobbyLeft(lobbyCode, playerCount);
                }
            }
        }

        public IEnumerator WaitUntilPhotonInit()
        {
            while (!PhotonNetwork.IsConnectedAndReady)
            {
                yield return null; 
            }

            if (Plugin.Instance != null && Plugin.Instance.Enabled)
            {
                Debug.Log("Photon is initialized. Sending game start message.");
                Plugin.SendMessage(Plugin.Instance.webhookUrl, $"{PhotonNetwork.NickName} has started their game!");
            }
        }
    }
}
