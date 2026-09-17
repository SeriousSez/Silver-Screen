using System;

namespace SilverScreen.Domain.Time
{
    [Serializable]
    public struct SimulationDateTime : IEquatable<SimulationDateTime>, IComparable<SimulationDateTime>
    {
        public int Year { get; private set; }
        public int Month { get; private set; }
        public int Day { get; private set; }
        public int Hour { get; private set; }
        public int Minute { get; private set; }

        private static readonly string[] MonthShortNames = new[]
        {
            "", "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
            "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
        };

        private static readonly int[] DaysInMonthsNonLeap = new[]
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        public SimulationDateTime(int year, int month, int day, int hour, int minute)
        {
            Year = Math.Max(1, year);
            Month = Math.Clamp(month, 1, 12);
            int maxDays = GetDaysInMonth(Year, Month);
            Day = Math.Clamp(day, 1, maxDays);
            Hour = Math.Clamp(hour, 0, 23);
            Minute = Math.Clamp(minute, 0, 59);
        }

        public static bool IsLeapYear(int year)
        {
            return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
        }

        public static int GetDaysInMonth(int year, int month)
        {
            if (month < 1 || month > 12) return 30;
            if (month == 2 && IsLeapYear(year)) return 29;
            return DaysInMonthsNonLeap[month];
        }

        public void AdvanceMinute(out bool hourRolled, out bool dayRolled, out bool monthRolled, out bool yearRolled)
        {
            hourRolled = false;
            dayRolled = false;
            monthRolled = false;
            yearRolled = false;

            Minute++;
            if (Minute >= 60)
            {
                Minute = 0;
                Hour++;
                hourRolled = true;

                if (Hour >= 24)
                {
                    Hour = 0;
                    Day++;
                    dayRolled = true;

                    int maxDays = GetDaysInMonth(Year, Month);
                    if (Day > maxDays)
                    {
                        Day = 1;
                        Month++;
                        monthRolled = true;

                        if (Month > 12)
                        {
                            Month = 1;
                            Year++;
                            yearRolled = true;
                        }
                    }
                }
            }
        }

        public string ToFormattedString()
        {
            string monthStr = (Month >= 1 && Month <= 12) ? MonthShortNames[Month] : "JAN";
            return $"{monthStr} {Day}, {Year} — {Hour:D2}:{Minute:D2}";
        }

        public override string ToString() => ToFormattedString();

        public bool Equals(SimulationDateTime other)
        {
            return Year == other.Year && Month == other.Month && Day == other.Day &&
                   Hour == other.Hour && Minute == other.Minute;
        }

        public override bool Equals(object obj)
        {
            return obj is SimulationDateTime other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Year, Month, Day, Hour, Minute);
        }

        public int CompareTo(SimulationDateTime other)
        {
            if (Year != other.Year) return Year.CompareTo(other.Year);
            if (Month != other.Month) return Month.CompareTo(other.Month);
            if (Day != other.Day) return Day.CompareTo(other.Day);
            if (Hour != other.Hour) return Hour.CompareTo(other.Hour);
            return Minute.CompareTo(other.Minute);
        }

        public static bool operator ==(SimulationDateTime left, SimulationDateTime right) => left.Equals(right);
        public static bool operator !=(SimulationDateTime left, SimulationDateTime right) => !left.Equals(right);
    }
}
