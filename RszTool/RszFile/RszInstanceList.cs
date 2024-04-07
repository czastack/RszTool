using System.Collections;

namespace RszTool
{
    public class RszInstanceList : IEnumerable<RszInstance>, IList<RszInstance>
    {
        private readonly List<RszInstance> _list = new();
        private readonly Dictionary<RszInstance, int> _indexMap = new();

        public RszInstance this[int index]
        {
            get => _list[index];
            set => throw new NotSupportedException();
        }

        RszInstance IList<RszInstance>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public void Add(RszInstance item)
        {
            _list.Add(item);
            _indexMap.Add(item, 0);
        }

        public void Clear()
        {
            _list.Clear();
            _indexMap.Clear();
        }

        public int IndexOf(RszInstance item)
        {
            var result = -1;
            if (_indexMap.TryGetValue(item, out var index))
                result = index;
            return result;
        }

        public bool Contains(RszInstance item) => _list.IndexOf(item) != -1;

        public IEnumerator<RszInstance> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(RszInstance[] array, int arrayIndex) => throw new NotImplementedException();
        public void Insert(int index, RszInstance item) => throw new NotImplementedException();
        public bool Remove(RszInstance item) => throw new NotImplementedException();
        public void RemoveAt(int index) => throw new NotImplementedException();
    }
}
