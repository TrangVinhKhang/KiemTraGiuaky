using KiemTraGiuaKy.Debugging;

namespace KiemTraGiuaKy;

public class KiemTraGiuaKyConsts
{
    public const string LocalizationSourceName = "KiemTraGiuaKy";

    public const string ConnectionStringName = "Default";

    public const bool MultiTenancyEnabled = true;


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "8aa38776851542fcb29289ae4bb44b0b";
}
