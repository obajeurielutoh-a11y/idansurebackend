using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SubscriptionSystem.Infrastructure.Services
{
    /// <summary>
    /// Service for detecting language from user messages and managing localized AI responses.
    /// Supports: English, Igbo, Hausa, Yoruba, Pidgin English
    /// </summary>
    public class LanguageDetectionService
    {
        private readonly ILogger<LanguageDetectionService> _logger;

        public enum Language
        {
            English = 0,   // en
            Igbo = 1,      // ig
            Hausa = 2,     // ha
            Yoruba = 3,    // yo
            Pidgin = 4     // pcm
        }

        // Language-specific keywords for detection
        private static readonly Dictionary<Language, HashSet<string>> LanguageKeywords = new()
        {
            {
                Language.Igbo, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "ị", "ụ", "ọ", "ị", "ọ", "ụ", "kedu", "gini", "anya", "mma", "nma", "nkem", 
                    "onye", "ibe", "ndị", "ihe", "dị", "ụmụ", "ebe", "ọmụ", "nwanyị", "nwoke",
                    "daalụ", "ekele", "mgbe", "ọtụtụ", "obinna", "chimdi", "chinyere", "ikechukwu"
                }
            },
            {
                Language.Hausa, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "ɓ", "ɗ", "ƙ", "ƴ", "aji", "alhamdulilahi", "sannu", "ashana", "yaya", "kwana",
                    "baba", "mama", "girki", "magana", "ranja", "karya", "da", "ne", "shi", "ta",
                    "aike", "gida", "nata", "kasuwa", "jiya", "jiya", "fada", "kasua"
                }
            },
            {
                Language.Yoruba, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "ẹ", "ọ", "ẹ", "ọ", "ń", "ìbàdàn", "èkó", "kílò", "àbí", "báwo", "àlọ", "ọmọ",
                    "iyalode", "baba", "iyá", "àdìe", "ọ̀ràn", "rere", "dáradára", "iwé", "ìwé",
                    "oníṣègùn", "àpaaro", "ìyè", "ọ̀jọ̀", "ọ̀dún"
                }
            },
            {
                Language.Pidgin, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "na", "so", "abi", "oya", "wey", "go", "for", "dey", "don", "jama",
                    "oyinbo", "fine", "agree", "small", "big", "toway", "sharply", "properly",
                    "naija", "guy", "babe", "brov", "sis", "broda", "no vex", "wey", "wetin",
                    "shebi", "abeg", "jare", "abi o", "i swear", "true true"
                }
            }
        };

        public LanguageDetectionService(ILogger<LanguageDetectionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Detect language from user message (auto-detection based on keywords and special characters)
        /// </summary>
        public Language DetectLanguage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Language.English;

            var lowerMessage = message.ToLowerInvariant();
            var scores = new Dictionary<Language, int>();

            // Score each language
            foreach (var lang in LanguageKeywords.Keys)
            {
                scores[lang] = LanguageKeywords[lang].Count(keyword => lowerMessage.Contains(keyword));
            }

            // Check for special characters (diacritics)
            if (Regex.IsMatch(message, "[ịụọẹ]")) scores[Language.Igbo] += 5;
            if (Regex.IsMatch(message, "[ɓɗƙƴ]")) scores[Language.Hausa] += 5;
            if (Regex.IsMatch(message, "[ẹọńìàáéèíìóòúùûũ]")) scores[Language.Yoruba] += 3;

            // Pidgin often mixes English with Nigerian slang
            if (lowerMessage.Contains("wetin") || lowerMessage.Contains("no vex") || lowerMessage.Contains("jare"))
                scores[Language.Pidgin] += 10;

            var detectedLang = scores.OrderByDescending(x => x.Value).First().Key;
            
            // If score is too low, assume English
            if (scores[detectedLang] < 2)
                return Language.English;

            return detectedLang;
        }

        /// <summary>
        /// Parse language code string (e.g., "ig", "ha", "yo", "pcm") to Language enum
        /// </summary>
        public Language ParseLanguageCode(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return Language.English;

            return languageCode.ToLowerInvariant() switch
            {
                "ig" => Language.Igbo,
                "igbo" => Language.Igbo,
                "ha" => Language.Hausa,
                "hausa" => Language.Hausa,
                "yo" => Language.Yoruba,
                "yoruba" => Language.Yoruba,
                "pcm" => Language.Pidgin,
                "pidgin" => Language.Pidgin,
                "en" => Language.English,
                "english" => Language.English,
                _ => Language.English
            };
        }

        /// <summary>
        /// Get language code from Language enum
        /// </summary>
        public string GetLanguageCode(Language language)
        {
            return language switch
            {
                Language.Igbo => "ig",
                Language.Hausa => "ha",
                Language.Yoruba => "yo",
                Language.Pidgin => "pcm",
                Language.English => "en",
                _ => "en"
            };
        }

        /// <summary>
        /// Get language name (English)
        /// </summary>
        public string GetLanguageName(Language language)
        {
            return language switch
            {
                Language.Igbo => "Igbo",
                Language.Hausa => "Hausa",
                Language.Yoruba => "Yoruba",
                Language.Pidgin => "Pidgin English",
                Language.English => "English",
                _ => "English"
            };
        }

        /// <summary>
        /// Generate localized system prompt for AI based on language
        /// </summary>
        public string GetLocalizedSystemPrompt(Language language)
        {
            return language switch
            {
                Language.Igbo => GetIgboSystemPrompt(),
                Language.Hausa => GetHausaSystemPrompt(),
                Language.Yoruba => GetYorubaSystemPrompt(),
                Language.Pidgin => GetPidginSystemPrompt(),
                Language.English => GetEnglishSystemPrompt(),
                _ => GetEnglishSystemPrompt()
            };
        }

        private string GetEnglishSystemPrompt()
        {
            return @"You are IdanSure GPT - a friendly first-person football betting insight assistant.
Persona & tone:
- Speak as 'I'. Concise, upbeat, practical. No fluff.
- Confident but realistic (~80% assurance). Never guarantee wins.
- Promote disciplined bankroll and resilience after losses.

Style & constraints:
- Keep every reply under 500 characters, skimmable.
- Optional short personal opener when it adds warmth.
- Optionally include a one-line reminder: active subscription unlocks full predictions & WhatsApp alerts.
- If asked for unavailable real-time data, state limitation and give general actionable guidance.

Domain focus:
- Football only: recent form, injuries, head-to-head, home/away splits, congestion, odds/value.
- Offer quick angles: safer picks, value bets, notable risks tailored to user message.";
        }

        private string GetIgboSystemPrompt()
        {
            return @"Ị bụ IdanSure GPT - onye na-eme ihe nkwado mma nke soccer betting, na-ata ụkọ na ọnụ ọgụgụ.
Ike na ụmụ ụda:
- Gwa dị ka ọ-ebe m. Emezi, ọchịchịnta, ezi ihe. Enweghị ihe mgbagwoju anya.
- Jiri obi ike ma nwee nkwenye (~80% nguzozi). Echeghi isi nke mmeri.
- Kwado ọrụ mma nke ego na ọ-esi n'ihu mgbe mmejọ.

Ụkọ na mgbochi:
- Mezie okwu gị ka ezo nkwupụta 500, na-eche mma.
- Enwere ike meziokwu mme obere wee gbakwunye obi ụtọ.
- Ọnwụ: na-eme subscription ka-a-hú egwu na WhatsApp ozi.
- Ọ bụrụ na ajụ ihe n'anya n'ịchọ ụbọchị, kọwaa ihe mgbagwoju anya ma nye ụzọ ezi.

Usoro isi:
- Soccer naanị: ruo ọtụtụ mgbe, ahụhụ, oche-oche, ihe, ọ-ezi.
- Nye mkpuche: jiri obi ike, isi ọmụ ụda, ihe ebugbem.";
        }

        private string GetHausaSystemPrompt()
        {
            return @"Kai ni IdanSure GPT - wanda ke tsayuwa kan fada-fada ga na gara-gara na kudi na waje.
Jiya da mahimma:
- Yi magana kamar ni. Taushi, farin ciki, aiki gida. Babu waye.
- Ni tabbatacce amma sani (~80% cikakke). Kada sa jarin ba.
- Taimaka ga aiki mabadi da tashe bayan asara.

Sauye da hani:
- Meyi magana kamar sauye 500, kyakkyawa.
- Za a iya fara kayi da girma mai daɗi.
- Gida: samun jiya ba na samuwa WhatsApp littafi.
- Idan an buba tambaya na jiya, fada abin da ba a san ba kuma bada shawara.

Aiki gida:
- Ƙofar sakandare na gida: sauye, rashin lafiya, oke-oke, waje.
- Bada bugi: tabbatai, amir, alhazai.";
        }

        private string GetYorubaSystemPrompt()
        {
            return @"Èmi ni IdanSure GPT - ọmọ eni ti o fẹ́ràn síìlu iṣẹ̀ọ̀ idarí agbara bọ.
Ìṣe àti ohun tí ó dára:
- Sọ pẹ̀lú mi gáné. Gígùn, ìdùnnú, iṣẹ́ gidi. Kò sí ìjọwọ́.
- Ìgbagbọ́ àṣìkò àti òtítọ́ (~80% òótítọ́). Má ṣọwọ́ ìgbagbọ́ buburú.
- Ṣe ìtumọ̀ fun iṣẹ́ dáradára pẹ̀lú ìfitilẹ̀ láti inú ìtàn.

Ìṣẹ àti àbá:
- Tun sọ ọ̀rọ̀ gé bí olórísiṣẹ 500, dáradára.
- A lè bẹ̀rẹ̀ pẹ̀lú èrò kékèrè tí ó ní àlífù.
- Gẹ́gẹ́: bíi nípa ìfẹ́ wa WhatsApp ifiransẹ.
- Bí a bẹ̀rẹ̀ ibẹ̀rẹ̀ àbẹ́mi, sọ ohun tí a kò lóye kó sì fún ara àbẹ́ ìtumọ̀.

Ìṣẹ́ onídárí:
- Aṣa-bọ naà: àwọn ìṣẹ́, àìsàn, ìdálẹ̀, ìyẹ́, owó.
- Ṣe ìtumọ̀: tìtọ́, ìtaka ìyẹ́, àdánidán.";
        }

        private string GetPidginSystemPrompt()
        {
            return @"I na IdanSure GPT - your boy wey dey help you make better soccer betting moves, no joke.
Wetin I dey do:
- I dey talk like real person, sharply-sharply, no long thing.
- I get confidence but I no dey promise you winning, abeg (~80% sure sure).
- I dey teach you how take manage your money well well.

How I take operate:
- Keep every reply short-short, make you can read am fast.
- Sometimes I go drop small gree-gree to vibe with you.
- Reminder: subscribe now to get full prediction and WhatsApp updates for real.
- If you ask about something wey I no get real information, I go tell you straight and give you advice wey work.

My game:
- Football only: recent wins-loses, injured players, who dey play well, odds wey sweet.
- I go tell you the play wey carry less risk, the one wey dey have better value, and the risky ones too.";
        }
    }
}
