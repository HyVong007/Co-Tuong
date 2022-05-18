using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;


namespace CoTuong
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class PieceGUI : MonoBehaviour
	{
		[field: SerializeField] public Color color { get; private set; }
		[field: SerializeField] public new PieceName name { get; private set; }
		public SpriteRenderer spriteRenderer { get; private set; }


		[ShowAssetPreview][SerializeField] private Sprite hiddenSprite;
		public bool Hide(bool value) => spriteRenderer.sprite = value ? hiddenSprite : isSymbol ? symbolSprite : normalSprite;


		[ShowAssetPreview][SerializeField] private Sprite symbolSprite;
		private Sprite normalSprite;
		private static readonly List<PieceGUI> pieces = new();
		private static bool isSymbol;
		public static void ShowSymbol(bool value)
		{
			isSymbol = value;
			foreach (var piece in pieces)
				piece.spriteRenderer.sprite = (piece.spriteRenderer.sprite == piece.hiddenSprite) ? piece.hiddenSprite
					: value ? piece.symbolSprite : piece.normalSprite;
		}


		private void Awake()
		{
			normalSprite = (spriteRenderer = GetComponent<SpriteRenderer>()).sprite;
			spriteRenderer.sprite = isSymbol ? symbolSprite : normalSprite;
			pieces.Add(this);
		}


		private void OnDestroy() => pieces.Remove(this);


		public override string ToString() => $"({color }, {name}, hidden= {spriteRenderer.sprite == hiddenSprite}, isSymbol= {isSymbol})";
	}
}