using System;
using System.IO;
using Newtonsoft.Json;
using Resizer.ViewModels;

namespace Resizer;

public static class ResizerConfig
{
    public static Config Instance { get; private set; } = null!;

    public static bool InvertImageX
    {
        get => Instance.InvertImageX;
        set => Instance.InvertImageX = value;
    }
    public static bool InvertImageY 
    {
        get => Instance.InvertImageY;
        set => Instance.InvertImageY = value;
    }
    public static double DefaultScale 
    {
        get => Instance.DefaultScale;
        set => Instance.DefaultScale = value;
    }
    public static double ScaleMultiplier 
    {
        get => Instance.ScaleMultiplier;
        set => Instance.ScaleMultiplier = value;
    }
    
    
    public static int CropperWidth
    {
        get => Instance.CropperWidth;
        set => Instance.CropperWidth = value;
    }
    public static int CropperHeight 
    {
        get => Instance.CropperHeight;
        set => Instance.CropperHeight = value;
    }

    public static void LoadConfig()
    {
        if (!File.Exists("config.json"))
        {
            Instance = new();
            SaveConfig();
            return;
        }

        try
        {
            Config? cfg = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            if (cfg != null)
                Instance = cfg;

            Instance.PropertyChanged += (sender, args) =>
            {
                SaveConfig();
            };
        }
        catch
        {
            Console.WriteLine("Bad config");
        }
    }
    
    private static void SaveConfig()
    {
        File.WriteAllText("config.json", JsonConvert.SerializeObject(Instance));
    }
}