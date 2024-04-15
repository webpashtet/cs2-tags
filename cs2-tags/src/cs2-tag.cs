using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using System.Collections.Concurrent;
using TagApi;
using IksAdminApi;
using Microsoft.Extensions.Logging;
using static TagApi.Tag;

namespace Tag;

public partial class Tag : BasePlugin, IPluginConfig<TagConfig>
{
    public override string ModuleName => "Tag";
    public override string ModuleVersion => "0.0.2";
    public override string ModuleAuthor => "schwarper";
    public override string ModuleDescription => "Fork with IksAdmin capability";
    
    private static readonly PluginCapability<IIksAdminApi> AdminCapability = new("iksadmin:core");

    public TagConfig Config { get; set; } = new();
    public ConcurrentDictionary<int, CTag> PlayersData { get; } = new();
    public bool[] PlayerToggleTags { get; } = new bool[64];
    public static Tag Instance { get; private set; } = new();
    public int GlobalTick { get; set; }
    private static IIksAdminApi? _api;

    public override void Load(bool hotReload)
    {
        Capabilities.RegisterPluginCapability(ITagApi.Capability, () => new TagAPI());

        Instance = this;

        for (int i = 0; i < 64; i++)
        {
            PlayersData[i] = new();
            PlayerToggleTags[i] = new();
        }

        if (hotReload)
        {
            UpdatePlayerTags();
        }

        Event.Load();
    }

    public void OnConfigParsed(TagConfig config)
    {
        try
        {
            _api = AdminCapability.Get();
        }
        catch (Exception)
        {
            Logger.LogInformation("IksAdminApi.dll nety :(");
        }
        
        Json.ReadCore();
        Config = config;
    }

    public static void UpdatePlayerTags()
    {
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            Instance.PlayersData[player.Slot] = GetTag(player);
        }
    }

    public static CTag GetTag(CCSPlayerController player)
    {
        ConcurrentDictionary<string, CTag> tags = Instance.Config.Tags;

        CTag steamIdTag = tags.FirstOrDefault(tag => tag.Key == player.SteamID.ToString()).Value;

        if (steamIdTag != null)
        {
            return steamIdTag;
        }

        CTag groupTag = tags.FirstOrDefault(tag => tag.Key.StartsWith('#') && AdminManager.PlayerInGroup(player, tag.Key)).Value;

        if (groupTag != null)
        {
            return groupTag;
        }

        CTag permissionTag = tags.FirstOrDefault(tag => tag.Key.StartsWith('@') 
                                                        && (AdminManager.PlayerHasPermissions(player, tag.Key) 
                                                            || _api!.HasPermisions(player.SteamID.ToString(), string.Empty, tag.Key))).Value;

        if (permissionTag != null)
        {
            return permissionTag;
        }

        return tags.FirstOrDefault(tag => tag.Key == "default").Value ?? new CTag();
    }
}