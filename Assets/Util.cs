using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;


namespace CoTuong
{
	public static class Util
	{
		public static bool Contains<T>(this T[] array, T item)
		{
			for (int i = 0; i < array.Length; ++i) if (array[i].Equals(item)) return true;
			return false;
		}


		#region Converts
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3Int ToVector3Int(this in Vector2Int value) => new(value.x, value.y, 0);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2Int ToVector2Int(this in Vector3Int value) => new(value.x, value.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 ToVector2(this in Vector3Int value) => new(value.x, value.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ToVector3(this in Vector2Int value) => new(value.x, value.y);


#if !DEBUG
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static Vector2Int ToVector2Int(this in Vector3 value) =>
#if DEBUG
				value.x < 0 || value.y < 0 ? throw new IndexOutOfRangeException($"value= {value} phải là tọa độ không âm !") :
#endif
			new((int)value.x, (int)value.y);

		/// <summary>
		/// z = 0
		/// </summary>
#if !DEBUG
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static Vector3Int ToVector3Int(this in Vector3 value) =>
#if DEBUG
				value.x < 0 || value.y < 0 ? throw new IndexOutOfRangeException($"value= {value} phải là tọa độ không âm !") :
#endif
			new((int)value.x, (int)value.y, 0);
		#endregion


		public static async UniTask Move(this Transform transform, Vector3 dest, float speed, CancellationToken token = default)
		{
			if (token.IsCancellationRequested) return;
			while (!token.IsCancellationRequested && transform && transform.gameObject.activeSelf && transform.position != dest)
			{
				transform.position = Vector3.MoveTowards(transform.position, dest, speed);
				await UniTask.Yield();
			}
			if (!token.IsCancellationRequested && transform) transform.position = dest;
		}


		public static bool CurrentPlayerIsLocalHuman(this TurnManager t) => (t as OfflineTurnManager).IsHumanPlayer(t.currentPlayerID);
	}



	[Serializable]
	public sealed class ObjectPool<T> : IEnumerable<T> where T : Component
	{
		[SerializeField] private T prefab;
		[SerializeField] private Transform usingAnchor, freeAnchor;
		[SerializeField] private List<T> free = new();
		private readonly List<T> @using = new();


		private ObjectPool() { }


		public ObjectPool(T prefab, Transform freeAnchor = null, Transform usingAnchor = null)
		{
			this.prefab = prefab;
			this.freeAnchor = freeAnchor;
			this.usingAnchor = usingAnchor;
		}


		public T Get(Vector3 position = default, bool active = true)
		{
			T item;
			if (free.Count != 0)
			{
				item = free[0];
				free.RemoveAt(0);
			}
			else item = UnityEngine.Object.Instantiate(prefab);

			item.transform.parent = usingAnchor;
			@using.Add(item);
			item.transform.position = position;
			item.gameObject.SetActive(active);
			return item;
		}


		public void Recycle(T item)
		{
			item.gameObject.SetActive(false);
			item.transform.parent = freeAnchor;
			@using.Remove(item);
			free.Add(item);
		}


		public void Recycle()
		{
			for (int i = 0; i < @using.Count; ++i)
			{
				var item = @using[i];
				item.gameObject.SetActive(false);
				item.transform.parent = freeAnchor;
				free.Add(item);
			}
			@using.Clear();
		}


		public void DestroyGameObject(T item)
		{
			@using.Remove(item);
			UnityEngine.Object.Destroy(item.gameObject);
		}


		public void DestroyGameObject()
		{
			foreach (var item in @using) UnityEngine.Object.Destroy(item.gameObject);
			@using.Clear();
		}


		IEnumerator IEnumerable.GetEnumerator() => @using.GetEnumerator();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator() => (@using as IEnumerable<T>).GetEnumerator();
	}
}