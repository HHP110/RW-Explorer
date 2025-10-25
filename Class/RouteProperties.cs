using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

public class RouteProperties
{
    public string DisplayName { get; set; } = "未命名线路";
    public List<string> Providers { get; set; } = new List<string>();
    public string FolderPath { get; set; }
    public string ImagePath { get; set; }
    public DateTime CreationTime { get; set; }

    public static RouteProperties ParseFromXml(string xmlContent)
    {
        var props = new RouteProperties();

        try
        {
            var doc = XDocument.Parse(xmlContent);
            props.DisplayName = ParseDisplayName(doc) ?? "未命名线路";
            props.Providers = ParseProviders(doc);
            Debug.WriteLine($"找到 {props.Providers.Count} 个provider");

            return props;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"XML解析错误: {ex.Message}");
            return props;
        }
    }

    private static string ParseDisplayName(XDocument doc)
    {
        try
        {
            var displayNameElement = doc.Descendants("DisplayName").FirstOrDefault();
            if (displayNameElement == null)
            {
                Debug.WriteLine("未找到DisplayName元素");
                return null;
            }

            var localizedString = displayNameElement.Element("Localisation-cUserLocalisedString");
            if (localizedString == null)
            {
                Debug.WriteLine("Not Found：Localisation-cUserLocalisedString");
                return null;
            }
            var englishName = localizedString.Element("English")?.Value;
            if (!string.IsNullOrEmpty(englishName))
            {
                Debug.WriteLine($"英文名称: {englishName}");
                return englishName;
            }
            var chinesePair = localizedString.Element("Other")?
                .Elements("Localisation-cUserLocalisedString-cOtherStringLangPair")
                .FirstOrDefault(x => x.Element("Language")?.Value == "zh");

            var chineseName = chinesePair?.Element("String")?.Value;
            if (!string.IsNullOrEmpty(chineseName))
            {
                Debug.WriteLine($"中文名称: {chineseName}");
                return chineseName;
            }

            Debug.WriteLine("未找到有效的显示名称");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"解析显示名称失败: {ex.Message}");
            return null;
        }
    }

    private static List<string> ParseProviders(XDocument doc)
    {
        var providers = new List<string>();

        try
        {
            var blueprintSets = doc.Descendants("RBlueprintSetPreLoad");
            Debug.WriteLine($"找到 {blueprintSets.Count()} 个RBlueprintSetPreLoad元素");

            foreach (var set in blueprintSets)
            {
                var providerElement = set.Element("Provider");
                if (providerElement != null)
                {
                    string provider = providerElement.Value;
                    if (!string.IsNullOrEmpty(provider) && !providers.Contains(provider))
                    {
                        providers.Add(provider);
                        Debug.WriteLine($"添加provider: {provider}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"解析provider失败: {ex.Message}");
        }

        return providers;
    }
}