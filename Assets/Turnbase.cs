using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace CoTuong
{
	public interface IMoveData
	{
		public int playerID { get; }
	}



	/// <summary>
	/// Lưu lại lịch sử các nước đi và cho phép Undo/ Redo.<br/>
	/// Trạng thái bàn chơi chỉ được thay đổi thông qua <see cref="Play(IMoveData)"/>, <see cref="Undo(int)"/> và <see cref="Redo(int)"/>
	/// </summary>
	public sealed class History
	{
		/// <summary>
		/// Số lượng nước đi liên tục (Play) tối đa có thể lưu lại. > 0
		/// </summary>
		public const ushort CAPACITY = ushort.MaxValue;
		private readonly List<IMoveData> recentMoves = new();
		private readonly List<IMoveData[]> undoneMoves = new();
		/// <summary>
		/// Số lượng nước đã đi (Play/Redo).
		/// </summary>
		public int moveCount => recentMoves.Count;
		public IMoveData this[int index] => recentMoves[index];
		public enum Mode
		{
			Play, Undo, Redo
		}
		/// <summary>
		/// Thực thi 1 nước đi (Play/Undo/Redo)<para/>
		/// Chú ý: không nên sử dụng <see cref="History"/> trong event vì trạng thái <see cref="History"/> đang không hợp lệ !
		/// </summary>
		public event Action<IMoveData, Mode> execute;


		public History() { }


		public History(History history)
		{
			recentMoves.AddRange(history.recentMoves);
			undoneMoves.AddRange(history.undoneMoves);
			for (int i = 0; i < undoneMoves.Count; ++i)
			{
				var moves = undoneMoves[i];
				Array.Copy(moves, undoneMoves[i] = new IMoveData[moves.Length], moves.Length);
			}
		}


		public void Play(IMoveData data)
		{
			undoneMoves.Clear();
			if (recentMoves.Count == CAPACITY) recentMoves.RemoveAt(0);
			recentMoves.Add(data);
			execute(data, Mode.Play);
		}


		public bool CanUndo(int playerID)
		{
			for (int i = recentMoves.Count - 1; i >= 0; --i) if (recentMoves[i].playerID == playerID) return true;
			return false;
		}


		private readonly List<IMoveData> tmpMoves = new();
		public void Undo(int playerID)
		{
			tmpMoves.Clear();
			int tmpID;

			do
			{
				var move = recentMoves[recentMoves.Count - 1];
				recentMoves.RemoveAt(recentMoves.Count - 1);
				tmpMoves.Add(move);
				execute(move, Mode.Undo);
				tmpID = move.playerID;
			} while (tmpID != playerID);
			undoneMoves.Add(tmpMoves.ToArray());
		}


		public bool CanRedo(int playerID)
		{
			for (int i = undoneMoves.Count - 1; i >= 0; --i)
			{
				var moves = undoneMoves[i];
				if (moves[moves.Length - 1].playerID == playerID) return true;
			}
			return false;
		}


		public void Redo(int playerID)
		{
			int tmpID;

			do
			{
				var moves = undoneMoves[undoneMoves.Count - 1];
				undoneMoves.RemoveAt(undoneMoves.Count - 1);
				for (int i = moves.Length - 1; i >= 0; --i)
				{
					var move = moves[i];
					execute(move, Mode.Redo);
					recentMoves.Add(move);
				}

				tmpID = moves[moves.Length - 1].playerID;
			} while (tmpID != playerID);
		}
	}


	public interface ITurnListener
	{
		void OnTurnBegin();

		void OnTurnEnd();

		UniTask OnPlayerMove(IMoveData data, bool undo);

		void OnGameEnd();
	}



	public abstract class TurnManager : MonoBehaviour
	{
		public static TurnManager instance { get; private set; }
		protected void Awake()
		{
			instance = this;
		}


		public int currentPlayerID { get; protected set; }
		protected readonly List<ITurnListener> listeners = new();
		public void AddListener(ITurnListener listener) => listeners.Add(listener);


		public Func<bool> isGameEnd;


		public abstract UniTask Play(IMoveData data);
	}
}