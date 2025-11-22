using System;
using System.Collections.Generic;
using System.Globalization;
using SubscriptionSystem.Application.DTOs;

namespace SubscriptionSystem.Infrastructure.Services
{
    /// <summary>
    /// Parses admin WhatsApp messages into prediction DTOs.
    /// Simple, rule-based parser that expects a small template.
    /// </summary>
    public class WhatsAppAdminCommandParser
    {
        public ParseResult Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return ParseResult.Error("Empty message");

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return ParseResult.Error("Empty message");

            var first = lines[0].Trim().ToLowerInvariant();

            if (first.StartsWith("/detailed") || first.StartsWith("detailed"))
            {
                return ParseDetailed(lines);
            }

            if (first.StartsWith("/simple") || first.StartsWith("simple"))
            {
                return ParseSimple(lines);
            }

            return ParseResult.Error("Unrecognized command. Use '/detailed' or '/simple' as the first line.");
        }

        private ParseResult ParseDetailed(string[] lines)
        {
            var dict = ExtractKeyValues(lines, 1);
            var errors = new List<string>();

            if (!dict.TryGetValue("tournament", out var tournament) || string.IsNullOrWhiteSpace(tournament))
                errors.Add("Missing 'Tournament'");
            if (!dict.TryGetValue("team1", out var team1) || string.IsNullOrWhiteSpace(team1))
                errors.Add("Missing 'Team1'");
            if (!dict.TryGetValue("team2", out var team2) || string.IsNullOrWhiteSpace(team2))
                errors.Add("Missing 'Team2'");
            if (!dict.TryGetValue("matchdate", out var matchDateStr) || string.IsNullOrWhiteSpace(matchDateStr))
                errors.Add("Missing 'MatchDate' (ISO or 'dd/MM/yyyy HH:mm')");
            if (!dict.TryGetValue("matchdetails", out var matchDetails))
                matchDetails = string.Empty;
            if (!dict.TryGetValue("confidencelevel", out var confidenceStr))
                confidenceStr = "0";
            if (!dict.TryGetValue("predictedoutcome", out var predictedOutcome))
                predictedOutcome = string.Empty;

            if (errors.Count > 0)
                return ParseResult.Error(errors.ToArray());

            if (!TryParseDate(matchDateStr, out var matchDate))
                return ParseResult.Error("Unable to parse MatchDate. Use ISO or 'dd/MM/yyyy HH:mm'");

            if (!int.TryParse(confidenceStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var confidence))
                confidence = 0;
            confidence = Math.Clamp(confidence, 0, 100);

            var dto = new DetailedPredictionDto
            {
                Tournament = tournament.Trim(),
                Team1 = team1.Trim(),
                Team2 = team2.Trim(),
                MatchDate = matchDate,
                MatchDetails = matchDetails?.Trim() ?? string.Empty,
                NonAlphanumericDetails = string.Empty,
                Team1Performance = new TeamPerformanceDto(),
                Team2Performance = new TeamPerformanceDto(),
                ConfidenceLevel = confidence,
                PredictedOutcome = predictedOutcome?.Trim() ?? string.Empty
            };

            return ParseResult.SuccessDetailed(dto);
        }

        private ParseResult ParseSimple(string[] lines)
        {
            var dict = ExtractKeyValues(lines, 1);
            var errors = new List<string>();

            if (!dict.TryGetValue("team1", out var team1) || string.IsNullOrWhiteSpace(team1))
                errors.Add("Missing 'Team1'");
            if (!dict.TryGetValue("team2", out var team2) || string.IsNullOrWhiteSpace(team2))
                errors.Add("Missing 'Team2'");
            if (!dict.TryGetValue("predictedoutcome", out var predictedOutcome))
                predictedOutcome = string.Empty;
            if (!dict.TryGetValue("confidencelevel", out var confidenceStr))
                confidenceStr = "0";

            if (errors.Count > 0)
                return ParseResult.Error(errors.ToArray());

            if (!int.TryParse(confidenceStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var confidence))
                confidence = 0;
            confidence = Math.Clamp(confidence, 0, 100);

            var simple = new SimplePredictionDto
            {
                AlphanumericPrediction = $"{team1.Trim()} vs {team2.Trim()}",
                NonAlphanumericDetails = string.Empty,
                IsPromotional = false
            };

            return ParseResult.SuccessSimple(simple, predictedOutcome?.Trim() ?? string.Empty, confidence);
        }

        private static bool TryParseDate(string input, out DateTime dt)
        {
            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dt))
                return true;

            if (DateTime.TryParseExact(input, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                return true;

            return false;
        }

        private static Dictionary<string, string> ExtractKeyValues(string[] lines, int startIndex)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];
                var idx = line.IndexOf(':');
                if (idx <= 0)
                    continue;
                var key = line.Substring(0, idx).Trim();
                var val = line.Substring(idx + 1).Trim();
                if (!dict.ContainsKey(key)) dict[key] = val;
            }
            return dict;
        }

        public record ParseResult(bool IsValid, string[] Errors, DetailedPredictionDto? Detailed, SimplePredictionDto? Simple, string PredictedOutcome, int Confidence)
        {
            public static ParseResult Error(params string[] errors) => new ParseResult(false, errors, null, null, string.Empty, 0);
            public static ParseResult SuccessDetailed(DetailedPredictionDto dto) => new ParseResult(true, Array.Empty<string>(), dto, null, string.Empty, 0);
            public static ParseResult SuccessSimple(SimplePredictionDto dto, string predictedOutcome, int confidence) => new ParseResult(true, Array.Empty<string>(), null, dto, predictedOutcome, confidence);
        }
    }
}
