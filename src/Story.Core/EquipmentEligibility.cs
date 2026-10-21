using System;

namespace Story.Core;

public static class EquipmentEligibility
{
    public static bool IsAvailable(DateTime date, int productionYear, int productionMonth, int endYear,
        bool allowed, bool partisan, bool prototype, bool prototypesDisabled, bool commander, bool campaign,
        bool techTree, bool researched)
    {
        if (!allowed || partisan || (prototype && prototypesDisabled) || (commander && campaign) || date.Year > endYear) return false;
        if (techTree && !campaign) return researched;
        productionMonth = productionMonth == 0 ? 1 : productionMonth;
        DateTime first;
        try { first = new DateTime(productionYear, productionMonth, 1); }
        catch (ArgumentOutOfRangeException) { first = new DateTime(date.Year, 1, 1); }
        return first <= date;
    }
}
