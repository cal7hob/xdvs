using System;
using System.Collections.Generic;

public abstract class SafeLinq
{
	public static T Max<T>(IEnumerable<T> collection)
        where T: IComparable
	{
		T maxVal = default(T);

		bool inited = false;

		foreach (T element in collection)
		{
			if (!inited) 
			{
				maxVal = element;

				inited = true;

				continue;
			}

			if (element.CompareTo(maxVal) > 0)
				maxVal = element;
		}
			
		return maxVal;
	}

	public static T Min<T>(IEnumerable<T> collection)
        where T : IComparable
	{
		T minVal = default(T);

		bool inited = false;

		foreach (T element in collection)
		{
			if (!inited)
			{
				minVal = element;

				inited = true;

				continue;
			}

			if (element.CompareTo(minVal) < 0)
				minVal = element;
		}

		return minVal;
	}

    public static KeyValuePair<T1, T2> Max<T1, T2>(Dictionary<T1, T2> collection)
        where T1 : IComparable
    {
        KeyValuePair<T1, T2> maxVal = default(KeyValuePair<T1, T2>);

        bool inited = false;

        foreach (KeyValuePair<T1, T2> element in collection)
        {
            if (!inited)
            {
                maxVal = element;

                inited = true;

                continue;
            }

            if (element.Key.CompareTo(maxVal.Key) > 0)
                maxVal = element;
        }

        return maxVal;
    }

    public static KeyValuePair<T1, T2> Min<T1, T2>(Dictionary<T1, T2> collection)
        where T1 : IComparable
    {
        KeyValuePair<T1, T2> minVal = default(KeyValuePair<T1, T2>);

        bool inited = false;

        foreach (KeyValuePair<T1, T2> element in collection)
        {
            if (!inited)
            {
                minVal = element;

                inited = true;

                continue;
            }

            if (element.Key.CompareTo(minVal.Key) < 0)
                minVal = element;
        }

        return minVal;
    }

    public static int Count<T>(IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null)
            throw new ArgumentNullException("source");

        if (predicate == null)
            throw new ArgumentNullException("predicate");

        int count = 0;

        foreach (T element in source)
        {
            checked
            {
                if (predicate(element))
                    count++;
            }
        }

        return count;
    }
}
