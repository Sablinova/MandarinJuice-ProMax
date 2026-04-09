using MandarinJuiceCore.Infrastructure;
using Mi5hmasH.GameLaunchers.Steam.Types;

namespace MandarinJuiceCore.GamingPlatforms;

public class Steam : IGamingPlatform
{
    public string AppId { get; set; } = string.Empty;
    public string UserIdInput { get; set; } = string.Empty;
    public string UserIdOutput { get; set; } = string.Empty;
    public uint ParseVariant { get; set; } = 0;

    public const string StoreBaseUrl = "https://store.steampowered.com/app";
    public void OpenStoreProductPage() => $"{StoreBaseUrl}/{AppId}".OpenUrl();

    public ulong GetParsedUserIdInput() => ParseUserId(UserIdInput);
    public ulong GetParsedUserIdOutput() => ParseUserId(UserIdOutput);

    public ulong ParseUserId(string userId) 
        => ParseUserIdInternal(s => s.Set(userId));

    public ulong ParseUserId(uint userId) 
    {
        ulong steamId64 = 0x0110000100000000UL | userId;
        
        switch (ParseVariant)
        {
            case 0: return steamId64;
            case 1: return ~((ulong)userId) | 0xFFFFFFFF00000000UL;
            case 2: return ~steamId64;
            case 3: 
                ulong notSteamId = steamId64 ^ 0x1A3B5C7DD0C2B4A8;
                ulong obfuscated = ((notSteamId >> 32) & 0xFF) |
                       (((notSteamId >> 40) & 0xFF) << 8) |
                       (((notSteamId >> 48) & 0xFF) << 16) |
                       (((notSteamId >> 56) & 0xFF) << 24) |
                       ((notSteamId & 0xFF) << 32) |
                       (((notSteamId >> 8) & 0xFF) << 40) |
                       (((notSteamId >> 16) & 0xFF) << 48) |
                       (((notSteamId >> 24) & 0xFF) << 56);
                return ~obfuscated;
            default: throw new NotSupportedException($"The Steam ID variant '{ParseVariant}' is not supported.");
        }
    }

    public ulong ParseUserId(ulong userId) 
        => ParseUserIdInternal(s => s.Set(userId));

    private ulong ParseUserIdInternal(Func<SteamId, bool> setter)
    {
        var steamId = new SteamId();
        var result = setter(steamId);
        return !result
            ? throw new FormatException("The provided User ID is not a valid Steam ID.")
            : ParseUserIdInternal(steamId);
    }

    private ulong ParseUserIdInternal(SteamId steamId)
    {
        return ParseVariant switch
        {
            0 => steamId.GetSteamId64(),
            1 => ~steamId.AccountId | 0xFFFFFFFF00000000UL,
            2 => ~steamId.GetSteamId64(),
            3 => ~GetObfuscatedSteamId64(steamId),
            _ => throw new NotSupportedException($"The Steam ID variant '{ParseVariant}' is not supported.")
        };

        static ulong GetObfuscatedSteamId64(SteamId steamId)
        {
            var notSteamId = steamId.GetSteamId64() ^ 0x1A3B5C7DD0C2B4A8;
            return ((notSteamId >> 32) & 0xFF) |
                   (((notSteamId >> 40) & 0xFF) << 8) |
                   (((notSteamId >> 48) & 0xFF) << 16) |
                   (((notSteamId >> 56) & 0xFF) << 24) |
                   ((notSteamId & 0xFF) << 32) |
                   (((notSteamId >> 8) & 0xFF) << 40) |
                   (((notSteamId >> 16) & 0xFF) << 48) |
                   (((notSteamId >> 24) & 0xFF) << 56);
        }
    }
}