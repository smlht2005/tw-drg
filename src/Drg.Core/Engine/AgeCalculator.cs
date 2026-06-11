using System.Globalization;

namespace Drg.Core.Engine;

/// <summary>年齡/月數/日數計算,移植自 legacy rddi0001 ages_cnt_yyy / months_cnt_yyy / days_cnt_yyy。
/// 日期格式同 legacy:"yyyy/MM/dd";空字串視為缺漏。</summary>
public static class AgeCalculator
{
    // ages_cnt_yyy:以 (now-base) 的時間差落在 0001/01/01 之年份 -1;空值或解析失敗回 0,負值歸 0。
    public static int Years(string nowDate, string baseDate)
    {
        if (baseDate.Length == 0 || nowDate.Length == 0) return 0;

        var years = 0;
        if (DateTime.TryParseExact(baseDate.Replace('/', '-'), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var b)
            && DateTime.TryParseExact(nowDate.Replace('/', '-'), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var n))
        {
            years = (new DateTime(1, 1, 1) + (n - b)).Year - 1;
        }
        return years < 0 ? 0 : years;
    }

    // months_cnt_yyy:僅取月份欄,nowMonth-baseMonth,負值 +12(不跨年累計)。空值回 0。
    public static int Months(string nowDate, string baseDate)
    {
        if (baseDate.Length == 0 || nowDate.Length == 0) return 0;

        var baseMonth = Convert.ToInt32(baseDate.Split('/')[1]);
        var nowMonth = Convert.ToInt32(nowDate.Split('/')[1]);
        var diff = nowMonth - baseMonth;
        return diff < 0 ? diff + 12 : diff;
    }

    // days_cnt_yyy:(now-base).Days,可為負;空值回 -1(legacy 不 clamp)。
    public static int Days(string nowDate, string baseDate)
    {
        if (baseDate.Length == 0 || nowDate.Length == 0) return -1;

        var b = DateTime.ParseExact(baseDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
        var n = DateTime.ParseExact(nowDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
        return (n - b).Days;
    }
}
