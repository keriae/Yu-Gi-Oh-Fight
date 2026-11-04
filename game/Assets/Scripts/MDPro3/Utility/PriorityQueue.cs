using System;
using System.Collections.Generic;

namespace MDPro3.Utility
{
    /// <summary>
    /// 自定义优先级队列实现
    /// </summary>
    /// <typeparam name="T">队列元素类型</typeparam>
    public class PriorityQueue<T>
    {
        private readonly List<T> data;
        private readonly IComparer<T> comparer;

        public PriorityQueue()
        {
            this.data = new List<T>();
            this.comparer = Comparer<T>.Default;
        }

        public PriorityQueue(IComparer<T> comparer)
        {
            this.data = new List<T>();
            this.comparer = comparer ?? Comparer<T>.Default;
        }

        public PriorityQueue(Comparison<T> comparison)
        {
            this.data = new List<T>();
            this.comparer = Comparer<T>.Create(comparison);
        }

        public void Enqueue(T item)
        {
            data.Add(item);
            int childIndex = data.Count - 1;
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (comparer.Compare(data[childIndex], data[parentIndex]) >= 0)
                    break;

                T tmp = data[childIndex];
                data[childIndex] = data[parentIndex];
                data[parentIndex] = tmp;
                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            int lastIndex = data.Count - 1;
            T frontItem = data[0];
            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);

            lastIndex--;
            int parentIndex = 0;
            while (true)
            {
                int leftChildIndex = parentIndex * 2 + 1;
                if (leftChildIndex > lastIndex)
                    break;

                int rightChildIndex = leftChildIndex + 1;
                if (rightChildIndex <= lastIndex && comparer.Compare(data[rightChildIndex], data[leftChildIndex]) < 0)
                    leftChildIndex = rightChildIndex;

                if (comparer.Compare(data[parentIndex], data[leftChildIndex]) <= 0)
                    break;

                T tmp = data[parentIndex];
                data[parentIndex] = data[leftChildIndex];
                data[leftChildIndex] = tmp;
                parentIndex = leftChildIndex;
            }

            return frontItem;
        }

        public T Peek()
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Queue is empty");
            return data[0];
        }

        public int Count
        {
            get { return data.Count; }
        }

        public bool TryDequeue(out T result)
        {
            if (data.Count == 0)
            {
                result = default(T);
                return false;
            }

            result = Dequeue();
            return true;
        }

        public bool TryPeek(out T result)
        {
            if (data.Count == 0)
            {
                result = default(T);
                return false;
            }

            result = Peek();
            return true;
        }

        public void Clear()
        {
            data.Clear();
        }

        public bool Contains(T item)
        {
            return data.Contains(item);
        }
    }
}