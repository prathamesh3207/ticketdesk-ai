using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TicketDesk.Api.Data;
using TicketDesk.Api.Domain;

namespace TicketDesk.Api.Services;

/// <summary>
/// Ranks knowledge-base articles for a ticket using TF-IDF style keyword scoring (no external API needed).
/// Can be swapped for embeddings + cosine similarity later behind the same method.
/// </summary>
public class KbSearchService(AppDbContext db)
{
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","and","for","you","your","with","this","that","from","are","was","have","has","can","not","but",
        "how","what","when","why","our","out","all","any","get","got","its","into","would","could","should","please","hello","thanks"
    };

    public static IEnumerable<string> Tokens(string s) =>
        Regex.Matches(s.ToLowerInvariant(), "[a-z0-9]+")
             .Select(m => m.Value)
             .Where(w => w.Length > 2 && !Stop.Contains(w));

    public async Task<List<KbArticle>> SuggestAsync(string text, int take = 3)
    {
        var articles = await db.KbArticles.AsNoTracking().ToListAsync();
        var query = Tokens(text).ToHashSet();
        if (query.Count == 0 || articles.Count == 0) return new();

        var docs = articles
            .Select(a => (Article: a, Tokens: Tokens($"{a.Title} {a.Title} {a.Tags} {a.Content}").ToList()))
            .ToList();

        var df = new Dictionary<string, int>();
        foreach (var d in docs)
            foreach (var t in d.Tokens.Distinct())
                df[t] = df.GetValueOrDefault(t) + 1;

        var n = docs.Count;
        return docs
            .Select(d =>
            {
                var tf = d.Tokens.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                double score = 0;
                foreach (var t in query)
                    if (tf.TryGetValue(t, out var c))
                        score += (1 + Math.Log(c)) * Math.Log(1.0 + (double)n / df[t]);
                return (d.Article, Score: score / Math.Sqrt(d.Tokens.Count + 1));
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(take)
            .Select(x => x.Article)
            .ToList();
    }
}
