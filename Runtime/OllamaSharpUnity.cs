using OllamaSharp;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

public class OllamaSharpUnity
{
    OllamaApiClient ollama;
    List<Message> chatHistory;
    Action<string> onWord;
    Action<string> onSentence;
    string modelName;

    public OllamaSharpUnity(string url, string model, Action<string> onWord = null, Action<string> onSentence = null)
    {
        this.onWord = onWord;
        this.onSentence = onSentence;
        this.modelName = model;
        var uri = new Uri(url);
        ollama = new OllamaApiClient(uri);
        chatHistory = new List<Message>();

        // 可选：添加系统提示
        // chatHistory.Add(new Message(Role.System, "你是一个有帮助的AI助手。"));
    }

    // 实时句子缓冲区
    StringBuilder sentenceBuffer = new StringBuilder();
    // 句子结束符正则（支持中英文标点）
    Regex sentenceDelimiters = new Regex(@"[。！？.!?](\s|$)|[。！？.!?][”’](\s|$)");

    CancellationTokenSource cts;

    public async void RequestAsync(string prompt)
    {
        // 添加用户消息到历史
        chatHistory.Add(new Message(ChatRole.User, prompt));

        cts = new CancellationTokenSource();

        // 创建聊天请求，包含整个对话历史
        var chatRequest = new ChatRequest();
        chatRequest.Messages = chatHistory;
        chatRequest.Model = modelName;

        try
        {
            // 使用流式聊天接口
            var responseStream = ollama.ChatAsync(chatRequest, cts.Token);

            StringBuilder fullResponse = new StringBuilder();

            await foreach (var response in responseStream)
            {
                if (response != null && response.Message != null)
                {
                    string content = response.Message.Content;

                    if (onWord != null)
                    {
                        onWord(content);
                    }

                    UnityEngine.Debug.Log("模型回答:" + content);

                    // 追加新内容到缓冲区
                    sentenceBuffer.Append(content);
                    fullResponse.Append(content);

                    // 实时处理缓冲区
                    ProcessBuffer(ref sentenceBuffer);

                    // 如果已结束
                    if (response.Done)
                    {
                        break;
                    }
                }
            }

            // 将完整的助理回复添加到历史中
            chatHistory.Add(new Message(ChatRole.Assistant, fullResponse.ToString()));
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"请求出错: {ex.Message}");
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
                UnityEngine.Debug.Log($"完整句子: {sentence}");
            }

            lastIndex = endPos;
        }

        // 保留未完成部分
        buffer = new StringBuilder(content.Substring(lastIndex));
    }

    /// <summary>
    /// 清除对话历史
    /// </summary>
    public void ClearHistory()
    {
        chatHistory.Clear();

        // 可选：重新添加系统提示
        // chatHistory.Add(new Message(Role.System, "你是一个有帮助的AI助手。"));
    }

    /// <summary>
    /// 获取当前对话历史
    /// </summary>
    public List<Message> GetHistory()
    {
        return new List<Message>(chatHistory);
    }

    /// <summary>
    /// 设置对话历史
    /// </summary>
    public void SetHistory(List<Message> history)
    {
        chatHistory = new List<Message>(history);
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
        Interrupt();
        if (ollama != null)
        {
            ollama.Dispose();
            ollama = null;
        }
    }
}