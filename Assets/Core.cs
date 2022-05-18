using System;
using System.Collections.Generic;
using UnityEngine;


namespace CoTuong
{
	public enum Color
	{
		Red = 0, Black = 1
	}



	public enum PieceName
	{
		General = 0,
		Advisor = 1,
		Elephant = 2,
		Horse = 3,
		Rook = 4,
		Cannon = 5,
		Pawn = 6
	}



	public readonly struct Piece
	{
		public readonly Color color;
		public readonly PieceName name;
		public readonly bool hidden;


		public Piece(Color color, PieceName name, bool hidden = false)
		{
#if DEBUG
			if (name == PieceName.General && hidden) throw new InvalidOperationException("Tướng không thể úp !");
#endif
			this.name = name;
			this.color = color;
			this.hidden = hidden;
		}


		public override string ToString() => $"({color} {name}{(hidden ? ", hidden" : "")})";
	}



	public readonly struct MoveData : IMoveData
	{
		public int playerID => (int)piece.color;
		public readonly Piece piece;
		public readonly Vector2Int from, dest;
		public readonly Piece? capturedPiece;


		public MoveData(Core core, Vector2Int from, Vector2Int dest)
		{
			piece = core[from].Value;
			this.from = from;
			this.dest = dest;
			capturedPiece = core[dest];
		}


		public override string ToString() => $"(piece= {piece} move {from} -> {dest}, capturedPiece= {capturedPiece})";
	}



	public sealed class Core
	{
		#region Khởi tạo
		private readonly Piece?[][] mailBox = new Piece?[9][];
		private static readonly Piece?[][] DEFAULT_MAILBOX =
		{
			
			/*FILE A*/ new Piece?[]{new (Color.Red, PieceName.Rook),null, null, new (Color.Red,PieceName.Pawn), null, null, new (Color.Black, PieceName.Pawn), null, null, new (Color.Black, PieceName.Rook) },
			/*FILE B*/ new Piece?[]{new ( Color.Red,PieceName.Horse ), null, new ( Color.Red,  PieceName.Cannon ), null, null, null, null, new( Color.Black,  PieceName.Cannon ), null, new( Color.Black,  PieceName.Horse ) },
			/*FILE C*/ new Piece?[]{new ( Color.Red,  PieceName.Elephant ), null, null, new ( Color.Red, PieceName.Pawn ), null, null, new( Color.Black,  PieceName.Pawn ), null, null, new( Color.Black, PieceName.Elephant ) },
			/*FILE D*/ new Piece?[]{new (Color.Red,  PieceName.Advisor ), null, null, null, null, null, null, null, null, new( Color.Black, PieceName.Advisor )},
			/*FILE E*/ new Piece?[]{new ( Color.Red,  PieceName.General ), null, null, new(Color.Red,  PieceName.Pawn ), null, null, new( Color.Black,  PieceName.Pawn ), null, null, new( Color.Black, PieceName.General )},
			/*FILE F*/ new Piece?[]{new ( Color.Red,  PieceName.Advisor ), null, null, null, null, null, null, null, null, new( Color.Black,  PieceName.Advisor ) },
			/*FILE G*/ new Piece?[]{new ( Color.Red, PieceName.Elephant ), null, null, new( Color.Red,  PieceName.Pawn ), null, null, new(Color.Black, PieceName.Pawn ), null, null, new( Color.Black, PieceName.Elephant ) },
			/*FILE H*/ new Piece?[]{new ( Color.Red, PieceName.Horse ), null, new( Color.Red,  PieceName.Cannon ), null, null, null, null, new( Color.Black, PieceName.Cannon ), null, new( Color.Black,  PieceName.Horse ) },
			/*FILE I*/ new Piece?[]{new  (Color.Red, PieceName.Rook),null, null, new( Color.Red, PieceName.Pawn), null, null, new( Color.Black, PieceName.Pawn), null, null, new( Color.Black, PieceName.Rook) },
		};
		/// <summary>
		/// if <see langword="true"/> : Chơi theo luật Cờ Úp
		/// </summary>
		private readonly bool hiddenChessRule;


		public Core(Piece?[][] param = null)
		{
			param ??= DEFAULT_MAILBOX;
			for (int x = 0; x < 9; ++x)
			{
				mailBox[x] = new Piece?[10];
				for (int y = 0; y < 10; ++y)
					if (param[x][y] != null)
					{
						var piece = (mailBox[x][y] = param[x][y]).Value;
						hiddenChessRule |= piece.hidden;
						if (piece.name == PieceName.General) generalCoords[piece.color] = new Vector2Int(x, y);
					}
			}
		}


		public static Piece?[][] CloneDefaultMailBox()
		{
			var result = new Piece?[9][];
			for (int x = 0; x < 9; ++x)
			{
				result[x] = new Piece?[10];
				for (int y = 0; y < 10; ++y) result[x][y] = DEFAULT_MAILBOX[x][y];
			}
			return result;
		}


		public Piece? this[int x, int y] => mailBox[x][y];
		public Piece? this[in Vector2Int coord] => mailBox[coord.x][coord.y];
		#endregion


		public enum State
		{
			Normal, Checked, CheckedMate, StaleMate
		}
		/// <summary>
		/// Trạng thái của người chơi tiếp theo
		/// </summary>
		public State state { get; private set; }
		private readonly Dictionary<Color, Vector2Int> generalCoords = new();
		public bool IsChecked(Color myColor)
		{
			var G = generalCoords[myColor];
			Vector2Int tmp;

			if (hiddenChessRule)
			{
				#region Kiểm tra Sĩ, Tượng của địch trong cờ Úp
				for (int i = 0; i < 4; ++i)
				{
					tmp = G + DIAG_VECTORS[i];
					if (!BOARD.Contains(tmp)) continue;
					var piece = this[tmp];
					if (piece == null)
					{
						if (!BOARD.Contains(tmp += DIAG_VECTORS[i])) continue;
						piece = this[tmp];
						if (piece?.name == PieceName.Elephant && piece?.color != myColor) return true;
					}
					else if (piece.Value.name == PieceName.Advisor && piece.Value.color != myColor) return true;
				}
				#endregion
			}

			#region Kiểm tra Tốt, Ngựa của địch
			var SPECIAL_PAWN = new Vector2Int(G.x, myColor == Color.Red ? G.y - 1 : G.y + 1); // Tốt địch không thể chiếu
			for (int i = 0; i < 4; ++i)
			{
				var (ortho, diags) = HORSE_VECTORS[i];
				tmp = G + ortho;
				if (tmp == SPECIAL_PAWN && !BOARD.Contains(tmp)) continue;

				var piece = this[tmp];
				if (piece != null)
				{
					// Nếu là Xe hoặc 1 trong 3 vị trí Tốt thì chiếu
					if (piece.Value.color != myColor &&
						(piece.Value.name == PieceName.Rook || (piece.Value.name == PieceName.Pawn && tmp != SPECIAL_PAWN))) return true;
				}
				else
				{
					// Kiểm tra Ngựa địch
					for (int d = 0; d < 2; ++d)
					{
						var diag = tmp + diags[d];
						if (!BOARD.Contains(diag)) continue;
						piece = this[diag];
						if (piece?.name == PieceName.Horse && piece.Value.color != myColor) return true;
					}
				}
			}

			#endregion

			#region Kiểm tra lộ mặt Tướng
			var OpponentG = generalCoords[(Color)(1 - (int)myColor)];
			tmp = G;
			if (tmp.x == OpponentG.x)
			{
				var m = mailBox[tmp.x];
				int DIR_Y = (myColor == Color.Red) ? 1 : -1;
				while (true)
				{
					if ((tmp.y += DIR_Y) == OpponentG.y) return true;
					if (m[tmp.y] != null) break;
				}
			}
			#endregion

			#region Kiểm tra Xe, Pháo của địch: xe, pháo phải đang ngửa
			tmp = G;
			for (tmp.x = 0; tmp.x < 9; ++tmp.x) if (IsChecked()) return true;
			tmp = G;
			for (tmp.y = 0; tmp.y < 10; ++tmp.y) if (IsChecked()) return true;


			bool IsChecked()
			{
				if (this[tmp] == null) return false;
				var piece = this[tmp].Value;
				if (!piece.hidden && piece.color != myColor && (piece.name == PieceName.Rook || piece.name == PieceName.Cannon)
				&& FindPseudoLegalMoves(tmp).Contains(G)) return true;
				return false;
			}
			#endregion

			return false;
		}


		#region FindPseudalLegalMoves
		public static readonly RectInt BOARD = new(0, 0, 9, 10);
		private static readonly IReadOnlyDictionary<Color, RectInt> SIDES = new Dictionary<Color, RectInt>
		{
			[Color.Red] = new(0, 0, 9, 5),
			[Color.Black] = new(0, 5, 9, 5)
		}, PALACES = new Dictionary<Color, RectInt>
		{
			[Color.Red] = new(3, 0, 3, 3),
			[Color.Black] = new(3, 7, 3, 3)
		};
		private static readonly Vector2Int[] ORTHO_VECTORS = { new(-1, 0), new(1, 0), new(0, 1), new(0, -1) },
			DIAG_VECTORS = { new(-1, -1), new(1, 1), new(-1, 1), new(1, -1) };
		private static readonly (Vector2Int ortho, Vector2Int[] diags)[] HORSE_VECTORS =
		{
			(ortho: new(-1, 0),    diags: new Vector2Int[]{new(-1, 1),  new(-1, -1) }),		// L
			(ortho: new(1, 0),     diags: new Vector2Int[]{new(1, 1),   new(1, -1)}),		// R
			(ortho: new(0, 1),     diags: new Vector2Int[]{new(-1, 1),  new(1, 1)}),		// U
			(ortho: new(0, -1),    diags: new Vector2Int[]{new(-1, -1), new(1, -1)})		// D
		};


		private readonly List<Vector2Int> pseudoList = new();
		public Vector2Int[] FindPseudoLegalMoves(in Vector2Int coord)
		{
			var piece = this[coord].Value;
			var SIDE = SIDES[piece.color];
			var PALACE = PALACES[piece.color];
			pseudoList.Clear();
			Vector2Int tmp;

			switch (piece.hidden ? DEFAULT_MAILBOX[coord.x][coord.y].Value.name : piece.name)
			{
				case PieceName.General:
					#region General
					for (int i = 0; i < 4; ++i)
					{
						tmp = coord + ORTHO_VECTORS[i];
						if (PALACE.Contains(tmp) && this[tmp]?.color != piece.color) pseudoList.Add(tmp);
					}
					break;
				#endregion

				case PieceName.Advisor:
					#region Advisor
					for (int i = 0; i < 4; ++i)
					{
						tmp = coord + DIAG_VECTORS[i];
						if ((
								(!piece.hidden && hiddenChessRule && BOARD.Contains(tmp))
								|| PALACE.Contains(tmp)
							)
							&& (this[tmp]?.color != piece.color)) pseudoList.Add(tmp);
					}
					break;
				#endregion

				case PieceName.Elephant:
					#region Elephant
					for (int i = 0; i < 4; ++i)
					{
						tmp = coord + DIAG_VECTORS[i];
						if ((!hiddenChessRule && !SIDE.Contains(tmp)) || (hiddenChessRule && !BOARD.Contains(tmp))
							|| this[tmp] != null) continue;

						tmp += DIAG_VECTORS[i];
						if (((!hiddenChessRule && SIDE.Contains(tmp)) || (hiddenChessRule && BOARD.Contains(tmp)))
							&& this[tmp]?.color != piece.color) pseudoList.Add(tmp);
					}
					break;
				#endregion

				case PieceName.Horse:
					#region Horse
					for (int i = 0; i < 4; ++i)
					{
						var (ortho, diags) = HORSE_VECTORS[i];
						tmp = coord + ortho;
						if (!BOARD.Contains(tmp) || this[tmp] != null) continue;

						for (int d = 0; d < 2; ++d)
						{
							var diag = tmp + diags[d];
							if (BOARD.Contains(diag) && this[diag]?.color != piece.color) pseudoList.Add(diag);
						}
					}
					break;
				#endregion

				case PieceName.Rook:
					#region Rook
					for (int i = 0; i < 4; ++i)
					{
						tmp = coord;
						while (BOARD.Contains(tmp += ORTHO_VECTORS[i]))
						{
							var c = this[tmp]?.color;
							if (c != piece.color) pseudoList.Add(tmp);
							if (c != null) break;
						}
					}
					break;
				#endregion

				case PieceName.Cannon:
					#region Cannon
					for (int i = 0; i < 4; ++i)
					{
						tmp = coord;
						while (BOARD.Contains(tmp += ORTHO_VECTORS[i]))
						{
							var c = this[tmp]?.color;
							if (c == null) { pseudoList.Add(tmp); continue; }

							while (BOARD.Contains(tmp += ORTHO_VECTORS[i]))
							{
								var c2 = this[tmp]?.color;
								if (c2 == null) continue;
								if (c2 != piece.color) pseudoList.Add(tmp);
								break;
							}
							break;
						}
					}
					break;
				#endregion

				case PieceName.Pawn:
					#region Pawn
					tmp = coord; tmp.y += (piece.color == Color.Red) ? 1 : -1;
					if (BOARD.Contains(tmp) && this[tmp]?.color != piece.color) pseudoList.Add(tmp);
					if (SIDE.Contains(coord)) break;
					for (int i = 0; i < 2; ++i)
					{
						tmp = coord + ORTHO_VECTORS[i];
						if (BOARD.Contains(tmp) && this[tmp]?.color != piece.color) pseudoList.Add(tmp);
					}
					break;
					#endregion
			}

			return pseudoList.ToArray();
		}
		#endregion


		private readonly List<Vector2Int> legalList = new();
		public Vector2Int[] FindLegalMoves(in Vector2Int coord)
		{
			var moves = FindPseudoLegalMoves(coord);
			if (moves.Length == 0) return Array.Empty<Vector2Int>();

			legalList.Clear();
			var myColor = this[coord].Value.color;
			for (int m = 0; m < moves.Length; ++m)
			{
				var data = new MoveData(this, coord, moves[m]);
				Move(data, undo: false, isPseudoMove: true);
				if (!IsChecked(myColor)) legalList.Add(moves[m]);
				Move(data, undo: true, isPseudoMove: true);
			}

			return legalList.ToArray();
		}


		public bool HasLegalMove(in Vector2Int coord)
		{
			var moves = FindPseudoLegalMoves(coord);
			if (moves.Length == 0) return false;

			var myColor = this[coord].Value.color;
			for (int m = 0; m < moves.Length; ++m)
			{
				var data = new MoveData(this, coord, moves[m]);
				Move(data, undo: false, isPseudoMove: true);
				bool check = IsChecked(myColor);
				Move(data, undo: true, isPseudoMove: true);
				if (!check) return true;
			}

			return false;
		}


		public void Move(in MoveData data, bool undo, bool isPseudoMove = false)
		{
			if (!undo)
			{
				mailBox[data.from.x][data.from.y] = null;
				mailBox[data.dest.x][data.dest.y] = new Piece(data.piece.color, data.piece.name);
				if (data.piece.name == PieceName.General) generalCoords[data.piece.color] = data.dest;
			}
			else
			{
				mailBox[data.from.x][data.from.y] = data.piece;
				mailBox[data.dest.x][data.dest.y] = data.capturedPiece;
				if (data.piece.name == PieceName.General) generalCoords[data.piece.color] = data.from;
			}
			if (isPseudoMove) return;

			#region Kiểm tra: chiếu, chiếu bí, hòa
			if (undo)
			{
				state = IsChecked(data.piece.color) ? State.Checked : State.Normal;
				return;
			}

			var opponentColor = (Color)(1 - data.playerID);
			bool opponentIsChecked = IsChecked(opponentColor);
			Vector2Int coord = default;
			for (coord.x = 0; coord.x < 9; ++coord.x)
				for (coord.y = 0; coord.y < 10; ++coord.y)
					if (this[coord]?.color == opponentColor && HasLegalMove(coord))
					{
						// Địch có nước đi hợp lệ
						state = opponentIsChecked ? State.Checked : State.Normal;
						return;
					}

			// Địch hết nước đi hợp lệ
			state = opponentIsChecked ? State.CheckedMate : State.StaleMate;
			#endregion
		}


		/// <summary>
		/// Sinh bàn cờ úp ngẫu nhiên
		/// </summary>
		public static Piece?[][] GenerateRandomHidden()
		{
			var color_names = new Dictionary<Color, List<PieceName>>
			{
				[Color.Red] = new List<PieceName> // Bug: có lỗi nếu bỏ "new List<PieceName>"
			{
				PieceName.Pawn,PieceName.Pawn,PieceName.Pawn,PieceName.Pawn,PieceName.Pawn,
				PieceName.Cannon, PieceName.Cannon,
				PieceName.Rook, PieceName.Rook,
				PieceName.Horse, PieceName.Horse,
				PieceName.Elephant, PieceName.Elephant,
				PieceName.Advisor, PieceName.Advisor
			}
			};

			color_names[Color.Black] = new(color_names[Color.Red]);
			var result = CloneDefaultMailBox();
			for (int x = 0; x < 9; ++x)
				for (int y = 0; y < 10; ++y)
				{
					if (result[x][y] != null && result[x][y].Value.name != PieceName.General)
					{
						var piece = result[x][y].Value;
						var names = color_names[piece.color];
						int index = UnityEngine.Random.Range(0, names.Count);
						result[x][y] = new(piece.color, names[index], true);
						names.RemoveAt(index);
					}
				}

			return result;
		}
	}
}