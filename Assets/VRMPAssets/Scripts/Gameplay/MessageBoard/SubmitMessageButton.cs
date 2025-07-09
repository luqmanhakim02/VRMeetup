using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

public class SubmitMessageButton : MonoBehaviour
{
    public Button submitButton;
    public TMPro.TMP_Text inputField;
    public NetworkMessageBoard messageBoard;

    void Start()
    {
        submitButton.onClick.AddListener(OnSubmitButtonClicked);
    }

    void OnSubmitButtonClicked()
    {
        string text = inputField.text;
        messageBoard.SubmitTextLocal(text);

        // Clear the text after submitting
        inputField.text = string.Empty;
    }
}
