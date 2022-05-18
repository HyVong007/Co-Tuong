using UnityEngine;
using UnityEngine.SceneManagement;


namespace CoTuong
{
	public sealed class GameManager : MonoBehaviour
	{
		[SerializeField] private Button buttonCoTuong, buttonCoUp, buttonExit;
		public static bool hiddenChess { get; private set; }


		public static GameManager instance { get; private set; }
		private void Awake()
		{
			instance = this;
			buttonCoTuong.click += _ =>
			  {
				  hiddenChess = false;
				  gameObject.SetActive(false);
				  SceneManager.LoadScene("Board", LoadSceneMode.Additive);
			  };

			buttonCoUp.click += _ =>
			  {
				  hiddenChess = true;
				  gameObject.SetActive(false);
				  SceneManager.LoadScene("Board", LoadSceneMode.Additive);
			  };

			buttonExit.click += _ => Application.Quit();
		}
	}
}