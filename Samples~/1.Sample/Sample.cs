using UnityEngine;

public class Sample : MonoBehaviour
{
    OllamaSharpUnity ollama;
    // Start is called before the first frame update
    void Start()
    {
        ollama = new OllamaSharpUnity("http://localhost:11434", "qwen2.5:1.5b");
        ollama.RequestAsync("讲个笑话");
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDestroy()
    {
        if (ollama != null)
        {
            ollama.Stop();
        }
    }
}