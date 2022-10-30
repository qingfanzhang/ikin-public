using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class NextScene : MonoBehaviour
{
    [SerializeField]
    private string sceneName;

    [SerializeField]
    private Text text;

    private Button button;

    private void Start()
    {
        if (string.IsNullOrEmpty(sceneName) || text == null)
        {
            return;
        }

        text.text = $"Go to Scene: {sceneName}";

        button = GetComponent<Button>();

        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        SceneManager.LoadScene(sceneName);
    }
}
