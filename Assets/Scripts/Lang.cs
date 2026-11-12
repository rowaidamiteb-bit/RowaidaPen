using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// لغة اللعبة: الإنجليزي هو الأساسي، والعربي بزر بالإعدادات
// الكلام بالكود مكتوب بالعربي، وهنا ترجمته الإنجليزية: يتترجم وقت ما ينعرض (ArabicText.Fix يمر من هنا)
// التسجيلات الصوتية تبقى عربي، والترجمة اللي تحتها تصير إنجليزي
public static class Lang
{
    const string Key = "language";   // 1 = إنجليزي (الأساسي)، 0 = عربي
    static int english = -1;
    static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

    public static bool English
    {
        get
        {
            if (english < 0) english = PlayerPrefs.GetInt(Key, 1);
            return english == 1;
        }
        set
        {
            english = value ? 1 : 0;
            PlayerPrefs.SetInt(Key, english);
            PlayerPrefs.Save();
            cache.Clear();
        }
    }

    public static bool HasArabic(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (char c in s) if (c >= '؀' && c <= 'ۿ') return true;
        return false;
    }

    // ترجمة جملة (بالإنجليزي بس، وبالعربي ترجع زي ما هي). الأرقام داخل الجملة تبقى مكانها
    public static string Tr(string s)
    {
        if (!English || !HasArabic(s)) return s;
        if (cache.TryGetValue(s, out string done)) return done;
        string result = Lookup(s);
        if (cache.Count > 3000) cache.Clear();
        cache[s] = result;
        return result;
    }

    // جملة بقالب، والأجزاء العربية تتترجم كل وحدة لحالها: Lang.F("المرحلة {0}: {1}", 5, title)
    public static string F(string template, params object[] args)
    {
        if (!English) return string.Format(template, args);
        for (int i = 0; i < args.Length; i++) if (args[i] is string a) args[i] = Tr(a);
        return string.Format(Tr(template), args);
    }

    // أرقام عربية (١٢٣) أو إنجليزية حسب اللغة
    public static string Digits(int n)
    {
        if (English) return n.ToString();
        char[] s = n.ToString().ToCharArray();
        for (int i = 0; i < s.Length; i++) s[i] = (char)('٠' + (s[i] - '0'));
        return new string(s);
    }

    static string Lookup(string s)
    {
        if (En.TryGetValue(s, out string en)) return en;
        string t = s.Trim();
        if (En.TryGetValue(t, out en)) return en;
        // جملة فيها أرقام: نحط مكانها {0} {1} وندور القالب
        var nums = new List<object>();
        string template = Regex.Replace(t, "[0-9٠-٩]+", m => { nums.Add(Latin(m.Value)); return "{" + (nums.Count - 1) + "}"; });
        if (nums.Count > 0 && En.TryGetValue(template, out en)) return string.Format(en, nums.ToArray());
        // نص بأكثر من سطر: كل سطر لحاله
        if (s.Contains("\n"))
        {
            string[] lines = s.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = Tr(lines[i]);
            return string.Join("\n", lines);
        }
        if (Application.isEditor) Debug.LogWarning("ما فيه ترجمة إنجليزية: " + s);
        return s;
    }

    static string Latin(string digits)
    {
        char[] c = digits.ToCharArray();
        for (int i = 0; i < c.Length; i++) if (c[i] >= '٠' && c[i] <= '٩') c[i] = (char)('0' + (c[i] - '٠'));
        return new string(c);
    }

    static Lang()
    {
        // ألوان المختبر (البوابة تحتاج لون)
        string[,] lab = { { "بنفسجي", "purple" }, { "أخضر", "green" }, { "برتقالي", "orange" }, { "أحمر", "red" }, { "أزرق", "blue" }, { "أصفر", "yellow" } };
        for (int i = 0; i < lab.GetLength(0); i++)
        {
            En["ظهر اللون: " + lab[i, 0] + "! فُتحت البوابة"] = "You made " + lab[i, 1] + "! The gate opened";
            En["البوابة تحتاج إلى لون: " + lab[i, 0] + "! لنرسم أنابيب من الخزانات إلى الدورق ونمزج الألوان"] = "The gate needs " + lab[i, 1] + "! Draw pipes from the tanks to the flask and mix the colors";
        }
        // صندوق الألوان: الباب يحتاج لون أو لونين
        string[,] crayon = { { "الأحمر (2)", "red (2)" }, { "الأزرق (3)", "blue (3)" }, { "الأخضر (4)", "green (4)" }, { "الأسود (1)", "black (1)" } };
        var wanted = new List<string[]>();
        for (int a = 0; a < 4; a++)
        {
            wanted.Add(new[] { crayon[a, 0], crayon[a, 1] });
            for (int b = 0; b < 4; b++)
                if (a != b) wanted.Add(new[] { crayon[a, 0] + " و" + crayon[b, 0], crayon[a, 1] + " and " + crayon[b, 1] });
        }
        foreach (string[] w in wanted)
        {
            En["لون خاطئ! الباب يحتاج إلى " + w[0]] = "Wrong color! The door needs " + w[1];
            En["الباب يحتاج إلى " + w[0] + "... لنختر اللون ونرسم عليه"] = "The door needs " + w[1] + "... pick the color and draw on it";
        }
    }

    static readonly Dictionary<string, string> En = new Dictionary<string, string>
    {
        // ---------- القوائم ----------
        { "قلم رويدا", "Rowaida's Pencil" },
        { "نكمل اللعب", "Keep Playing" },
        { "رجوع", "Back" },
        { "الإعدادات", "Settings" },
        { "الموسيقى", "Music" },
        { "الأصوات", "Sounds" },
        { "صوت رويدا", "Rowaida's Voice" },
        { "حين تروي القصة", "When she tells the story" },
        { "اللغة", "Language" },
        { "من جديد", "Start Over" },
        { "الخروج", "Exit" },
        { "يعمل", "On" },
        { "مطفأ", "Off" },
        { "فكرة اللعبة", "The Story" },
        { "كيف نرسم؟", "How Do We Draw?" },
        { "الأشكال المغلقة", "Closed Shapes" },
        { "الحبر", "Ink" },
        { "أزرار الشاشة", "Screen Buttons" },
        { "نامت رويدا فوجدت نفسها داخل دفتر رسمها! لنساعدها تعبر الصفحات حتى تصل إلى آخر صفحة وتعود إلى غرفتها", "Rowaida fell asleep and woke up inside her sketchbook! Let's help her cross the pages until she reaches the last page and gets back to her room." },
        { "نرسم بإصبعنا على الشاشة... يصبح الخط جسرًا أو طريقًا، ورويدا تمشي عليه وحدها", "Draw with your finger on the screen... the line becomes a bridge or a path, and Rowaida walks on it by herself." },
        { "إذا رسمنا شكلًا مغلقًا مثل الدائرة صار كرة نرميها، أو بالونًا، أو شيئًا آخر حسب المرحلة", "Draw a closed shape like a circle and it becomes a ball to throw, a balloon, or something else, depending on the level." },
        { "حبر القلم محدود! نراقب القلم في أعلى الشاشة، ونرسم بقدر الحاجة فقط", "The pencil's ink is limited! Watch the pencil at the top of the screen and only draw what you need." },
        { "في بداية كل مرحلة تقف رويدا وتشرح لنا المطلوب... لنستمع إليها جيدًا!", "At the start of each level, Rowaida stops and explains what to do... listen carefully!" },
        { "طريقة اللعب", "How to Play" },
        { "التالي", "Next" },
        { "فهمت! لنلعب", "Got it! Let's play" },
        { "القفز (في بعض المراحل)", "Jump (in some levels)" },
        { "لون الحبر (في مراحل الألوان)", "Ink color (in color levels)" },
        { "الإعدادات والاستراحة", "Settings and pause" },
        { "إعادة المرحلة", "Restart Level" },
        { "هيا نلعب!", "Let's Play!" },
        { "الخريطة", "Map" },
        { "المتجر", "Shop" },
        { "الأقلام", "Pencils" },
        { "نكمل المرحلة {0}", "Continue Level {0}" },
        { "الجزء الأول", "Part One" },
        { "الجزء الثاني", "Part Two" },
        { "الجزء الثالث", "Part Three" },
        { "الجزء الرابع", "Part Four" },
        { "مقفلة", "Locked" },
        { "لنبدأ!", "Let's Go!" },
        { "المرحلة {0}", "Level {0}" },
        { "المرحلة {0}: {1}", "Level {0}: {1}" },
        { "المرحلة {0} — {1}", "Level {0} — {1}" },
        { "تُفتح هذه المرحلة بـ {0} كوينز", "This level unlocks for {0} coins" },
        { "تُفتح بعد إنهاء المراحل السابقة، وسعرها {0} كوينز", "Unlocks after you finish the levels before it, and costs {0} coins" },
        { "تُفتح بعد إنهاء المراحل السابقة", "Unlocks after you finish the levels before it" },
        { "فتحها بـ {0} كوينز", "Unlock: {0} coins" },
        { "لنفتح المرحلة {0}", "Unlock Level {0}" },
        { "سعرها {0} كوينز، ومعنا {1}", "It costs {0} coins. You have {1}" },
        { "كل مرحلة ننهيها أول مرة تعطينا {0} كوينز", "Every level you finish for the first time gives you {0} coins" },
        { "الكوينز لا تكفي، نجمعها من المراحل أو من المتجر", "Not enough coins. Earn more in the levels or get some in the shop" },
        { "فتح المرحلة", "Unlock Level" },
        { "{0} كوينز", "{0} Coins" },
        { "تكفي لفتح 20 مرحلة كاملة", "Enough to unlock 20 full levels" },
        { "كل مرحلة جديدة تعطينا {0} كوينز، والإعلان بعدها يعطي {1} زيادة!", "Every new level gives you {0} coins, and watching the ad after it gives {1} more!" },
        { "معنا {0} كوينز", "You have {0} coins" },
        { "لحظة...", "One moment..." },
        { "شراء —", "Buy —" },
        { "شراء — {0} دولار", "Buy — ${0}" },
        { "{0} دولار", "${0}" },
        { "وصلت {0} كوينز (شراء تجريبي)", "{0} coins added (test purchase)" },
        { "وصلت {0} كوينز، شكرًا لكِ!", "{0} coins added. Thank you!" },
        { "لم يكتمل الشراء", "The purchase wasn't completed" },
        { "البدء من جديد؟", "Start Over?" },
        { "ستُمسح كل المراحل المفتوحة والنجوم", "All unlocked levels and stars will be erased" },
        { "نعم، من جديد", "Yes, start over" },
        { "لا", "No" },
        { "صندوق الأقلام", "Pencil Box" },
        { "كل قلم له لمسته السحرية", "Every pencil has its own magic touch" },
        { "شكل الخط", "Line Style" },
        { "هذا قلمي!", "This Is My Pencil!" },
        { "الخط: {0} — {1}", "Line: {0} — {1}" },
        { "استراحة", "Paused" },
        { "متابعة اللعب", "Resume" },
        { "الرئيسية", "Home" },
        { "الدفتر في الانتظار...", "The sketchbook is waiting..." },
        { "شاهد إعلانًا واربح {0}", "Watch an ad, get {0}" },
        { "حصلنا على {0} كوينز إضافية", "You got {0} extra coins" },
        { "اكتملت كل المراحل!", "All Levels Complete!" },
        { "أحسنتِ!", "Well Done!" },
        { "القائمة الرئيسية", "Main Menu" },
        { "الصفحة التالية", "Next Page" },
        { "إنهاء المرحلة", "Finish the level" },
        { "بلا خسارة أي قلب", "Lose no hearts" },
        { "توفير الحبر", "Save ink" },

        // ---------- ألوان وأشكال الأقلام ----------
        { "رصاصي", "Graphite" },
        { "وردي", "Pink" },
        { "برتقالي", "Orange" },
        { "ذهبي", "Gold" },
        { "أخضر", "Green" },
        { "أزرق", "Blue" },
        { "بنفسجي", "Purple" },
        { "بني", "Brown" },
        { "أحمر", "Red" },
        { "أصفر", "Yellow" },
        { "عادي", "Normal" },
        { "منقط", "Dotted" },
        { "لامع", "Glitter" },
        { "نيون", "Neon" },
        { "قوس قزح", "Rainbow" },

        // ---------- شاشة اللعب ----------
        { "الهواء", "Air" },
        { "الريح", "Wind" },
        { "الصفحة تنطوي بعد {0}", "The page folds in {0}" },
        { "انطوت الصفحة!", "The page folded!" },
        { "بقي قلب واحد!", "One heart left!" },
        { "بقي قلبان", "Two hearts left" },
        { "نفدت كل القلوب!", "Out of hearts!" },
        { "نعيد المرحلة من البداية", "Let's start the level again" },
        { "تخطي المشهد  «", "Skip Scene  »" },
        { "لنقفز! نضغط زر القفز الوردي", "Let's jump! Press the pink jump button" },
        { "من رسوماتي", "My drawing" },

        // ---------- الإعلانات (بالمحرر بس) ----------
        { "إعلان بين المراحل", "Between-levels ad" },
        { "إعلان المكافأة", "Reward ad" },
        { "(بالجوال يطلع هنا إعلان حقيقي من يونتي)", "(On a phone, a real Unity ad shows here)" },

        // ---------- القصة: البداية والنهاية ----------
        { "كنت أحب الرسم... كل ليلة أرسم في دفتري قبل أن أنام", "I loved drawing... every night, I drew in my sketchbook before bed." },
        { "لكن تلك الليلة... كنت متعبة جدا", "But that night... I was so tired." },
        { "نمت وأنا أمسك قلمي... وسقط الدفتر", "I fell asleep holding my pencil... and the sketchbook fell." },
        { "وفجأة... صار الدفتر يضيء!", "Suddenly... the sketchbook started to glow!" },
        { "ووجدت نفسي داخل دفتر رسمي...", "And I found myself inside my sketchbook..." },
        { "الطريق الوحيد للعودة إلى غرفتي... هو الوصول إلى آخر صفحة", "The only way back to my room... is to reach the last page." },
        { "استيقظت... وأنا في غرفتي! والشمس مشرقة", "I woke up... in my room! And the sun was shining." },
        { "هل كان كله حلما؟", "Was it all a dream?" },
        { "لكن الدفتر...", "But the sketchbook..." },
        { "فيه كل الرسومات التي أعادتني!", "has all the drawings that brought me home!" },
        { "شكرا على رسم طريقي إلى البيت", "Thank you for drawing my way home." },
        { "النهاية", "The End" },

        // ---------- أسماء المراحل وشرحها ----------
        { "الصفحات الأولى", "The First Pages" },
        { "أول صفحة في الدفتر... لنرسم جسرًا فوق الحفرة لتعبر رويدا", "The first page of the sketchbook... let's draw a bridge over the gap so Rowaida can cross." },
        { "صفحات المطر", "The Rainy Pages" },
        { "المطر يمسح الرسم بعد قليل... يجب أن نسرع!", "The rain washes drawings away after a while... we have to hurry!" },
        { "تحت المطر", "Under the Rain" },
        { "الصفحة التالية أعلى... لنرسم طريقًا مائلًا يصعد برويدا إليها!", "The next page is higher... let's draw a slope that takes Rowaida up to it!" },
        { "صفحات الريح", "The Windy Pages" },
        { "الريح تدفع الرسومات إلى الخلف... لنرسم الجسر أطول من الأمام!", "The wind pushes drawings backward... let's draw the bridge longer at the front!" },
        { "العاصفة", "The Storm" },
        { "الريح تغيّر اتجاهها... لننتبه للسهم في الأعلى!", "The wind keeps changing direction... watch the arrow at the top!" },
        { "صفحات الخربشات", "The Scribble Pages" },
        { "رسوماتي القديمة استيقظت! لنخربش عليها لتُمسح", "My old drawings woke up! Scribble over them to erase them." },
        { "خربشات أكثر", "More Scribbles" },
        { "الوحوش تأكل الجسور... لنمسحها قبل أن نرسم!", "The monsters eat bridges... let's erase them before we draw!" },
        { "صفحة الألوان", "The Color Page" },
        { "٢ أحمر يقفز  •  ٣ أزرق يزلق  •  ٤ أخضر يطيل", "2 Red bounces  •  3 Blue slides  •  4 Green stretches" },
        { "الأرض الطائرة", "Floating Islands" },
        { "أربع أرضيات تطير في سماء الدفتر... لنرسم جسرًا بينها!", "Four islands float in the sketchbook sky... let's draw bridges between them!" },
        { "الصفحة المقلوبة", "The Upside-Down Page" },
        { "انقلبت الجاذبية! لنرسم طرقًا تتخطى العقبات... ونمسح الوحوش", "Gravity flipped! Let's draw paths over the obstacles... and erase the monsters." },
        { "صفحة الحبر المنسكب", "The Spilled Ink Page" },
        { "الحبر يرتفع! لنرسم سلّمًا تصعد عليه رويدا... وقاربًا كبيرًا يطفو", "The ink is rising! Let's draw stairs for Rowaida to climb... and a big boat that floats." },
        { "الصفحة المتلاشية", "The Vanishing Page" },
        { "الأرض تختفي أمام رويدا... لنرسم خطًّا ونعبر!", "The ground is disappearing in front of Rowaida... let's draw a line and cross!" },
        { "زيزو", "Zizo" },
        { "هيا لنقفز! الجليد يتكسر، والكرات تطير، والانهيار في الخلف", "Let's jump! The ice is cracking, balls are flying, and an avalanche is coming from behind!" },
        { "صفحة الفضاء", "The Space Page" },
        { "لننتبه للدوائر الحمراء! نرسم مظلة من النيازك، وسقفًا يحمي من الثقب الأسود", "Watch out for the red circles! Draw an umbrella against the meteors and a roof against the black hole." },
        { "صفحة البالونات", "The Balloon Page" },
        { "لنرسم دائرة تصبح بالونًا... ونحذر من الدبابيس!", "Draw a circle to make a balloon... and watch out for the pins!" },
        { "صفحة النار", "The Fire Page" },
        { "التنين ينفث النار! لنرسم درعًا فوق رأس رويدا يصدّها", "The dragon breathes fire! Draw a shield over Rowaida's head to block it." },
        { "صفحة البوابات", "The Gate Page" },
        { "لنرسم كرة فوق البوابة الزرقاء... تسقط على الزر وتفتح الباب!", "Draw a ball above the blue gate... it falls on the button and opens the door!" },
        { "صفحة الوادي", "The Valley Page" },
        { "طائر الورق يخطف الخطوط! لنتركه يمر ثم نرسم خلفه", "The paper bird snatches lines! Let it pass, then draw behind it." },
        { "الممحاة الكبيرة", "The Big Eraser" },
        { "الممحاة تلاحق رويدا وتمسح كل شيء خلفها... لا وقت للتوقف!", "The eraser is chasing Rowaida and erasing everything behind her... no time to stop!" },
        { "صفحة اليراعات", "The Firefly Page" },
        { "الفانوس ينطفئ! لنرسم خطًّا يلمس اليراعات فتملؤه، والظلال تخاف من الضوء", "The lantern is going out! Draw a line through the fireflies to refill it... the shadows are afraid of the light." },
        { "الصفحة الأخيرة", "The Last Page" },
        { "آخر صفحة في الدفتر... كل الذكريات هنا، لنعد إلى البيت!", "The last page of the sketchbook... all the memories are here. Let's go home!" },
        { "المد والجزر", "The Tide" },
        { "رويدا تنتظر الجزر لتعبر الرمل... والرمل الطويل نرسم له ممشى", "Rowaida waits for low tide to cross the sand... and for the long stretch, we draw a walkway." },
        { "أعماق البحر", "The Deep Sea" },
        { "لنرسم جسورًا فوق الحفر... تنزلق الفقاعات تحتها وتملأ هواء رويدا", "Let's draw bridges over the gaps... bubbles slide under them and refill Rowaida's air." },
        { "الأخطبوط العملاق", "The Giant Octopus" },
        { "الأخطبوط جائع! لنرمِ له كرة يأكلها فينام... ثم نرسم الجسر", "The octopus is hungry! Throw it a ball to eat so it falls asleep... then draw the bridge." },
        { "الدوامة", "The Whirlpool" },
        { "الدوامة تبتلع أي خط يقترب منها... لنرسم الجسر مقوّسًا فوقها!", "The whirlpool swallows any line that gets close... let's draw the bridge curving over it!" },
        { "المنارة", "The Lighthouse" },
        { "لنرسم قاربًا في البحر يوصل رويدا إلى المنارة... فالقرش يخاف من ضوئها!", "Let's draw a boat to take Rowaida to the lighthouse... the shark is afraid of its light!" },
        { "المسطرة", "The Ruler" },
        { "لنرمِ كرة على طرف المسطرة لتعتدل... ونقفز فوق الأقلام، ونحذر من الدبّاسة!", "Throw a ball on the end of the ruler to level it... jump over the pencils, and watch out for the stapler!" },
        { "صندوق الألوان", "The Crayon Box" },
        { "كل باب يحتاج لونه! لنختر اللون بالأرقام ونرسم على الباب", "Each door needs its own color! Pick the color with the numbers and draw on the door." },
        { "الآلة الحاسبة", "The Calculator" },
        { "لنرمِ كرات على الأرقام... إذا ساوى مجموعها رقم الشاشة فُتح الباب", "Throw balls at the numbers... if they add up to the number on the screen, the door opens." },
        { "المقص", "The Scissors" },
        { "المقص يقص الجسور! لنرمِ كرة بين شفرتيه فتعلقه... ثم نرسم الجسر", "The scissors cut bridges! Throw a ball between the blades to jam them... then draw the bridge." },
        { "المقلمة", "The Pencil Case" },
        { "رويدا داخل المقلمة! لنغلق السحّاب، ونحذر من الصمغ، فالبرّاية جائعة", "Rowaida is inside the pencil case! Close the zipper and watch out for the glue... the sharpener is hungry!" },
        { "الساعة", "The Clock" },
        { "عقرب الساعة يدور! لنرسم خطًّا تحت مكانه حين يستوي... فيتوقف عليه وتعبر رويدا", "The clock hand is turning! Draw a line under it when it's flat... it stops on the line and Rowaida can cross." },
        { "الساعة الرملية", "The Hourglass" },
        { "لنلمس الساعة الرملية بخط... فتنقلب ويملأ الرمل الحفرة", "Touch the hourglass with a line... it flips over and the sand fills the gap." },
        { "التروس", "The Gears" },
        { "الترس الأزرق يدور... لنرسم خطًّا يصله بالترس الآخر لينزل الجسر", "The blue gear is turning... draw a line connecting it to the other gear to lower the bridge." },
        { "المنبه", "The Alarm Clock" },
        { "القطط نائمة في الطريق! لنرمِ كرة على زر المنبه ليرنّ فتستيقظ", "The cats are asleep on the path! Throw a ball at the alarm button so it rings and wakes them up." },
        { "الليل والنهار", "Day and Night" },
        { "لنلمس الشمس والقمر بخط ليتغير الوقت... لكل صفحة وقتها", "Touch the sun and the moon with a line to change the time... each page has its own time." },
        { "البيانو", "The Piano" },
        { "لنعزف اللحن! نرمي كرات على المفاتيح بترتيب ألوان النوتة", "Let's play the tune! Throw balls at the keys in the order of the note colors." },
        { "الطبول", "The Drums" },
        { "الجدار عالٍ! لنرسم دائرة في قاع الحفرة تصبح طبلًا... تقفز عليه رويدا", "The wall is high! Draw a circle at the bottom of the pit to make a drum... Rowaida bounces on it." },
        { "النوتات", "The Notes" },
        { "لنجمع النوتات! نرسم طريقًا مثل التلة يوصل رويدا إليها", "Let's collect the notes! Draw a hill-shaped path that takes Rowaida to them." },
        { "الأوتار", "The Strings" },
        { "الوتر يهتز! لنرسم خطًّا يلمسه ليسكت ويصبح جسرًا", "The string is vibrating! Draw a line that touches it to quiet it, and it becomes a bridge." },
        { "اللحن الأخير", "The Final Tune" },
        { "لنرسم النوتات في أماكنها المنقطة ليكتمل اللحن... ونحذر من المترونوم والسماعة والجمهور المشاغب!", "Draw the notes in their dotted spots to complete the tune... and watch out for the metronome, the speaker, and the noisy crowd!" },
        { "مدخل الغابة", "The Forest Entrance" },
        { "في هذه المرحلة نقفز بأنفسنا! لنقطع الشبكة، ونزرع بذرة، ونقفز فوق الجذوع", "In this level, you do the jumping! Cut the web, plant a seed, and jump over the logs." },
        { "بستان الفطر", "The Mushroom Grove" },
        { "لنوقف القنفذ، ونتأرجح على الأغصان، ونحذر من البومة، ونوقظ الفطرة!", "Stop the hedgehog, swing on the branches, watch out for the owl, and wake up the mushroom!" },
        { "ضفاف النهر", "The Riverbank" },
        { "لنصطد جذعًا بالحبل، ونطعم السلاحف خسًّا، ونقفز على الحجارة حين تطفو!", "Catch a log with the rope, feed the turtles lettuce, and jump on the stones when they float!" },
        { "خلية النحل", "The Beehive" },
        { "النحل يحرس الطريق! لنرسم دائرة تصبح وردة تلهيه... ونحذر من العسل", "The bees are guarding the path! Draw a circle to make a flower that distracts them... and watch out for the honey." },
        { "قدر الطبخ", "The Cooking Pot" },
        { "لنتبع الوصفة، ونغلق الثلاجة، ونحذر من الفلفل والخبز المحمص والنار!", "Follow the recipe, close the fridge, and watch out for the peppers, the toast, and the fire!" },
        { "نهر الشوكولاتة", "The Chocolate River" },
        { "لنرفع العصا، ونرسم المارشملو في الشوكولاتة، وندير ذراع آلة العلكة!", "Raise the candy cane, draw marshmallows in the chocolate, and pull the gumball machine's lever!" },
        { "الفرن والمقلاة", "The Oven and the Pan" },
        { "لنقلب الفطيرة، ونعكس السير، ونجعل باب الفرن جسرًا!", "Flip the pancake, reverse the conveyor belt, and turn the oven door into a bridge!" },
        { "قلعة الحلوى", "The Candy Castle" },
        { "لنلهِ الدببة، ونصعد الكعكة ونطفئ الشموع، ونكمل حتى باب القلعة!", "Distract the bears, climb the cake, blow out the candles, and make it to the castle door!" },
        { "أنابيب الألوان", "The Color Pipes" },
        { "لنرسم أنابيب من الخزانات إلى الدورق ونمزج الألوان... ونطعم السلايم ونوصل الكهرباء!", "Draw pipes from the tanks to the flask and mix the colors... feed the slime and connect the power!" },
        { "التجميد والذوبان", "Freeze and Melt" },
        { "لنجمّد الماء، ونذيب الثلج بالنار، ونرسم مظلة من الثلج المتساقط!", "Freeze the water, melt the ice with fire, and draw an umbrella against the falling snow!" },
        { "التكبير والتصغير", "Grow and Shrink" },
        { "لنصغّر رويدا لتدخل بيت الفأر، ونكبّر الرسمة لتصبح جسرًا!", "Shrink Rowaida so she fits into the mouse house, and grow a drawing into a bridge!" },
        { "البركان المجنون", "The Crazy Volcano" },
        { "فشلت تجربة البركان! لنصعد قبل أن تصل الرغوة... والروبوت سيساعد", "The volcano experiment went wrong! Climb before the foam reaches you... the robot will help." },
        { "عالم البكسل", "Pixel World" },
        { "دخلت رويدا لعبة فيديو قديمة! كل خط نرسمه يصبح بكسلات", "Rowaida is inside an old video game! Every line you draw turns into pixels." },
        { "الكود", "The Code" },
        { "لنبرمج المنصة بالأسهم، ونمسك الحشرة، ونقطع الحلقة التي لا تتوقف", "Program the platform with arrows, catch the bug, and cut the loop that never stops." },
        { "لعبة الأرقام", "The Number Game" },
        { "لنعدّ جيدًا! كل بوابة وميزان وآلة حاسبة تحتاج الرقم الصحيح بالضبط", "Count carefully! Every gate, scale, and calculator needs exactly the right number." },
        { "الزعيم الأخير", "The Final Boss" },
        { "فيروس عملاق يلاحق رويدا! لنرسم جدران حماية، ونغلق الإعلانات، ونحفظ اللعبة", "A giant virus is chasing Rowaida! Draw firewalls, close the pop-up ads, and save the game." },
        { "بين الغيوم", "Among the Clouds" },
        { "نامت رويدا وبدأ الحلم... كل شيء هنا ناعم كالقطن", "Rowaida fell asleep and the dream began... everything here is as soft as cotton." },
        { "سماء النجوم", "The Starry Sky" },
        { "صار الحلم ليلًا... لنصل النجوم ونوقظ القمر", "The dream turned into night... let's connect the stars and wake up the moon." },
        { "الكابوس والنور", "Nightmare and Light" },
        { "صار الحلم كابوسًا! لنرسم مصابيح تطرد الظل، ونوقظ رويدا قبل أن يصل إليها", "The dream became a nightmare! Draw lamps to chase away the shadow, and wake Rowaida up before it reaches her." },

        // ---------- تعليمات داخل المراحل ----------
        // الحلم
        { "بوينغ!", "Boing!" },
        { "جدار عالٍ! لنرسم خطًّا على الأرض أمامه... يصبح غيمة تقفز برويدا إلى الأعلى", "A tall wall! Draw a line on the ground in front of it... it becomes a cloud that bounces Rowaida up." },
        { "ريح نعسانة تدفع رويدا إلى الخلف! لنرسم لها وسادة (شكلًا مغلقًا) بجانب رأسها لتنام", "A sleepy wind is pushing Rowaida back! Draw it a pillow (a closed shape) next to its head so it falls asleep." },
        { "نامت الريح... ششش", "The wind fell asleep... shhh" },
        { "لنعدّ الخراف! نرسم {0} دوائر... كل واحدة تصبح خروفًا يصطف في الحفرة", "Let's count sheep! Draw {0} circles... each one becomes a sheep that lines up in the pit." },
        { "كوكبة! صارت النجوم جسرًا من نور", "A constellation! The stars became a bridge of light." },
        { "نجوم فوق الحفرة! لنرسم خطًّا يمر على كل النجوم ويصلها", "Stars over the gap! Draw a line through all the stars to connect them." },
        { "استيقظ القمر وأضاء الطريق!", "The moon woke up and lit the way!" },
        { "ظلام! رويدا خائفة... لنرسم خطًّا يلمس القمر النائم ويوقظه", "It's dark! Rowaida is scared... draw a line that touches the sleeping moon to wake it up." },
        { "انتفخ الفانوس وأضاء!", "The lantern puffed up and lit up!" },
        { "سلة فانوس! لنرسم دائرة فوقها... تصبح فانوسًا يطير ويرفع رويدا", "A lantern basket! Draw a circle above it... it becomes a flying lantern that lifts Rowaida." },
        { "مصباح! الظل يخاف من النور", "A lamp! The shadow is afraid of the light." },
        { "ابتسم! وصار قطًّا لطيفًا لا يخيف", "It smiled! Now it's a cute cat, not scary at all." },
        { "لنرسم له ابتسامة (خطًّا مقوسًا إلى الأسفل)", "Draw it a smile (a line curving down)" },
        { "وحش في الظلام! لنرسم ابتسامة على وجهه كي لا يخيف", "A monster in the dark! Draw a smile on its face so it isn't scary." },
        { "رنّ المنبه! استيقظت رويدا من الحلم", "The alarm rang! Rowaida woke up from the dream." },
        { "منبه عملاق! لنرسم خطًّا يضرب زره الأحمر في الأعلى ويوقظ رويدا", "A giant alarm clock! Draw a line that hits the red button on top to wake Rowaida up." },
        // الصفحة الأخيرة
        { "حبر قلم رويدا يكاد ينفد! لنرسم خطًّا يمر على قطرات الحبر الطائرة ويملأ القلم", "Rowaida's pencil is almost out of ink! Draw a line through the flying ink drops to refill it." },
        { "صارت الذكريات جسرًا!", "The memories became a bridge!" },
        { "صور من الرحلة! لنرسم خطًّا يمر على الصور الثلاث فتصبح جسرًا", "Photos from the journey! Draw a line through all three photos to turn them into a bridge." },
        { "لنرسم الخط وهي مستلقية على الحفرة!", "Draw the line while it's lying over the gap!" },
        { "علامة الكتاب ثبتت الصفحة!", "The bookmark held the page down!" },
        { "صفحة عملاقة تنقلب! لنرسم خطًّا من الأعلى إلى الأسفل يقطعها وهي مستلقية... فيصبح علامة كتاب تثبتها", "A giant page is turning! While it's lying flat, draw a line from top to bottom across it... it becomes a bookmark that holds it down." },
        { "انطوت الورقة وصارت طائرة!", "The paper folded into a plane!" },
        { "وادٍ كبير! لنرسم شكلًا مغلقًا فوق المنصة... فينطوي ويصبح طائرة ورقية تحمل رويدا", "A big valley! Draw a closed shape above the platform... it folds into a paper plane that carries Rowaida." },
        { "شكرا يا رويدا على الرحلة!", "Thank you for the journey, Rowaida!" },
        { "بيت رويدا", "Rowaida's Home" },
        // الغابة
        { "لنرسم دائرة (بذرة) ونتركها تسقط في التراب... ولنرَ ماذا ينمو!", "Draw a circle (a seed) and let it fall into the soil... let's see what grows!" },
        { "الفطرة نائمة! لنلمسها بخط لتستيقظ وتقفز برويدا", "The mushroom is asleep! Touch it with a line to wake it up so it bounces Rowaida." },
        { "النهر يجري! لنرسم خطًّا على النهر يصبح حبلًا يصطاد جذعًا", "The river is flowing! Draw a line on the river to make a rope that catches a log." },
        { "آه! لسعت النحلة رويدا", "Ouch! A bee stung Rowaida." },
        { "النحل يحرس الطريق! لنرسم دائرة تصبح وردة تلهيه", "The bees are guarding the path! Draw a circle to make a flower that distracts them." },
        { "شجرة يابسة! لندفعها بخط من الأعلى... فتسقط وتصبح جسرًا", "A dry tree! Push it with a line from the top... it falls and becomes a bridge." },
        { "شبكة عنكبوت تسد الطريق! لنقطعها بخط", "A spider web is blocking the way! Cut it with a line." },
        { "نامت البومة! يمكن العبور الآن", "The owl is asleep! You can cross now." },
        { "رأت البومة رويدا! لننتظر حتى تغمض عينيها", "The owl saw Rowaida! Let's wait until it closes its eyes." },
        { "البومة تراقب! لننتظر حتى تغمض عينيها... أو نرسم دائرة على عينيها", "The owl is watching! Wait until it closes its eyes... or draw a circle over its eyes." },
        { "المعكرونة قصيرة! لنرسم خطًّا تحتها يطيلها ونتأرجح", "The noodle is too short! Draw a line under it to make it longer, then swing." },
        { "لننتظر حتى تصل الكرمة!", "Let's wait for the vine to reach us!" },
        { "الكرمة قصيرة! لنرسم خطًّا تحتها يطيلها", "The vine is too short! Draw a line under it to make it longer." },
        { "قنفذ يتدحرج! لنرسم خطًّا أمامه يوقفه", "A rolling hedgehog! Draw a line in front of it to stop it." },
        { "بركة سلاحف! لنرسم دائرة في الماء تصبح خسًّا... فتتجمع السلاحف وتصبح جسرًا", "A turtle pond! Draw a circle in the water to make lettuce... the turtles gather and become a bridge." },
        { "الحجارة تطفو وتغطس! تقفز رويدا عليها حين تطفو... والماء يبتلع الخطوط", "The stones float and sink! Rowaida jumps on them when they float... and the water swallows lines." },
        { "جذع على الطريق! نضغط زر القفز الوردي فتقفز رويدا", "A log in the way! Press the pink jump button to make Rowaida jump." },
        // عالم الألعاب
        { "جُمعت كل العملات! انكسر الجدار", "All the coins are collected! The wall broke." },
        { "جدار طوب! لنرسم خطًّا يمر على العملات الخمس ويجمعها", "A brick wall! Draw a line through the five coins to collect them." },
        { "حُبس الوحش!", "The monster is trapped!" },
        { "وحش بكسلات! لنرسم صندوقًا (شكلًا مغلقًا) يسقط عليه ويحبسه", "A pixel monster! Draw a box (a closed shape) that falls on it and traps it." },
        { "تكسير الطوب! لنرسم خطًّا فوق الحفرة: يصبح مضربًا يردّ الكرة إلى الطوب، وجسرًا لرويدا", "Brick breaker! Draw a line over the gap: it becomes a paddle that bounces the ball at the bricks, and a bridge for Rowaida." },
        { "لنرسم سهمًا هنا", "Draw an arrow here" },
        { "لنبرمج المنصة! نرسم سهمًا على الشاشة: للأعلى ترتفع، ولليمين تتحرك", "Let's program the platform! Draw an arrow on the screen: up makes it rise, right makes it move." },
        { "أُمسكت الحشرة! وأُصلح الخطأ", "Bug caught! The error is fixed." },
        { "أفلتت! لنحط الحشرة كلها بالدائرة", "It got away! Circle the whole bug." },
        { "حشرة برمجية أفسدت الطريق! لنحطها بدائرة كي نمسكها", "A software bug broke the path! Circle it to catch it." },
        { "انقطعت الحلقة! توقف الباب مفتوحًا", "The loop is broken! The door stayed open." },
        { "باب يُفتح ويُغلق بلا توقف! لنرسم خطًّا يقطع الحلقة البرتقالية", "A door that opens and closes nonstop! Draw a line that cuts the orange loop." },
        { "لنرسم {0} خطوط هنا", "Draw {0} lines here" },
        { "صحيح! {0} بالضبط", "Correct! Exactly {0}" },
        { "أكثر من اللازم! المطلوب {0} بالضبط، لنحاول من جديد", "Too many! We need exactly {0}. Let's try again." },
        { "بوابة الرقم {0}! لنرسم داخل المربع {1} خطوط بالضبط", "Number {0} gate! Draw exactly {1} lines inside the box." },
        { "أكثر من اللازم! المطلوب {0} بالضبط", "Too many! We need exactly {0}." },
        { "توازن الميزان! نزل الجسر", "The scale is balanced! The bridge came down." },
        { "ميزان الأرقام! لنضع في الكفة اليمنى {0} دوائر ليتوازن وينزل الجسر", "The number scale! Put {0} circles on the right pan to balance it and lower the bridge." },
        { "صحيح! الآلة تطبع الآن", "Correct! The machine is printing now." },
        { "خطأ! لنجرب جوابًا آخر", "Wrong! Let's try another answer." },
        { "آلة حاسبة! لنحِط الجواب الصحيح بدائرة فتطبع درجًا", "A calculator! Circle the right answer and it prints a step." },
        { "جدار حماية! توقف الفيروس", "Firewall! The virus stopped." },
        { "مبروك!!", "You won!!" },
        { "جائزة كبرى", "Grand Prize" },
        { "أُغلق!", "Closed!" },
        { "إعلان مزعج سدّ الطريق! لنرسم خطًّا على زر X الأحمر لإغلاقه", "An annoying pop-up ad is blocking the way! Draw a line on the red X button to close it." },
        { "حفظ", "Save" },
        { "حُفظت اللعبة! ومُسح الفيروس", "Game saved! The virus is erased." },
        { "لنحط القرص كله بالدائرة", "Circle the whole disk" },
        { "تم الحفظ", "Saved" },
        { "زر الحفظ! لنحط القرص بدائرة لحفظ اللعبة", "The save button! Circle the disk to save the game." },
        // المطبخ
        { "ليس هذا! لننظر إلى بطاقة الوصفة ونتبع الترتيب", "Not that one! Look at the recipe card and follow the order." },
        { "لذيذ! الحساء جاهز", "Yummy! The soup is ready." },
        { "لنتبع الوصفة! نلمس المكونات بخط بترتيب البطاقة نفسه", "Follow the recipe! Touch the ingredients with a line in the same order as the card." },
        { "مغناطيس! أُغلقت الثلاجة", "Magnet! The fridge is closed." },
        { "الثلاجة تُخرج ريحًا باردة! لننتظر حتى تُغلق... أو نرسم دائرة على بابها", "The fridge is blowing cold wind! Wait until it closes... or draw a circle on its door." },
        { "نهر شوكولاتة! الخطوط تغرق... لنرسم دوائر في الأماكن المنقطة تصبح مارشملو", "A chocolate river! Lines sink in it... draw circles in the dotted spots to make marshmallows." },
        { "آلة علكة! لنلمس ذراعها الحمراء بخط لتملأ الحفرة", "A gumball machine! Touch its red lever with a line to fill the gap." },
        { "بوابة عصا الحلوى! لنلمسها بخط لترتفع", "A candy cane gate! Touch it with a line to raise it." },
        { "لنلمس المقلاة بخط فتقلب الفطيرة... وتصبح جسرًا!", "Touch the pan with a line to flip the pancake... it becomes a bridge!" },
        { "صار الفرن مفتوحًا دائمًا", "The oven stays open now" },
        { "باب الفرن يصبح جسرًا حين يُفتح! لننتظر... أو نلمس زر الفرن بخط", "The oven door becomes a bridge when it opens! Wait... or touch the oven button with a line." },
        { "السير يعيد رويدا إلى الخلف! لنلمس المفتاح الذي بعده بخط لينعكس", "The conveyor belt is carrying Rowaida back! Touch the switch after it with a line to reverse it." },
        { "ذهبت الدببة إلى الحلوى! لنرسم الجسر بسرعة", "The bears went for the candy! Quick, draw the bridge." },
        { "أكل الدب الخط! لنرسم أولًا دائرة حلوى تلهيها", "The bear ate the line! First, draw a candy circle to distract them." },
        { "دببة الجيلي تأكل الخطوط! لنرسم دائرة تصبح حلوى تلهيها... ثم الجسر", "The jelly bears eat lines! Draw a circle to make candy that distracts them... then the bridge." },
        { "شمعة في الطريق! لنطفئها بخط عليها", "A candle in the way! Draw a line on it to blow it out." },
        // المختبر
        { "أوه! صار اللون طينيًّا... هذا اللون ليس من الخلطة. لنُزل أنبوبه ونحاول مرة أخرى", "Oh no! The color turned muddy... that color isn't part of the mix. Remove its pipe and try again." },
        { "شبع وكبر! لنرَ ماذا سيفعل", "It's full and it grew! Let's see what it does." },
        { "سلايم جائع! لنرسم له دوائر يأكلها... فيكبر ويساعد", "A hungry slime! Draw circles for it to eat... it grows and helps." },
        { "وصلت الكهرباء!", "The power is on!" },
        { "المصعد متوقف! لنرسم سلكًا من طرف البطارية إلى المحرك في الأعلى", "The elevator is stopped! Draw a wire from the battery to the motor at the top." },
        { "أذاب الحمض الخط! لنرسم دوائر صودا... تفور وتصبح رغوة", "The acid melted the line! Draw soda circles... they fizz and turn into foam." },
        { "بركة حمض! الخطوط تذوب... لنرسم دوائر صودا تفور وتصبح رغوة", "An acid pool! Lines melt in it... draw soda circles that fizz into foam." },
        { "ليزر! لنرسم خطًّا يقطع الشعاع الأحمر... فيرتد إلى الهدف ويفتح الباب", "A laser! Draw a line across the red beam... it bounces to the target and opens the door." },
        { "مرتفع أكثر من اللازم! لننتظر حتى تنزل الرغوة درجة...", "Too high! Let's wait for the foam to go down a step..." },
        { "برج الرغوة! كل قطرة (دائرة) فوقه ترفعه درجة... لنتوقف عند العلامة الصفراء بالضبط", "A foam tower! Each drop (circle) on top raises it one step... stop exactly at the yellow mark." },
        { "تجمّد الماء! لنعبر بسرعة قبل أن تذيبه المدفأة", "The water froze! Cross quickly before the heater melts it." },
        { "ماء! لنرسم خطًّا من مسدس التجميد إلى الماء... فيتجمد ويصبح جسرًا", "Water! Draw a line from the freeze gun to the water... it freezes into a bridge." },
        { "الثلج يذوب!", "The ice is melting!" },
        { "جدار ثلج! لنرسم خطًّا من نار الموقد إلى الجدار... فيسخن الخط ويذيب الثلج", "An ice wall! Draw a line from the stove's flame to the wall... the line heats up and melts the ice." },
        { "سقف ثلج! لنرسم خطًّا فوق رأسها... مظلة تحميها من الثلج المتساقط", "An icy ceiling! Draw a line over her head... an umbrella to protect her from the falling snow." },
        { "آه! لنرسم خطًّا فوقها يحميها", "Ouch! Draw a line above her to protect her." },
        { "صغُرت رويدا! يمكنها الآن دخول بيت الفأر", "Rowaida shrank! Now she can go into the mouse house." },
        { "عادت إلى حجمها!", "She's back to her normal size!" },
        { "شعاع التصغير متوقف! لنرسم دائرة تسقط على الزر الأحمر فوق الرف", "The shrink ray is off! Draw a circle that falls on the red button above the shelf." },
        { "حفرة كبيرة! لنرسم خطًّا صغيرًا داخل الشعاع الأخضر... فيكبر ويصبح جسرًا", "A big gap! Draw a small line inside the green beam... it grows into a bridge." },
        { "ثقُل الدلو!", "The bucket got heavy!" },
        { "مصعد بحبل! لنرسم دائرة تسقط في الدلو الأزرق... فيثقل وينزل وترتفع رويدا", "A rope elevator! Draw a circle that drops into the blue bucket... it gets heavy and goes down, and Rowaida goes up." },
        { "انفجر البركان! الرغوة ترتفع... لنرسم منحدرًا (خطًّا مائلًا) لكل درجة بسرعة", "The volcano erupted! The foam is rising... quickly draw a ramp (a slanted line) for each step." },
        { "قلّد الروبوت الرسمة للدرجة التالية!", "The robot copied the drawing for the next step!" },
        { "دورق سينفجر! لننتظر حتى يفرقع ثم نعبر", "A flask is about to burst! Wait until it pops, then cross." },
        // الموسيقى
        { "لحن جميل! فُتح الباب", "Beautiful tune! The door opened." },
        { "نوتة خاطئة! لنبدأ اللحن من أوله", "Wrong note! Let's start the tune from the beginning." },
        { "صار طبلًا!", "It became a drum!" },
        { "الجدار عالٍ! لنرسم دائرة في قاع الحفرة تصبح طبلًا تقفز عليه رويدا", "The wall is high! Draw a circle at the bottom of the pit to make a drum Rowaida can bounce on." },
        { "فاتت نوتة! لنرسم طريقًا يوصل إليها", "We missed a note! Draw a path that reaches it." },
        { "الوتر يهتز! لنرسم خطًّا يلمسه ليسكت", "The string is vibrating! Draw a line that touches it to quiet it." },
        { "عاد يهتز! لنسكته مرة أخرى", "It's vibrating again! Let's quiet it once more." },
        { "السطر يحتاج نوتات لا خطوطًا! لنرسم دائرة في كل مكان منقط", "The staff needs notes, not lines! Draw a circle in each dotted spot." },
        { "لنرسم نوتة (دائرة) في كل مكان منقط ليكتمل اللحن", "Draw a note (a circle) in each dotted spot to complete the tune" },
        { "مفاتيح البيانو تصعد وتنزل! تمشي رويدا عليها حين تصبح بالارتفاع نفسه", "The piano keys go up and down! Rowaida walks on them when they're at the same height." },
        // المدرسة
        { "لنرمِ كرة على طرف المسطرة الآخر... كلما كانت أبعد كانت أقوى", "Throw a ball on the other end of the ruler... the farther out it lands, the stronger the launch." },
        { "صحيح! {0}", "Correct! {0}" },
        { "أكثر من {0}! لنحاول مرة أخرى", "More than {0}! Let's try again." },
        { "لنرمِ كرات على الأرقام... يجب أن يكون مجموعها {0}", "Throw balls at the numbers... they must add up to {0}." },
        { "علِق المقص! لنرسم الجسر", "The scissors are stuck! Draw the bridge." },
        { "المقص يقص الجسور! لنرمِ كرة بين شفرتيه", "The scissors cut bridges! Throw a ball between the blades." },
        { "لنرسم الجسر", "Draw the bridge" },
        { "السحّاب مفتوح! لنرسم خطًّا من المقبض حتى آخر الحفرة ليُغلق", "The zipper is open! Draw a line from the pull tab to the end of the gap to close it." },
        { "أكلت البرّاية الخط! لنرسم الجسر بسرعة وهي تبري", "The sharpener ate the line! Quick, draw the bridge while it's sharpening." },
        { "البرّاية جائعة! لنرسم خطًّا تأكله، وبينما تبري نرسم الجسر", "The sharpener is hungry! Draw a line for it to eat, and while it's sharpening, draw the bridge." },
        { "عسل! ستلتصق رويدا به... لنرسم خطًّا فوقه تمشي عليه", "Honey! Rowaida will get stuck in it... draw a line over it for her to walk on." },
        { "صمغ! ستلتصق رويدا به... لنرسم خطًّا فوقه تمشي عليه", "Glue! Rowaida will get stuck in it... draw a line over it for her to walk on." },
        // البحر
        { "العقرب قادم ليلدغ! لنرسم خطًّا عليه يقلبه على ظهره", "A scorpion is coming to sting! Draw a line on it to flip it onto its back." },
        { "السلطعون قادم ليهجم! لنرسم خطًّا عليه يقلبه على ظهره", "A crab is coming to attack! Draw a line on it to flip it onto its back." },
        { "آه! لدغ العقرب رويدا... ستمشي ببطء قليلًا", "Ouch! The scorpion stung Rowaida... she'll walk slowly for a bit." },
        { "آه! قرص السلطعون رويدا", "Ouch! The crab pinched Rowaida." },
        { "الهواء يكاد ينفد! لنأخذ فقاعة", "Almost out of air! Let's grab a bubble." },
        { "شبع ونام! لنرسم الجسر", "It's full and asleep! Draw the bridge." },
        { "الأخطبوط جائع! لنرمِ له كرة في الماء", "The octopus is hungry! Throw it a ball in the water." },
        { "قرش في الأمام! القارب ينتظر ضوء المنارة", "A shark ahead! The boat is waiting for the lighthouse light." },
        { "عضّ القرش القارب! لنرسم القارب حين يكون ضوء المنارة على القرش", "The shark bit the boat! Draw the boat when the lighthouse light is on the shark." },
        { "لنرسم جسرًا لتعبر رويدا", "Draw a bridge so Rowaida can cross" },
        { "الرمل طويل! لنرسم ممشى فوق الماء", "That's a long stretch of sand! Draw a walkway over the water." },
        { "لننتظر الجزر... أو نرسم ممشى", "Let's wait for low tide... or draw a walkway." },
        // الوقت
        { "توقف العقرب!", "The clock hand stopped!" },
        { "العقرب يدور! لنرسم خطًّا تحت مكانه حين يستوي فيوقفه", "The clock hand is turning! Draw a line under it when it's flat to stop it." },
        { "بقيت ساعة رملية... لنقلبها هي أيضًا", "One more hourglass... let's flip it too." },
        { "لنلمس الساعة الرملية بخط لتنقلب وتملأ الحفرة", "Touch the hourglass with a line so it flips and fills the gap." },
        { "نزل الجسر!", "The bridge came down!" },
        { "لنرسم خطًّا يلمس الترسين... فيصبح سيرًا يديرهما", "Draw a line that touches both gears... it becomes a belt that turns them." },
        { "بقي ترس! لنصله بخط أيضًا", "One gear left! Connect it with a line too." },
        { "رررن! استيقظت القطة", "Rrring! The cat woke up." },
        { "عادت القطة إلى النوم! لنوقظها حين تصل رويدا", "The cat went back to sleep! Wake it up when Rowaida gets there." },
        { "القطة نائمة في الطريق! لنرمِ كرة على زر المنبه", "A cat is asleep on the path! Throw a ball at the alarm button." },
        { "الصفحة الزرقاء تظهر في الليل... لنلمس القمر بخط", "The blue page appears at night... touch the moon with a line." },
        { "الصفحة الصفراء تظهر في النهار... لنلمس الشمس بخط", "The yellow page appears during the day... touch the sun with a line." },
    };
}
