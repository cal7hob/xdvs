using System.Collections.Generic;
using UnityEngine;

public class InputWrapper : MonoBehaviour
{
    private class ButtonInteraction
    {
        private float lastInteractedTime;

        public int MultipleClicksCount { get; private set; }

        public void RegisterInteraction()
        {
            float currentTime = Time.time;
            float deltaTime = currentTime - lastInteractedTime;

            lastInteractedTime = currentTime;

            if (deltaTime <= MULTIPLE_CLICK_RATE)
                MultipleClicksCount++;
            else
                MultipleClicksCount = 1;
        }
    }

    private const float MULTIPLE_CLICK_RATE = 0.25f;

    private readonly Dictionary<string, ButtonInteraction> buttonInteractions = new Dictionary<string, ButtonInteraction>();

    private static InputWrapper instance;

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        instance = null;
    }

    public static bool GetButtonDown(string buttonName, int times)
    {
        if (!XDevs.Input.GetButtonDown(buttonName))
            return false;

        return instance.GetInteraction(buttonName).MultipleClicksCount >= times;
    }

    private ButtonInteraction GetInteraction(string buttonName)
    {
        ButtonInteraction result;

        if (!buttonInteractions.TryGetValue(buttonName, out result))
        {
            result = new ButtonInteraction();
            buttonInteractions.Add(buttonName, result);
        }

        result.RegisterInteraction();

        return result;
    }
}
