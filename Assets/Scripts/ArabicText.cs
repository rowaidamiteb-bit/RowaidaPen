using System.Collections.Generic;
using System.Text;
using UnityEngine;

// نصوص Unity العادية ما تدعم العربي: الحروف تطلع منفصلة ومقلوبة.
// هذا يحول النص لأشكال الحروف المتصلة (أول/وسط/آخر الكلمة) ويقلبه عشان يطلع صح من اليمين لليسار
public static class ArabicText
{
    // لكل حرف: معزول، آخر، أول، وسط (0 = ما له هالشكل)
    static readonly Dictionary<char, ushort[]> forms = new Dictionary<char, ushort[]>
    {
        { 'ء', new ushort[] { 0xFE80, 0, 0, 0 } },
        { 'آ', new ushort[] { 0xFE81, 0xFE82, 0, 0 } },
        { 'أ', new ushort[] { 0xFE83, 0xFE84, 0, 0 } },
        { 'ؤ', new ushort[] { 0xFE85, 0xFE86, 0, 0 } },
        { 'إ', new ushort[] { 0xFE87, 0xFE88, 0, 0 } },
        { 'ئ', new ushort[] { 0xFE89, 0xFE8A, 0xFE8B, 0xFE8C } },
        { 'ا', new ushort[] { 0xFE8D, 0xFE8E, 0, 0 } },
        { 'ب', new ushort[] { 0xFE8F, 0xFE90, 0xFE91, 0xFE92 } },
        { 'ة', new ushort[] { 0xFE93, 0xFE94, 0, 0 } },
        { 'ت', new ushort[] { 0xFE95, 0xFE96, 0xFE97, 0xFE98 } },
        { 'ث', new ushort[] { 0xFE99, 0xFE9A, 0xFE9B, 0xFE9C } },
        { 'ج', new ushort[] { 0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0 } },
        { 'ح', new ushort[] { 0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4 } },
        { 'خ', new ushort[] { 0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8 } },
        { 'د', new ushort[] { 0xFEA9, 0xFEAA, 0, 0 } },
        { 'ذ', new ushort[] { 0xFEAB, 0xFEAC, 0, 0 } },
        { 'ر', new ushort[] { 0xFEAD, 0xFEAE, 0, 0 } },
        { 'ز', new ushort[] { 0xFEAF, 0xFEB0, 0, 0 } },
        { 'س', new ushort[] { 0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4 } },
        { 'ش', new ushort[] { 0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8 } },
        { 'ص', new ushort[] { 0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC } },
        { 'ض', new ushort[] { 0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0 } },
        { 'ط', new ushort[] { 0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4 } },
        { 'ظ', new ushort[] { 0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8 } },
        { 'ع', new ushort[] { 0xFEC9, 0xFECA, 0xFECB, 0xFECC } },
        { 'غ', new ushort[] { 0xFECD, 0xFECE, 0xFECF, 0xFED0 } },
        { 'ـ', new ushort[] { 0x0640, 0x0640, 0x0640, 0x0640 } },
        { 'ف', new ushort[] { 0xFED1, 0xFED2, 0xFED3, 0xFED4 } },
        { 'ق', new ushort[] { 0xFED5, 0xFED6, 0xFED7, 0xFED8 } },
        { 'ك', new ushort[] { 0xFED9, 0xFEDA, 0xFEDB, 0xFEDC } },
        { 'ل', new ushort[] { 0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0 } },
        { 'م', new ushort[] { 0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4 } },
        { 'ن', new ushort[] { 0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8 } },
        { 'ه', new ushort[] { 0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC } },
        { 'و', new ushort[] { 0xFEED, 0xFEEE, 0, 0 } },
        { 'ى', new ushort[] { 0xFEEF, 0xFEF0, 0, 0 } },
        { 'ي', new ushort[] { 0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4 } },
    };

    // لام + ألف تصير حرف واحد (لا، لأ، لإ، لآ): معزول، آخر
    static readonly Dictionary<char, ushort[]> lamAlef = new Dictionary<char, ushort[]>
    {
        { 'آ', new ushort[] { 0xFEF5, 0xFEF6 } },
        { 'أ', new ushort[] { 0xFEF7, 0xFEF8 } },
        { 'إ', new ushort[] { 0xFEF9, 0xFEFA } },
        { 'ا', new ushort[] { 0xFEFB, 0xFEFC } },
    };

    static Font font;

    // خط من الويندوز يدعم الحروف العربية
    public static Font Font
    {
        get
        {
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Tahoma", "Segoe UI", "Arial" }, 64);
            return font;
        }
    }

    static bool IsArabic(char c) => forms.ContainsKey(c);
    static bool IsDiacritic(char c) => c >= 'ً' && c <= 'ٟ';

    // الحرف يقدر يتصل باللي بعده (يعني له شكل أول/وسط)
    static bool JoinsNext(char c) => forms.TryGetValue(c, out var f) && f[2] != 0;

    // يحوّل سطر عربي لشكل يطلع صح في Unity (كل سطر لحاله)
    public static string Fix(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++) lines[i] = FixLine(lines[i]);
        return string.Join("\n", lines);
    }

    static string FixLine(string s)
    {
        var shaped = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (!IsArabic(c))
            {
                if (!IsDiacritic(c)) shaped.Append(c);
                continue;
            }

            char prev = Neighbor(s, i, -1);
            char next = Neighbor(s, i, +1);
            bool prevJoins = JoinsNext(prev);

            // لام ألف
            if (c == 'ل' && lamAlef.TryGetValue(next, out var lig))
            {
                shaped.Append((char)(prevJoins ? lig[1] : lig[0]));
                i = IndexOfNeighbor(s, i, +1);
                continue;
            }

            ushort[] f = forms[c];
            bool nextJoins = IsArabic(next) && f[2] != 0;
            ushort glyph;
            if (prevJoins && nextJoins && f[3] != 0) glyph = f[3];
            else if (prevJoins && f[1] != 0) glyph = f[1];
            else if (nextJoins && f[2] != 0) glyph = f[2];
            else glyph = f[0];
            shaped.Append((char)glyph);
        }

        // نقلب الترتيب عشان النص ينرسم من اليمين لليسار
        char[] arr = shaped.ToString().ToCharArray();
        System.Array.Reverse(arr);
        for (int i = 0; i < arr.Length; i++)
        {
            // نعكس الأقواس عشان تطلع بالاتجاه الصح
            if (arr[i] == '(') arr[i] = ')'; else if (arr[i] == ')') arr[i] = '(';
        }
        return new string(arr);
    }

    // الحرف اللي قبل/بعد (نتجاهل التشكيل)
    static char Neighbor(string s, int i, int dir)
    {
        int j = IndexOfNeighbor(s, i, dir);
        return j >= 0 && j < s.Length ? s[j] : '\0';
    }

    static int IndexOfNeighbor(string s, int i, int dir)
    {
        int j = i + dir;
        while (j >= 0 && j < s.Length && IsDiacritic(s[j])) j += dir;
        return j;
    }
}
