namespace System.Collections.Generic
{
    public class AEList<TValue> : IEnumerable
    {
        private List<TValue> values = new List<TValue>();
        private List<TValue> trash = new List<TValue>();

        public TValue this[int index]
        {
            get
            {
                return values[index];
            }

            set
            {
                values[index] = value;
            }
        }

        public void Add(TValue value)
        {
            values.Add(value);
        }

        public bool Contains(TValue value)
        {
            return values.Contains(value);
        }

        public int Count
        {
            get
            {
                return values.Count;
            }
        }

        public virtual bool RemoveSafe(TValue value)
        {
            if (values.Contains(value))
            {
                if (trash.Contains(value))
                {
                    return false;
                }
                else
                {
                    trash.Add(value);
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            values.Clear();
        }

        public bool Synchronize()
        {
            if (trash.Count == 0) return false;
            values.RemoveAll(trash.Contains);
            trash.Clear();
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Synchronize();
            return values.GetEnumerator();
        }
    }
}
