using Cysharp.Threading.Tasks;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace CoTuong
{
	public sealed class Board : MonoBehaviour, ITurnListener
	{
		[Serializable] private sealed class PieceName_PieceGUIs : SerializableDictionaryBase<PieceName, ObjectPool<PieceGUI>> { }
		[SerializeField] private SerializableDictionaryBase<Color, PieceName_PieceGUIs> pieces;
		[SerializeField] private Button back;
		private readonly PieceGUI[][] mailBox = new PieceGUI[9][];
		private Core core;
		private Button button;
		private void Awake()
		{
			core = GameManager.hiddenChess ? new(Core.GenerateRandomHidden()) : new();
			for (int x = 0; x < 9; ++x)
			{
				mailBox[x] = new PieceGUI[10];
				for (int y = 0; y < 10; ++y)
					if (core[x, y] != null)
					{
						var p = core[x, y].Value;
						(mailBox[x][y] = pieces[p.color][p.name].Get(new Vector3(x, y))).Hide(p.hidden);
					}
			}
			(button = GetComponent<Button>()).beginDrag += BeginDrag;
			back.click += async _ =>
			{
				await SceneManager.UnloadSceneAsync("Board");
				GameManager.instance.gameObject.SetActive(true);
			};
		}


		private void Start()
		{
			TurnManager.instance.AddListener(this);
			TurnManager.instance.isGameEnd += () => core.state == Core.State.CheckedMate || core.state == Core.State.StaleMate;
		}


		public async void OnTurnBegin()
		{
			if (!(button.interactable = TurnManager.instance.CurrentPlayerIsLocalHuman()))
			{
				MoveData data = default;
				await UniTask.RunOnThreadPool(() =>
				{
					data = AI.FindNextMove(core, (Color)TurnManager.instance.currentPlayerID);
				});
				if (!this || !enabled) return;
				TurnManager.instance.Play(data);
			}
		}


		public void OnTurnEnd()
		{
		}


		[SerializeField] private Transform cellFlag;
		[SerializeField] private ObjectPool<Transform> hintPool;
		private bool BeginDrag(Vector2 pixel)
		{
			var from = Convert(pixel);
			if (!Core.BOARD.Contains(from) || core[from] == null) return false;

			var moves = core.FindLegalMoves(from);
			if (moves.Length == 0) return false;

			// Tô màu các ô có thể đi
			foreach (var move in moves) hintPool.Get(move.ToVector3());
			var t = TurnManager.instance;
			if (!t.CurrentPlayerIsLocalHuman() || (int)core[from].Value.color != t.currentPlayerID)
			{
				button.endDrag += _;
				return true;

				void _(Vector2 __)
				{
					hintPool.Recycle();
					button.endDrag -= _;
				}
			}

			var piece = mailBox[from.x][from.y];
			++piece.spriteRenderer.sortingOrder;
			button.dragging += dragging;
			button.endDrag += endDrag;
			return true;


			bool dragging(Vector2 _)
			{
				var pos = Camera.main.ScreenToWorldPoint(_);
				pos.z = 0;
				var f = Vector3Int.FloorToInt(pos);
				if (0 <= f.x && f.x < 9 && 0 <= f.y && f.y < 10) if (cellFlag) cellFlag.position = f;
				pos.x -= 0.5f; pos.y -= 0.5f;
				if (piece) piece.transform.position = pos;
				return true;
			}


			async void endDrag(Vector2 _)
			{
				hintPool.Recycle();
				button.dragging -= dragging;
				button.endDrag -= endDrag;
				cellFlag.position = new Vector3(0, -1.5f);
				var dest = Convert(_);
				button.interactable = false;
				if (moves.Contains(dest)) await t.Play(new MoveData(core, from, dest));
				else
				{
					await piece.transform.Move(from.ToVector3(), 0.45f);
					if (!this || !enabled) return;
					button.interactable = true;
				}

				if (!this || !enabled) return;
				--piece.spriteRenderer.sortingOrder;
			}


			Vector2Int Convert(Vector2 _) => Vector2Int.FloorToInt(Camera.main.ScreenToWorldPoint(_));
		}


		[SerializeField] private Transform moveTarget;
		[SerializeField] private float pieceMoveSpeed;
		public async UniTask OnPlayerMove(IMoveData moveData, bool undo)
		{
			var data = (MoveData)moveData;
			core.Move(data, undo);

			if (!undo)
			{
				#region PLAY
				var piece = mailBox[data.from.x][data.from.y];
				mailBox[data.from.x][data.from.y] = null;
				++piece.spriteRenderer.sortingOrder;

				// Nếu là AI hoặc Remote thì tô màu đích đến trước khi di chuyển quân cờ
				if (!TurnManager.instance.CurrentPlayerIsLocalHuman()) moveTarget.position = data.dest.ToVector3();

				await piece.transform.Move(data.dest.ToVector3(), pieceMoveSpeed);
				if (!this || !enabled) return;
				--piece.spriteRenderer.sortingOrder;

				if (data.capturedPiece != null)
				{
					var p = mailBox[data.dest.x][data.dest.y];
					if (data.capturedPiece.Value.hidden)
					{
						p.Hide(false);
						await p.transform.Move(new Vector3(-3, 4.5f), 0.3f);
						if (!this || !enabled) return;
						p.transform.localScale = new Vector3(3, 3, 1);
						await UniTask.Delay(1000);
						if (!this || !enabled) return;
						p.transform.localScale = Vector3.one;
					}
					pieces[p.color][p.name].Recycle(p);
				}

				(mailBox[data.dest.x][data.dest.y] = piece).Hide(false);
				moveTarget.position = data.dest.ToVector3();
				#endregion
			}
			else
			{
				#region UNDO
				var piece = mailBox[data.dest.x][data.dest.y];
				if (data.capturedPiece != null)
				{
					var opponent = data.capturedPiece.Value;
					(mailBox[data.dest.x][data.dest.y] = pieces[opponent.color][opponent.name].Get(data.dest.ToVector3()))
						.Hide(opponent.hidden);
				}
				else mailBox[data.dest.x][data.dest.y] = null;

				await piece.transform.Move(data.from.ToVector3(), pieceMoveSpeed);
				if (!this || !enabled) return;
				(mailBox[data.from.x][data.from.y] = piece).Hide(core[data.from.x, data.from.y].Value.hidden);
				moveTarget.position = data.from.ToVector3();
				#endregion
			}
		}


		[SerializeField] private Text text;
		public void OnGameEnd()
		{
			text.text = core.state == Core.State.StaleMate ? "VÁN CỜ HÒA !"
				: TurnManager.instance.currentPlayerID == 0 ? "ĐỎ THẮNG !" : "ĐEN THẮNG !";
		}
	}
}