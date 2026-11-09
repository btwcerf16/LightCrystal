using UnityEngine;
using UnityEngine.UI;

public sealed class RunGuideUI : MonoBehaviour
{
    [SerializeField] private Button _continueButton;
    private PlayerInputReader _input;

    private void Awake()
    {
        _continueButton.onClick.AddListener(Continue);
    }

    public void Show(PlayerInputReader input)
    {
        _input = input;
        _input.SetMenuOpen(true);
    }

    public void Continue()
    {
        _input.SetMenuOpen(false);
        Destroy(gameObject);
    }
}
