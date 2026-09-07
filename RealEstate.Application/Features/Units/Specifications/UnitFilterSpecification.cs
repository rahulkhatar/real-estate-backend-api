using System.Text.RegularExpressions;
using RealEstate.Application.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Specifications;

namespace RealEstate.Application.Features.Units.Specifications;

public class UnitFilterSpecification : BaseSpecification<Unit>
{
    public UnitFilterSpecification(UnitQueryParams query)
        : base(u => !u.IsDeleted)
    {
        if (!string.IsNullOrWhiteSpace(query.PropertyId))
            AddCriteria(u => u.PropertyId == query.PropertyId);

        if (!string.IsNullOrWhiteSpace(query.ProjectId))
            AddCriteria(u => u.ProjectId == query.ProjectId);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<UnitStatus>(query.Status, true, out var status))
            AddCriteria(u => u.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Type) &&
            Enum.TryParse<UnitType>(query.Type, true, out var type))
            AddCriteria(u => u.Type == type);

        if (query.MinPrice.HasValue)
            AddCriteria(u => u.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            AddCriteria(u => u.Price <= query.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            AddCriteria(BuildSearchCriteria(query.Search.Trim()));

        ApplyOrderByDescending(u => u.CreatedAt);
        ApplyPaging(query.PageNumber, query.PageSize);
    }

    // A single search box has to cover three unrelated shapes of input -- a price ("1000000"),
    // a BHK term ("2bhk", "two bhk", "studio"), or free text (a city, project, or property
    // name) -- so classify the term first and query the matching field(s) instead of one blanket
    // substring match, which wouldn't make sense against a decimal price or an enum-backed type.
    private static System.Linq.Expressions.Expression<Func<Unit, bool>> BuildSearchCriteria(string term)
    {
        if (decimal.TryParse(term, out var price))
        {
            // Literal digit-substring matching on a price is what SQL LIKE '%term%' would do,
            // but it's not useful ("100" would match 51000000) -- treat the number as a target
            // price instead and return listings within ~15% of it.
            var lower = price * 0.85m;
            var upper = price * 1.15m;
            return u => u.Price >= lower && u.Price <= upper;
        }

        if (TryParseBhkType(term, out var bhkType))
            return u => u.Type == bhkType;

        var lower2 = term.ToLower();
        return u =>
            u.UnitNumber.ToLower().Contains(lower2) ||
            u.ProjectSnapshot.Name.ToLower().Contains(lower2) ||
            u.ProjectSnapshot.City.ToLower().Contains(lower2) ||
            u.PropertySnapshot.Name.ToLower().Contains(lower2);
    }

    private static readonly Dictionary<string, UnitType> NamedTypeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["studio"] = UnitType.Studio,
        ["penthouse"] = UnitType.Penthouse,
        ["villa"] = UnitType.Villa,
        ["office"] = UnitType.Office,
        ["shop"] = UnitType.Shop,
    };

    private static readonly string[] BhkNumberWords = ["zero", "one", "two", "three", "four"];

    // Matches "2bhk", "2 bhk", "2-bhk", "two bhk", etc. The enum only goes up to FourBhk, so
    // anything outside 1-4 falls through to the free-text branch instead of guessing.
    private static readonly Regex BhkPattern = new(@"^(\d+|one|two|three|four)\s*-?\s*bhk$", RegexOptions.Compiled);

    private static bool TryParseBhkType(string term, out UnitType type)
    {
        var normalized = term.Trim().ToLowerInvariant();

        if (NamedTypeWords.TryGetValue(normalized, out type))
            return true;

        var match = BhkPattern.Match(normalized);
        if (match.Success)
        {
            var token = match.Groups[1].Value;
            var digit = int.TryParse(token, out var n) ? n : Array.IndexOf(BhkNumberWords, token);
            switch (digit)
            {
                case 1: type = UnitType.OneBhk; return true;
                case 2: type = UnitType.TwoBhk; return true;
                case 3: type = UnitType.ThreeBhk; return true;
                case 4: type = UnitType.FourBhk; return true;
            }
        }

        type = default;
        return false;
    }
}
