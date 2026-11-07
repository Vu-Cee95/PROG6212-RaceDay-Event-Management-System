namespace RaceDay.Mvc.Helpers;

public static class PositionText
{
    public static string Format(int? position, int total)
    {
        if (position is null or < 1)
        {
            return "No result yet";
        }

        return $"{ToOrdinal(position.Value)} of {total}";
    }

    public static string ToOrdinal(int number)
    {
        var abs = Math.Abs(number);
        var lastTwo = abs % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return $"{number}th";
        }

        return (abs % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }
}
