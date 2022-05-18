using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace CoTuong
{
	public static class AI
	{
		private static readonly List<Vector2Int> myPieces = new(), opponentPieces = new(), pseudoMoves = new();
		private static readonly Dictionary<Vector2Int, List<Vector2Int>> from_checkMoves = new(), from_normalMoves = new();
		public static MoveData FindNextMove(Core core, Color myColor)
		{
			var rand = new System.Random(DateTime.Now.Millisecond);
			myPieces.Clear();
			opponentPieces.Clear();
			from_checkMoves.Clear();
			from_normalMoves.Clear();

			for (int x = 0; x < 9; ++x)
				for (int y = 0; y < 10; ++y)
					if (core[x, y] != null) (core[x, y].Value.color == myColor ? myPieces : opponentPieces).Add(new Vector2Int(x, y));

			var opponentColor = (Color)(1 - (int)myColor);
			while (myPieces.Count > 0)
			{
				int i = rand.Next(myPieces.Count);
				var from = myPieces[i];
				myPieces.RemoveAt(i);

				pseudoMoves.Clear();
				pseudoMoves.AddRange(core.FindPseudoLegalMoves(from));
				if (pseudoMoves.Count == 0) continue;

				while (pseudoMoves.Count > 0)
				{
					i = rand.Next(pseudoMoves.Count);
					var dest = pseudoMoves[i];
					pseudoMoves.RemoveAt(i);

					var data = new MoveData(core, from, dest);
					core.Move(data, undo: false, isPseudoMove: true);
					if (core.IsChecked(myColor))
					{
						core.Move(data, undo: true, isPseudoMove: true);
						continue;
					}

					for (i = 0; i < opponentPieces.Count; ++i) if (core.HasLegalMove(opponentPieces[i])) goto OPPONENT_HAS_LEGALMOVE;
					core.Move(data, undo: true, isPseudoMove: true);
					return data;

				OPPONENT_HAS_LEGALMOVE:
					core.Move(data, undo: true, isPseudoMove: true);
					var tmp = core.IsChecked(opponentColor) ? from_checkMoves : from_normalMoves;
					try { tmp[from].Add(dest); } catch { (tmp[from] = new()).Add(dest); }
				}
			}

			var dict = from_checkMoves.Count > 0 ? from_checkMoves : from_normalMoves;
			var kvp = dict.ElementAt(rand.Next(dict.Count));
			return new MoveData(core, kvp.Key, kvp.Value[rand.Next(kvp.Value.Count)]);
		}
	}
}