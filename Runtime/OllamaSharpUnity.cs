using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OllamaSharp;
using OllamaSharp.Models;

public class OllamaSharpUnity
{
    OllamaApiClient ollama;
    Action<string> onWord;
    Action<string> onSentence;

    public OllamaSharpUnity(string url, string model, Action<string> onWord = null, Action<string> onSentence = null)
    {
        onWord = this.onWord;
        onSentence = this.onSentence;
        var uri = new Uri(url);
        ollama = new OllamaApiClient(uri, model);
    }

    // 实时句子缓冲区
    StringBuilder sentenceBuffer = new StringBuilder();
    // 句子结束符正则（支持中英文标点）
    Regex sentenceDelimiters = new Regex(@"[。！？.!?](\s|$)|[。！？.!?][”’](\s|$)");

    CancellationTokenSource cts;

    public async void RequestAsync(string prompt)
    {
        cts = new CancellationTokenSource();
        GenerateRequest gr = new GenerateRequest();
        gr.Prompt = prompt;
        gr.Stream = true;
        var resp = ollama.GenerateAsync(gr, cts.Token);
        try
        {
            await foreach (GenerateResponseStream? stream in resp)
            {
                if (stream != null)
                {
                    if (onWord != null)
                    {
                        onWord(stream.Response);
                    }
                    UnityEngine.Debug.Log("模型回答:" + stream.Response);

                    // 追加新内容到缓冲区
                    sentenceBuffer.Append(stream.Response);

                    // 实时处理缓冲区
                    ProcessBuffer(ref sentenceBuffer);

                    // 如果已结束
                    if (stream.Done)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    void ProcessBuffer(ref StringBuilder buffer)
    {
        var content = buffer.ToString();
        var lastIndex = 0;

        // 查找所有完整句子
        var matches = sentenceDelimiters.Matches(content);
        foreach (Match match in matches)
        {
            // 截取到标点符号的位置
            var endPos = match.Index + match.Length;
            var sentence = content.Substring(lastIndex, endPos - lastIndex).Trim();

            if (!string.IsNullOrEmpty(sentence))
            {
                // 触发句子处理
                if (onSentence != null)
                {
                    onSentence(sentence);
                }
                UnityEngine.Debug.Log($"模型回答: {sentence}");
            }

            lastIndex = endPos;
        }

        // 保留未完成部分
        buffer = new StringBuilder(content.Substring(lastIndex));
    }

    /// <summary>
    /// 打断生成过程
    /// </summary>
    public void Interrupt()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts = null;
        }
    }

    public void Stop()
    {
        if (ollama != null)
        {
            ollama.Dispose();
            ollama = null;
        }
    }
}