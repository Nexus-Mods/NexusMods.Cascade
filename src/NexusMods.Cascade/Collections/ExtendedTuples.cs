using System;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Collections;

public readonly struct ExtendedTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITuple
{
    public ExtendedTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
    }

    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;
    public readonly T4 Item4;
    public readonly T5 Item5;
    public readonly T6 Item6;
    public readonly T7 Item7;
    public readonly T8 Item8;
    public readonly T9 Item9;

    public object? this[int index]
    {
        get
        {
            return index switch
            {
                0 => Item1,
                1 => Item2,
                2 => Item3,
                3 => Item4,
                4 => Item5,
                5 => Item6,
                6 => Item7,
                7 => Item8,
                8 => Item9,
                _ => throw new IndexOutOfRangeException()
            };
        }
    }

    public int Length => 9;
}

public readonly struct ExtendedTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITuple
{
    public ExtendedTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
    }

    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;
    public readonly T4 Item4;
    public readonly T5 Item5;
    public readonly T6 Item6;
    public readonly T7 Item7;
    public readonly T8 Item8;
    public readonly T9 Item9;
    public readonly T10 Item10;

    public object? this[int index]
    {
        get
        {
            return index switch
            {
                0 => Item1,
                1 => Item2,
                2 => Item3,
                3 => Item4,
                4 => Item5,
                5 => Item6,
                6 => Item7,
                7 => Item8,
                8 => Item9,
                9 => Item10,
                _ => throw new IndexOutOfRangeException()
            };
        }
    }

    public int Length => 10;
}

public readonly struct ExtendedTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : ITuple
{
    public ExtendedTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
        Item11 = item11;
    }

    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;
    public readonly T4 Item4;
    public readonly T5 Item5;
    public readonly T6 Item6;
    public readonly T7 Item7;
    public readonly T8 Item8;
    public readonly T9 Item9;
    public readonly T10 Item10;
    public readonly T11 Item11;

    public object? this[int index]
    {
        get
        {
            return index switch
            {
                0 => Item1,
                1 => Item2,
                2 => Item3,
                3 => Item4,
                4 => Item5,
                5 => Item6,
                6 => Item7,
                7 => Item8,
                8 => Item9,
                9 => Item10,
                10 => Item11,
                _ => throw new IndexOutOfRangeException()
            };
        }

    }

    public int Length => 11;
}

public readonly struct ExtendedTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : ITuple
{
    public ExtendedTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
        Item6 = item6;
        Item7 = item7;
        Item8 = item8;
        Item9 = item9;
        Item10 = item10;
        Item11 = item11;
        Item12 = item12;
    }

    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;
    public readonly T4 Item4;
    public readonly T5 Item5;
    public readonly T6 Item6;
    public readonly T7 Item7;
    public readonly T8 Item8;
    public readonly T9 Item9;
    public readonly T10 Item10;
    public readonly T11 Item11;
    public readonly T12 Item12;

    public object? this[int index]
    {
        get
        {
            return index switch
            {
                0 => Item1,
                1 => Item2,
                2 => Item3,
                3 => Item4,
                4 => Item5,
                5 => Item6,
                6 => Item7,
                7 => Item8,
                8 => Item9,
                9 => Item10,
                10 => Item11,
                11 => Item12,
                _ => throw new IndexOutOfRangeException()
            };
        }
    }

    public int Length => 12;
}
