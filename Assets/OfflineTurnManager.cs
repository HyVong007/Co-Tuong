using Cysharp.Threading.Tasks;


namespace CoTuong
{
	public sealed class OfflineTurnManager : TurnManager
	{
		public int humanPlayerID;
		public bool IsHumanPlayer(int playerID) => playerID == humanPlayerID;


		private async void Start()
		{
			await UniTask.Yield();
			if (!this || !enabled) return;
			StartPlaying();
		}


		public void StartPlaying()
		{
			foreach (var listener in listeners) listener.OnTurnBegin();
		}


		public override async UniTask Play(IMoveData data)
		{
			foreach (var listener in listeners) await listener.OnPlayerMove(data, false);
			if (!this || !enabled) return;
			foreach (var listener in listeners) listener.OnTurnEnd();
			if (isGameEnd())
			{
				foreach (var listener in listeners) listener.OnGameEnd();
				Destroy(gameObject);
			}
			else await UniTask.NextFrame().ContinueWith(() =>
			{
				if (!this || !enabled) return;
				currentPlayerID = 1 - currentPlayerID;
				foreach (var listener in listeners) listener.OnTurnBegin();
			});
		}
	}
}
