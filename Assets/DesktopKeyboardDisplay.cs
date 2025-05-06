using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace XRMultiplayer
{
    /// <summary>
    /// Utility class to handle text input using a Desktop keyboard.
    /// </summary>
    public class DesktopKeyboardDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("Input field linked to this display.")]
        TMP_InputField m_InputField;

        [SerializeField, Tooltip("Message Board for submitting the text.")]
        NetworkMessageBoard m_MessageBoard;

        [SerializeField, Tooltip("If true, this display will update with each key press.")]
        bool m_UpdateOnKeyPress = true;

        [SerializeField, Tooltip("If true, this display will clear the input field text on text submit.")]
        public bool m_ClearTextOnSubmit;

        /// <summary>
        /// Detects Enter key press for text submission.
        /// </summary>
        void Update()
        {
            // Check for Enter key press (Desktop mode)
            if (Input.GetKeyDown(KeyCode.Return)) // Enter key detection
            {
                SubmitText();
            }
        }

        /// <summary>
        /// Method to submit the text from TMP_InputField when Enter is pressed.
        /// </summary>
        void SubmitText()
        {
            // Ensure there is text to submit
            if (!string.IsNullOrEmpty(m_InputField.text))
            {
                // Submit the message to the NetworkMessageBoard
                m_MessageBoard.SubmitTextLocal(m_InputField.text);

                // Optionally, clear the input field after submission
                if (m_ClearTextOnSubmit)
                {
                    m_InputField.text = "";
                }
            }
        }

        /// <summary>
        /// Optional method to handle key press behavior if you need to update on each key press.
        /// </summary>
        void OnTextUpdate()
        {
            if (!m_UpdateOnKeyPress) return;

            // Here you can handle updates to the text as each key is pressed
            // For example, you could limit the character length or monitor special characters
        }

        // You can add any additional behavior you want to trigger on text submit, like resetting the input field or updating UI elements.
    }
}
