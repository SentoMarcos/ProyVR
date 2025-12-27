using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputSystemDebug : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool logKeyPresses = true;
    public bool logActions = false;

    void Start()
    {
        Debug.Log("=== INPUT SYSTEM DEBUG INICIADO ===");
        Debug.Log($"Input System disponible: {InputSystem.devices.Count > 0}");

        // Listar dispositivos
        foreach (var device in InputSystem.devices)
        {
            Debug.Log($"Dispositivo: {device.name} ({device.GetType().Name})");
        }
    }

    void Update()
    {
        if (!logKeyPresses) return;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            CheckKey(keyboard.wKey, "W");
            CheckKey(keyboard.sKey, "S");
            CheckKey(keyboard.aKey, "A");
            CheckKey(keyboard.dKey, "D");
            CheckKey(keyboard.leftShiftKey, "Shift");
            CheckKey(keyboard.leftCtrlKey, "Ctrl");
            CheckKey(keyboard.rKey, "R");
            CheckKey(keyboard.escapeKey, "ESC");
        }
    }

    void CheckKey(KeyControl key, string keyName)
    {
        if (key.wasPressedThisFrame)
        {
            Debug.Log($"Tecla presionada: {keyName}");
        }
    }

    void OnGUI()
    {
        // Mostrar estado actual
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.green;

        string status = "=== INPUT DEBUG ===\n";
        status += $"Cursor Locked: {Cursor.lockState == CursorLockMode.Locked}\n";
        status += $"Devices: {InputSystem.devices.Count}\n";

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            status += $"Keyboard: {keyboard.name}\n";
            status += $"W pressed: {keyboard.wKey.isPressed}\n";
            status += $"Mouse Delta: {Mouse.current?.delta.ReadValue()}\n";
        }

        GUI.Label(new Rect(10, 10, 300, 200), status, style);
    }
}