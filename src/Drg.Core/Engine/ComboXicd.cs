using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>combo_xicd_chk_yyy:COMBO_NO(1–164,有缺號)分派器,移植自 rddi0001 2042–3880。
/// 對單一候選(treeDrg + comboNo)依其 COMBO_NO 跑一串 combo_AX/BX/CX 短路嘗試,回 0=配方成立、非 0=不成立。
/// 計數委由 <see cref="ComboCounter"/>;CNT 取其 <see cref="ComboCounter.LastCnt"/>。
/// 註:逐 case 忠實照抄 legacy,完整 DRG 一致性須待串入 combo_drg + 主編排後以 oracle 驗證。</summary>
public sealed class ComboXicd(ComboCounter counter, GroupingContext ctx)
{
    private string _treeDrg = "";
    private string _comboNo = "";

    /// <summary>case 5 可能改寫候選 DRG;combo_drg 取此值。預設為輸入 treeDrg。</summary>
    public string MappedTreeDrg { get; private set; } = "";

    /// <summary>case 74 之 v_DRG_1="ERR" 副作用旗標。</summary>
    public bool Err { get; private set; }

    public int Check(string treeDrg, string comboNo)
    {
        _treeDrg = treeDrg;
        _comboNo = comboNo;
        MappedTreeDrg = treeDrg;
        Err = false;

        var num = 9;   // 未匹配之 COMBO_NO 預設不成立
        switch (int.Parse(comboNo))
        {
            case 1: num = A("A"); break;
            case 3: num = A("A2"); break;
            case 5:
                num = B("B");
                if (num == 0 || (_treeDrg is "03702" or "03704" or "03707"))
                {
                    if (num != 0)
                    {
                        // mdc02 子查詢(RDDT_DRG_MDC02);此處以一般候選表近似,未命中則維持 num。
                        var num2 = AMdc02("A2");
                        if (num2 == 0)
                        {
                            MappedTreeDrg = _treeDrg switch
                            {
                                "03702" => "03701",
                                "03704" => "03703",
                                "03707" => "03706",
                                _ => MappedTreeDrg,
                            };
                        }
                    }
                }
                break;
            case 6: num = B("B5"); break;
            case 7: num = B("B6"); break;
            case 9: num = B("B2"); break;
            case 10: num = B("B8"); break;
            case 12: num = C("C"); break;
            case 16: num = A("A"); if (num == 0) num = A("A2"); break;
            case 17: num = A("A"); if (num == 0) num = B("B"); break;
            case 18:
                num = A("A");
                if (num == 0) { num = B("B5"); if (num == 0) num = B("B3"); }
                break;
            case 19: num = A("A"); if (num == 0) num = A("A4"); break;
            case 20:
                num = B("B");
                if (num != 0) { num = B("B1"); if (num == 0) num = B("B7"); }
                break;
            case 21:
                num = A("A3");
                if (num == 0) { num = B("B"); if (num == 0) num = A("A4"); }
                break;
            case 22: num = A("A"); if (num == 0) num = B("B2"); break;
            case 23:
                num = A("A");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B1"); }
                if (num != 0) num = B("B4");
                break;
            case 24:
                num = A("A3");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B3"); }
                break;
            case 25: num = A("A"); if (num == 0) num = C("C"); break;
            case 26:
                num = A("A3");
                if (num == 0) { num = C("C"); if (num == 0) num = B("B"); }
                break;
            case 27: num = A("A3"); if (num == 0) num = B("B"); break;
            case 28: num = B("B"); num = Cnt <= 1 ? -1 : B("B3"); break;
            case 29: num = B("B"); if (num == 0) num = B("B1"); break;
            case 30: num = B("B"); num = Cnt <= 1 ? -1 : B("B7"); break;
            case 31: num = B("B"); num = Cnt <= 1 ? -1 : 0; break;
            case 32: num = B("B"); if (num == 0) num = B("B2"); break;
            case 33: num = B("B"); num = Cnt <= 2 ? -1 : B("B7"); break;
            case 34: num = B("B"); num = Cnt <= 1 ? -1 : B("B1"); break;
            case 35: num = B("B"); num = Cnt <= 2 ? -1 : B("B2"); break;
            case 36:
                num = B("B");
                if (num == 0) { num = B("B1"); if (num == 0) num = B("B2"); }
                break;
            case 37: num = B("B"); num = Cnt <= 1 ? -1 : B("B2"); break;
            case 38: num = C("C"); if (num == 0) num = B("B"); break;
            case 39: num = C("C"); if (num == 0) num = B("B2"); break;
            case 40:
                num = C("C");
                if (num == 0) { num = C("C1"); if (num == 0) num = B("B7"); }
                break;
            case 41: num = C("C"); if (num == 0) num = C("C1"); break;
            case 42:
                num = C("C");
                if (num == 0) { num = C("C1"); if (num == 0) num = B("B2"); }
                break;
            case 43: num = C("C3"); if (num == 0) num = B("B5"); break;
            case 44: num = C("C3"); if (num == 0) num = C("C"); break;
            case 45:
                num = C("C3");
                if (num == 0) { num = C("C2"); if (num == 0) num = B("B5"); }
                break;
            case 46: num = B("B"); if (num == 0) num = B("B3"); break;
            case 47:
                num = A("A");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B3"); }
                break;
            case 48: num = B("B"); if (num != 0) num = B("B1"); break;
            case 49: num = B("B"); if (num != 0) num = B("B2"); break;
            case 50:
                num = B("B");
                if (num != 0) { num = B("B1"); if (num != 0) num = B("B2"); }
                break;
            case 51:
                num = B("B");
                if (num != 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; }
                if (num != 0) { num = B("B4"); if (num == 0) num = B("B11"); }
                break;
            case 52: num = B("B13"); if (num != 0) num = B("B1"); break;
            case 53:
                num = B("B2");
                if (num != 0) break;
                num = B("B");
                if (num != 0) { num = B("B1"); if (num == 0) num = B("B4"); }
                break;
            case 54:
                num = C("C");
                if (num != 0) num = A("A2");
                if (num != 0) break;
                num = B("B");
                if (num != 0) { num = B("B1"); if (num == 0) num = B("B4"); }
                break;
            case 55:
                num = A("A");
                if (num == 0) num = B("B");
                if (num != 0) num = B("B1");
                break;
            case 56:
                num = C("C");
                if (num == 0) num = B("B");
                if (num != 0) { num = A("A2"); if (num == 0) num = B("B"); }
                break;
            case 57:
                num = B("B3");
                if (num != 0) break;
                num = B("B2");
                if (num == 0) { num = C("C"); if (num == 0) num = A("A2"); }
                break;
            case 58:
                num = C("C");
                if (num != 0) break;   // combo_CX("C") 計數=0 即拒絕(legacy 2476–2481;原移植漏此守門)
                num = C("C3");
                if (num == 0) { num = B("B"); if (num != 0) num = A("A2"); }
                break;
            case 59:
                num = A("A");
                if (num != 0) break;
                num = A("A4");
                if (num == 0)
                {
                    num = B("B6");
                    if (num != 0) { var n2 = B("B5") == 0 ? -1 : 0; num += n2; }
                }
                break;
            case 60:
                num = A("A");
                if (num == 0) { num = B("B"); if (num != 0) num = B("B1"); }
                break;
            case 61:
                num = A("A");
                if (num == 0) { num = B("B8"); if (num != 0) num = B("B2"); }
                break;
            case 62:
                num = B("B");
                if (num != 0) { num = A("A"); if (num == 0) num = B("B1"); }
                if (num == 0) num = B("B3");
                break;
            case 63:
                num = C("C");
                if (num != 0) break;
                num = C("C3");
                if (num == 0) { num = CcMark1 != "Y" ? -1 : 0; if (num != 0) num = C("C1"); }
                break;
            case 64:
                num = C("C");
                if (num == 0) { num = C("C1"); if (num != 0) num = B("B2"); }
                break;
            case 65:
                num = C("C3");
                if (num == 0) { num = C("C1"); if (num != 0) num = B("B2"); }
                break;
            case 66: num = C("C3"); if (num == 0) num = C("C2"); break;
            case 67:
                num = C("C");
                if (num != 0) break;
                num = B("B");
                if (num != 0) num = A("A2");
                if (num == 0) { num = CcMark1 != "Y" ? -1 : 0; if (num != 0) num = C("C1"); }
                break;
            case 68:
                num = A("A3");
                if (num != 0) break;
                num = B("B");
                if (num == 0) { num = B("B1"); if (num == 0) num = B("B2"); }
                break;
            case 69:
                num = C("C");
                if (num == 0) { num = C("C3"); if (num == 0) num = B("B2"); }
                break;
            case 70: num = C("C3"); if (num == 0) num = B("B2"); break;
            case 71:
                num = C("C");
                if (num != 0) break;
                num = A("A4");
                if (num == 0) { num = C("C3"); if (num == 0) num = B("B3"); }
                break;
            case 72:
                num = C("C");
                if (num != 0) break;
                num = A("A4");
                if (num != 0) break;
                num = B("B3");
                if (num == 0) { num = CcMark1 != "Y" ? -1 : 0; if (num != 0) num = C("C1"); }
                break;
            case 73: num = ctx.Days < 28 ? -1 : 0; break;
            case 74:
                num = -1;
                if (ctx.TranCode != "4") break;
                if (ctx.OutDate.Length == 0) { Err = true; break; }
                var baseDay = ctx.PartMark == "903" && ctx.ChildBirthday != "0000/00/00" ? ctx.ChildBirthday : ctx.Birthday;
                var daysN02 = AgeCalculator.Days(ctx.OutDate, baseDay);
                num = daysN02 >= 2 || daysN02 < 0 ? -1 : 0;
                break;
            case 75: num = B("B5") == 0 ? -1 : 0; break;
            case 76: num = C("C"); if (num == 0) num = B("B3"); break;
            case 77:
                num = B("B");
                if (num == 0) num = B("B1");
                if (num != 0) num = B("B4");
                break;
            case 78: num = C("C1"); if (num == 0) num = C("C3"); break;
            case 79: num = B("B"); if (num == 0) num = B("B7"); break;
            case 80:
                num = B("B");
                if (num == 0) { num = B("B1"); if (num == 0) num = B("B7"); }
                break;
            case 81:
                num = B("B");
                if (num == 0) { num = B("B1"); if (num == 0) num = B("B3"); }
                break;
            case 82:
                num = C("C");
                if (num == 0) { num = B("B2"); if (num != 0) num = C("C2"); }
                break;
            case 83:
                num = B("B3");
                if (num != 0) break;
                num = B("B");
                if (num != 0) { num = B("B1"); if (num == 0) num = B("B4"); }
                if (num != 0) { num = B("B11"); if (num == 0) num = B("B12"); }
                break;
            case 84:
                num = B("B");
                if (num == 0) { num = B("B1"); if (num != 0) { num = B("B4"); num = Cnt <= 1 ? -1 : 0; } }
                break;
            case 85: num = C("C"); if (num == 0) num = B("B7"); break;
            case 86:
                num = C("C");
                if (num == 0) { num = B("B7"); if (num == 0) num = C("C3"); }
                break;
            case 87: num = A("A"); if (num == 0) num = B("B7"); break;
            case 88:
                num = C("C3");
                if (num == 0) { var n2 = B("B5") == 0 ? -1 : 0; num += n2; }
                break;
            case 89:
                num = B("B2");
                if (num == 0) { num = B("B"); if (num != 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; } }
                break;
            case 90:
                num = B("B");
                if (num == 0) { num = B("B2"); if (num == 0) num = B("B3"); }
                break;
            case 91:
                num = B("B7");
                if (num == 0) { num = B("B"); if (num != 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; } }
                break;
            case 92:
                num = B("B");
                if (num == 0) { num = B("B3"); if (num == 0) num = B("B7"); }
                break;
            case 93:
                num = A("A3");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B1"); }
                if (num != 0) num = B("B4");
                break;
            case 94:
                num = A("A");
                if (num != 0) { num = A("A1"); if (num == 0) num = A("A2"); }
                break;
            case 95:
                num = C("C");
                if (num == 0) { num = C("C1"); if (num == 0) num = C("C3"); }
                break;
            case 96:
                num = A("A3");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B1"); }
                break;
            case 97:
                num = B("B3");
                if (num != 0) break;
                num = B("B");
                if (num != 0) { num = B("B1"); if (num == 0) num = B("B4"); }
                break;
            case 98:
                num = B("B3");
                if (num == 0) { num = B("B2"); if (num == 0) num = A("A2"); }
                break;
            case 99:
                num = B("B1");
                if (num != 0) break;
                num = B("B");
                if (num != 0) { num = C("C"); if (num != 0) num = A("A2"); }
                break;
            case 100:
                num = B("B");
                if (num != 0) break;
                num = B("B2");
                if (num == 0) { num = B("B3"); if (num == 0) num = A("A2"); }
                break;
            case 101:
                num = B("B2");
                if (num != 0) break;
                num = A("A");
                if (num != 0) { num = A("A1"); if (num == 0) num = A("A2"); }
                break;
            case 102:
                num = C("C3");
                if (num == 0) { num = C("C2"); if (num != 0) num = B("B2"); }
                break;
            case 103:
                num = B("B7");
                if (num != 0) break;
                num = A("A");
                if (num != 0) { num = A("A1"); if (num == 0) num = A("A2"); }
                break;
            case 104:
                num = C("C");
                if (num != 0) break;
                num = B("B3");
                if (num != 0) break;
                num = B("B");
                if (num == 0) break;
                num = B("B2");
                if (num != 0) { num = C("C1"); if (num != 0) num = A("A2"); }
                break;
            case 105:
                num = A("A");
                if (num == 0) { num = A("A2"); if (num == 0) num = A("A4"); }
                break;
            case 106:
                num = A("A");
                if (num == 0) { num = A("A2"); if (num == 0) num = A("A5"); }
                break;
            case 107:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num == 0) { num = A("A4"); if (num == 0) num = B("B7"); }
                break;
            case 108:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num == 0) { num = A("A5"); if (num == 0) num = A("A6"); }
                break;
            case 109:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num == 0) { num = A("A4"); if (num == 0) num = A("A5"); }
                break;
            case 110:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num != 0) break;
                num = A("A5");
                if (num == 0) { num = B("B2"); if (num == 0) num = B("B9"); }
                break;
            case 111:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num != 0) break;
                num = A("A5");
                if (num == 0) { num = B("B2"); if (num == 0) num = B("B7"); }
                break;
            case 112:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num == 0) { num = A("A5"); if (num == 0) num = B("B7"); }
                break;
            case 113:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num == 0) { num = A("A4"); if (num == 0) num = B("B2"); }
                break;
            case 114:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num != 0) break;
                num = A("A4");
                if (num == 0) { num = B("B2"); if (num == 0) num = B("B9"); }
                break;
            case 115:
                num = A("A");
                if (num != 0) break;
                num = A("A2");
                if (num != 0) break;
                num = A("A4");
                if (num == 0) { num = B("B2"); if (num == 0) num = B("B7"); }
                break;
            case 116: num = A("A"); if (num == 0) { num = A("A2"); num = Cnt <= 1 ? -1 : 0; } break;
            case 117: num = A("A"); if (num == 0) { num = B("B"); num = Cnt <= 1 ? -1 : 0; } break;
            case 118:
                num = A("A");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B7"); }
                break;
            case 119:
                num = A("A");
                if (num != 0) break;
                num = B("B");
                if (num == 0) { num = B("B2"); if (num == 0) num = B("B7"); }
                break;
            case 120:
                num = A("A");
                if (num != 0) break;
                num = B("B");
                if (num == 0) { num = B("B2"); if (num == 0) num = B("B9"); }
                break;
            case 121:
                num = A("A");
                if (num == 0) { num = A("A4"); if (num == 0) num = B("B2"); }
                break;
            case 123:
                num = A("A");
                if (num == 0) { num = A("A4"); if (num == 0) num = B("B7"); }
                break;
            case 124: num = A("A3"); if (num == 0) { num = B("B"); num = Cnt <= 1 ? -1 : 0; } break;
            case 125: num = A("A"); if (num == 0) num = B("B3"); break;
            case 126:
                num = B("B2");
                if (num != 0) break;
                num = B("B7");
                if (num != 0) break;
                num = A("A");
                if (num != 0) { num = A("A1"); if (num == 0) num = A("A2"); }
                break;
            case 127:
                num = B("B2");
                if (num != 0) break;
                num = B("B9");
                if (num != 0) break;
                num = A("A");
                if (num != 0) { num = A("A1"); if (num == 0) num = A("A2"); }
                break;
            case 128:
                num = B("B2");
                if (num == 0) { num = A("A"); if (num == 0) num = C("C"); }
                break;
            case 129:
                num = B("B7");
                if (num == 0) { num = A("A"); if (num == 0) num = C("C"); }
                break;
            case 130:
                num = C("C");
                if (num != 0) break;
                num = C("C3");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B3"); }
                break;
            case 131:
                num = A("A3");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B2"); }
                break;
            case 132:
                num = A("A");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B2"); }
                break;
            case 133:
                num = A("A");
                if (num == 0) { num = B("B"); if (num != 0) num = B("B2"); }
                break;
            case 134:
                num = A("A3");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B7"); }
                break;
            case 135:
                num = A("A");
                if (num != 0) break;
                num = B("B2");
                if (num == 0) { num = B("B9"); if (num == 0) num = B("B10"); }
                break;
            case 136:
                num = A("A");
                if (num != 0) break;
                num = B("B2");
                if (num == 0) { num = B("B9"); if (num == 0) num = B("B7"); }
                break;
            case 137:
                num = A("A");
                if (num == 0) { num = A("A2"); if (num == 0) num = B("B2"); }
                break;
            case 138:
                num = A("A");
                if (num == 0) { num = A("A2"); if (num == 0) num = B("B"); }
                break;
            case 139:
                num = A("A");
                if (num == 0) { num = A("A2"); if (num == 0) num = B("B"); }
                break;
            case 141:
                num = B("B");
                if (num == 0) { num = B("B1"); if (num == 0) { num = B("B4"); if (num == 0) num = B("B11"); } }
                if (num == 0) break;
                num = B("B12");
                if (num == 0) { num = B("B4"); if (num == 0) num = B("B11"); }
                break;
            case 142:
                num = B("B");
                if (num != 0) break;
                num = B("B1");
                if (num == 0) { num = B("B3"); if (num == 0) num = B("B4"); }
                break;
            case 143:
                num = B("B");
                if (num != 0) break;
                num = B("B1");
                if (num == 0) { num = B("B3"); if (num == 0) num = B("B7"); }
                break;
            case 144:
                num = B("B");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B"); }
                break;
            case 145:
                num = B("B");
                if (num == 0) { num = B("B1"); if (num == 0) num = B("B4"); }
                if (num != 0) { num = B("B4"); if (num == 0) num = B("B11"); }
                break;
            case 146:
                num = B("B3");
                if (num != 0) break;
                num = B("B4");
                if (num != 0) { num = B("B"); if (num == 0) num = B("B1"); }
                break;
            case 147:
                num = B("B4");
                if (Cnt > 2) { num = B("B"); if (num != 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; } }
                else num = -1;
                break;
            case 148:
                num = B("B4");
                if (Cnt > 1) { num = B("B"); if (num != 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; } }
                else num = -1;
                break;
            case 149:
                num = B("B4");
                if (num == 0) { num = B("B"); if (num != 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; } }
                break;
            case 150:
                num = B("B3");
                if (num == 0) { num = B("B"); if (num != 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; } }
                break;
            case 151: num = B("B"); if (num == 0) { num = B("B1"); num = Cnt <= 2 ? -1 : 0; } break;
            case 152: num = B("B"); if (num == 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; } break;
            case 153: num = B("B2"); if (num == 0) num = B("B7"); break;
            case 154:
                num = A("A");
                if (num == 0) { num = B("B3"); if (num == 0) { num = B("B"); num = Cnt <= 1 ? -1 : 0; } }
                break;
            case 155:
                num = A("A3");
                if (num == 0) { num = B("B3"); if (num == 0) { num = B("B"); num = Cnt <= 1 ? -1 : 0; } }
                break;
            case 156:
                num = A("A3");
                if (num != 0) break;
                num = B("B");
                if (num == 0) { num = B("B1"); if (num == 0) num = B("B3"); }
                break;
            case 157:
                num = A("A");
                if (num == 0) { num = B("B"); if (num != 0) { num = B("B1"); num = Cnt <= 1 ? -1 : 0; } }
                break;
            case 158:
                num = C("C");
                if (num == 0) { num = B("B"); if (num == 0) num = B("B3"); }
                break;
            case 159:
                num = C("C3");
                if (num == 0) { num = C("C"); if (num == 0) num = B("B5"); }
                break;
            case 160:
                num = C("C");
                if (num != 0) break;
                num = C("C1");
                if (num != 0) break;
                num = C("C3");
                if (num == 0) { num = CcMark1 != "Y" ? -1 : 0; if (num != 0) num = C("C4"); }
                break;
            case 161:
                num = C("C3");
                if (num == 0) { num = C("C2"); if (num != 0) num = B("B2"); }
                break;
            case 162:
                num = C("C");
                if (num != 0) break;
                num = A("A2");
                if (Cnt > 1) { num = B("B"); if (num != 0) num = B("B2"); }
                else num = -1;
                break;
            case 163:
                if (ctx.Days >= 28)
                {
                    num = C("C");
                    if (num != 0) { num = B("B5"); if (num != 0) num = B("B2"); }
                }
                else num = -1;
                break;
            case 164:
                if (ctx.Days >= 28) { num = C("C"); if (num == 0) num = B("B7"); }
                else num = -1;
                break;
        }
        return num;
    }

    private int A(string item) => counter.ComboA(_treeDrg, _comboNo, item) ? 0 : -1;
    private int B(string item) => counter.ComboB(_treeDrg, _comboNo, item) ? 0 : -1;
    private int C(string item) => counter.ComboC(_treeDrg, _comboNo, item) ? 0 : -1;

    // case 5 mdc02 子查詢:此資料版以一般候選表近似(RDDT_DRG_MDC02 專屬路徑後續補)。
    private int AMdc02(string item) => A(item);

    private int Cnt => counter.LastCnt;
    private string CcMark1 => ctx.CcMark == "N" ? "N" : "Y";
}
