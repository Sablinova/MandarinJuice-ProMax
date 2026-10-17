using System.Runtime.InteropServices;
using MandarinJuiceCore.Helpers;
using MandarinJuiceCore.Infrastructure;
using Mi5hmasH.GameLaunchers.Epic.Types;

namespace MandarinJuiceCore.GamingPlatforms;

public class Epic : IGamingPlatform
{
    public string AppId { get; set; } = string.Empty;
    public string UserIdInput { get; set; } = string.Empty;
    public string UserIdOutput { get; set; } = string.Empty;
    public uint ParseVariant { get; set; } = 0;

    public const string StoreBaseUrl = "https://store.epicgames.com/en/p";
    public void OpenStoreProductPage() => $"{StoreBaseUrl}/{AppId}".OpenUrl();

    public ulong GetParsedUserIdInput() => ParseUserId(UserIdInput);
    public ulong GetParsedUserIdOutput() => ParseUserId(UserIdOutput);

    public ulong ParseUserId(string userId)
    {
        var epicId = new EpicId();
        return !epicId.TrySetEpicId(userId) 
            ? throw new FormatException("The provided User ID is not a valid Epic ID.") 
            : ParseUserIdInternal(epicId);
    }

    public ulong ParseUserId(uint id) => ParseUserIdInternal(id);
    public ulong ParseUserId(ulong id) => ParseUserIdInternal(id);

    private ulong ParseUserIdInternal(EpicId epicId)
    {
        var (uintHash, ulongHash) = ComputeHashes(epicId);
        return ApplyVariant(uintHash, ulongHash, ParseVariant);
    }

    private ulong ParseUserIdInternal(uint id) 
        => ApplyVariant(id, id, ParseVariant);

    private ulong ParseUserIdInternal(ulong id) 
        => ApplyVariant((uint)id, id, ParseVariant);

    private static ulong ApplyVariant(uint uintHash, ulong ulongHash, uint variant)
    {
        return variant switch
        {
            0 => ulongHash,
            1 => ~uintHash | 0xFFFFFFFF00000000,
            2 => ~ulongHash,
            _ => throw new NotSupportedException($"The Epic ID variant '{variant}' is not supported.")
        };
    }

    private static (uint fullHash, ulong ulongHash) ComputeHashes(EpicId epicId)
    {
        const uint seed = 0xFFFFFFFF;

        var wide = epicId.GetAsWideString().AsSpan();
        var asUint = MemoryMarshal.Cast<byte, uint>(wide);

        var hi = MandarinDeencryptor.Murmur3_32(asUint[..8], seed);
        var lo = MandarinDeencryptor.Murmur3_32(asUint[^8..], seed);

        var ulongHash = ((ulong)hi << 32) | lo;
        var fullHash = MandarinDeencryptor.Murmur3_32(asUint, seed);

        return (fullHash, ulongHash);
    }
}