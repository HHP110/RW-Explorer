using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public class AppConfig
{
    public string GameDirectory { get; set; }
    public Dictionary<string, string> FolderComments { get; set; } = new Dictionary<string, string>();

    public static AppConfig Load(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载配置文件失败: {ex.Message}");
        }
        return new AppConfig();
    }

    public void Save(string filePath)
    {
        try
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"保存配置文件失败: {ex.Message}");
        }
    }
    public string GetFolderComment(string folderPath)
    {
        return FolderComments.TryGetValue(folderPath, out var comment) ? comment : null;
    }
    public void SetFolderComment(string folderPath, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            if (FolderComments.ContainsKey(folderPath))
                FolderComments.Remove(folderPath);
        }
        else
        {
            FolderComments[folderPath] = comment;
        }
    }
}