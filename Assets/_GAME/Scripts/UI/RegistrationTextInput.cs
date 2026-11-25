using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

// uGUI InputField uses BaseInput for IME even when the project uses the new Input System.
[RequireComponent(typeof(InputSystemUIInputModule))]
public sealed class RegistrationTextInput : BaseInput
{
    private InputSystemUIInputModule inputModule;
    private Keyboard keyboard;
    private IMECompositionMode mode;
    private string composition = string.Empty;

    public override string compositionString
    {
        get
        {
            RefreshKeyboard();
            return composition;
        }
    }

    public override IMECompositionMode imeCompositionMode
    {
        get => mode;
        set
        {
            RefreshKeyboard();
            mode = value;
            keyboard?.SetIMEEnabled(mode == IMECompositionMode.On);
            if (mode != IMECompositionMode.On)
                composition = string.Empty;
        }
    }

    public override Vector2 compositionCursorPos
    {
        get => Vector2.zero;
        set
        {
            RefreshKeyboard();
            keyboard?.SetIMECursorPosition(value);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        inputModule = GetComponent<InputSystemUIInputModule>();
        inputModule.inputOverride = this;
    }

    protected override void OnDisable()
    {
        if (inputModule && inputModule.inputOverride == this)
            inputModule.inputOverride = null;

        DetachKeyboard();
        mode = IMECompositionMode.Auto;
        base.OnDisable();
    }

    private void RefreshKeyboard()
    {
        var current = Keyboard.current;
        if (keyboard == current)
            return;

        DetachKeyboard();
        keyboard = current;
        if (keyboard == null)
            return;

        keyboard.onIMECompositionChange += UpdateComposition;
        if (mode == IMECompositionMode.On)
            keyboard.SetIMEEnabled(true);
    }

    private void DetachKeyboard()
    {
        if (keyboard != null)
        {
            keyboard.onIMECompositionChange -= UpdateComposition;
            if (keyboard.added && mode == IMECompositionMode.On)
                keyboard.SetIMEEnabled(false);
            keyboard = null;
        }

        composition = string.Empty;
    }

    private void UpdateComposition(IMECompositionString value)
    {
        composition = value.ToString();
    }
}
