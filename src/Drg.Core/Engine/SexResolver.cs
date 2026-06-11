namespace Drg.Core.Engine;

/// <summary>性別正規化,移植自 legacy DRGService.convertSex。
/// 已是 F/M/X(忽略大小寫)即沿用;否則由身分證第 2 碼推導:1/C/A→M、2/D/B→F、其餘→X。</summary>
public static class SexResolver
{
    public static string Resolve(string sex, string pid)
    {
        var s = (sex ?? "").ToUpperInvariant();
        if (s is "F" or "M" or "X") return s;

        // Strings.Mid(pid, 2, 1):身分證第 2 碼(性別碼)
        var second = (pid ?? "").Length >= 2 ? pid![1].ToString() : "";
        return second switch
        {
            "1" or "C" or "A" => "M",
            "2" or "D" or "B" => "F",
            _ => "X",
        };
    }
}
