using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class WhiteboardEraser : NetworkBehaviour
{
    [SerializeField] private Transform _tip;  // The eraser tip
    [SerializeField] private int _eraserSize = 30;  // Size of the eraser

    private Renderer _renderer;
    private Color[] _eraseColor;     // Color to reset the texture (background color of the whiteboard)
    private float _tipHeight;

    private RaycastHit _touch;
    private WhiteBoard _whiteboard;  // Reference to the whiteboard to access its texture
    private Vector2 _touchPos, _lastTouchPos;
    private bool _touchedLastFrame;
    private Quaternion _lastTouchRot;

    void Start()
    {
        // Dynamically find the whiteboard in the scene
        _whiteboard = FindFirstObjectByType<WhiteBoard>(); // Finds the first instance of WhiteBoard in the scene

        if (_whiteboard != null)
        {
            // Sample the background color from the whiteboard's texture
            _eraseColor = Enumerable.Repeat(SampleBackgroundColor(), _eraserSize * _eraserSize).ToArray();
        }
        else
        {
            Debug.LogError("Whiteboard not found in the scene!");
        }

        _tipHeight = _tip.localScale.y;
    }

    void Update()
    {
        Erase();
    }

    private void Erase()
    {
        if (Physics.Raycast(_tip.position, transform.up, out _touch, _tipHeight))
        {
            if (_touch.transform.CompareTag("WhiteBoard"))
            {
                if (_whiteboard == null)
                {
                    _whiteboard = _touch.transform.GetComponent<WhiteBoard>();
                }

                _touchPos = new Vector2(_touch.textureCoord.x, _touch.textureCoord.y);

                var x = (int)(_touchPos.x * _whiteboard.textureSize.x - (_eraserSize / 2));
                var y = (int)(_touchPos.y * _whiteboard.textureSize.y - (_eraserSize / 2));

                if (y < 0 || y > _whiteboard.textureSize.y || x < 0 || x > _whiteboard.textureSize.x) return;

                if (_touchedLastFrame)
                {
                    // Reset the area to the background color (whiteboard's background color)
                    _whiteboard.texture.SetPixels(x, y, _eraserSize, _eraserSize, _eraseColor);

                    // Interpolate to smoothly erase the drawn path
                    for (float f = 0.01f; f < 1.00f; f += 0.01f)
                    {
                        var lerpX = (int)Mathf.Lerp(_lastTouchPos.x, x, f);
                        var lerpY = (int)Mathf.Lerp(_lastTouchPos.y, y, f);
                        _whiteboard.texture.SetPixels(lerpX, lerpY, _eraserSize, _eraserSize, _eraseColor);
                    }

                    // Maintain the eraser's orientation
                    transform.rotation = _lastTouchRot;

                    // Apply the changes to the texture
                    _whiteboard.texture.Apply();
                }

                // Store the current touch position for interpolation
                _lastTouchPos = new Vector2(x, y);
                _lastTouchRot = transform.rotation;
                _touchedLastFrame = true;
                return;
            }
        }

        _whiteboard = null;
        _touchedLastFrame = false;
    }

    // Sample a pixel color from the whiteboard texture to match its background
    private Color SampleBackgroundColor()
    {
        if (_whiteboard != null && _whiteboard.texture != null)
        {
            // Sample from the top-left corner or any other part of the texture you wish
            return _whiteboard.texture.GetPixel(0, 0); // Assuming the background color is at (0, 0)
        }

        return Color.white; // Default to white if something goes wrong
    }
}
