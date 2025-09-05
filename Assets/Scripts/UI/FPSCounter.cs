using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public float updateInterval = 0.5f; // How often the FPS is updated
    private float accum = 0.0f;
    private int frames = 0;
    private float timeleft;

    // Use this for initialization
    void Start()
    {
        timeleft = updateInterval;
    }

    // Update is called once per frame
    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // Interval ended - update GUI text and start new interval
        if (timeleft <= 0.0f)
        {
            float fps = accum / frames;
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
            // Update the UI text with the calculated FPS
            // This example uses a Text component; replace with your UI element
            // Example: GetComponent<Text>().text = fps.ToString("F2") + " FPS";
        }
    }

    void OnGUI()
    {
        // Display the FPS on screen
        // Adjust the Rect position as needed
        GUI.Label(new Rect(5, 5, 100, 25), "FPS: " + (accum / frames).ToString("F2"), new GUIStyle());
    }
}