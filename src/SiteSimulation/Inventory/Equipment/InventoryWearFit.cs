namespace SiteSimulation;

/// <summary>Relative fit units, not worn depth, weight, storage size or the same-band strap allowance.</summary>
public sealed record WearFitProfile(string Family, int Bulk, int MaxUnderBulk, string[] FitsOver, string[]? ExcludesUnderTags = null);
public sealed record WearFitResult(bool Fits, string Reason, int UnderBulk, int Capacity);

/// <summary>Checks one outer garment against the equipment beneath it at one overlapping body region.</summary>
public static class InventoryWearFit
{
    private static bool Token(string? value) => value is { Length: > 0 and <= 64 }
        && value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is ':' or '-' or '_' or '.');
    private static bool Tokens(string[]? values) => values is { Length: <= 32 }
        && values.All(Token) && values.Distinct(StringComparer.Ordinal).Count() == values.Length;
    internal static bool ProfileValid(WearFitProfile? fit) => fit is not null && Token(fit.Family)
        && fit.Bulk is >= 1 and <= 1000 && fit.MaxUnderBulk is >= 0 and <= 1000
        && Tokens(fit.FitsOver) && (fit.ExcludesUnderTags is null || Tokens(fit.ExcludesUnderTags));

    /// <summary>Pure compatibility query. The caller supplies the chosen order; authored Layer never decides fit.</summary>
    public static WearFitResult Evaluate(ItemDefinition outer, IReadOnlyList<ItemDefinition> beneath)
    {
        if (outer?.Wear?.Fit is not { } fit || !ProfileValid(fit))
            return new(false, "Outer equipment has no valid authored fit profile.", 0, 0);
        if (beneath is null || beneath.Count > 128 || beneath.Any(d => d?.Wear is null || d.Tags is null || !ProfileValid(d.Wear.Fit)))
            return new(false, "Underlying equipment has no valid authored fit profile, or the fit query exceeds its safety bound.", 0, fit.MaxUnderBulk);
        // Repeated definitions represent distinct physical garments and each contributes bulk.
        var bulk = beneath.Sum(d => d.Wear!.Fit!.Bulk);
        foreach (var inner in beneath.OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            if (inner.Wear!.Outermost)
                return new(false, "Nothing can cover outermost " + inner.Name + "; remove it first.", bulk, fit.MaxUnderBulk);
            if (!fit.FitsOver.Contains(inner.Wear.Fit!.Family, StringComparer.Ordinal))
                return new(false, outer.Name + " cannot fit over " + inner.Name + " (" + inner.Wear.Fit.Family + ").", bulk, fit.MaxUnderBulk);
            if (fit.ExcludesUnderTags is { } excluded && inner.Tags.Any(excluded.Contains))
                return new(false, outer.Name + " excludes the fit traits of " + inner.Name + ".", bulk, fit.MaxUnderBulk);
        }
        return bulk > fit.MaxUnderBulk
            ? new(false, outer.Name + $" has insufficient room: underlying bulk {bulk} exceeds clearance {fit.MaxUnderBulk}.", bulk, fit.MaxUnderBulk)
            : new(true, "Fits within the authored clearance.", bulk, fit.MaxUnderBulk);
    }

    /// <summary>Temporary bridge from the existing layer order; the evaluator is reusable by the future worn-stack projection.</summary>
    internal static void ValidateWorn(InventoryState state, IReadOnlyList<InventoryItem> worn)
    {
        foreach (var outer in worn)
        {
            var definition = state.Definition(outer.Id);
            foreach (var region in InventoryWear.Mount(state, outer, outer.Placement)!.Coverage)
            {
                var beneath = worn.Where(i => i.Id != outer.Id && state.Definition(i.Id).Wear!.Layer < definition.Wear!.Layer
                    && InventoryWear.Mount(state, i, i.Placement)!.Coverage.Contains(region)).Select(i => state.Definition(i.Id)).ToArray();
                var result = Evaluate(definition, beneath);
                if (!result.Fits) throw new ArgumentException(region + ": " + result.Reason);
            }
        }
    }
}
