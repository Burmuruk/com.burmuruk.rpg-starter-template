using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Burmuruk.WorldG.Patrol
{
    public enum CyclicType
    {
        None,
        Circle,
        Backwards
    }

    public class PatrolPath<T> : ICollection<T>, IDisposable where T : ISplineNode
    {
        #region Variables
        [SerializeField] LinkedList<T> points = new LinkedList<T>();

        private LinkedListNode<T> lastSearched = null;

        private CyclicType cyclicType = CyclicType.None;

        class PatrolEnumerator : IEnumerator<T>
        {
            LinkedList<T> collection;
            int currentIndex = -1;
            CyclicType cyclicType = CyclicType.None;
            LinkedListNode<T> current;
            bool goingRight = true;

            public PatrolEnumerator(LinkedList<T> list, CyclicType cyclicType)
            {
                this.collection = list;
                this.cyclicType = cyclicType;
                current = list.First;
                this.goingRight = true;
            }

            public object Current
            {
                get { return current.Value; }
            }

            T IEnumerator<T>.Current => current.Value;

            public void Dispose()
            {
                collection = null;
                current = null;
                return;
            }

            public bool MoveNext()
            {
                if (cyclicType == CyclicType.Circle)
                {
                    if (goingRight)
                    {
                        if (currentIndex == -1)
                        {
                            currentIndex++;
                            current = collection.First;
                        }
                        else if (currentIndex >= collection.Count - 1)
                        {
                            currentIndex = 0;
                            current = collection.First;
                        }
                        else
                        {
                            currentIndex++;
                            current = current.Next;
                        }
                    }
                    else if (!goingRight)
                    {
                        if (currentIndex <= 0)
                        {
                            currentIndex = collection.Count - 1;
                            current = collection.Last;
                        }
                        else
                        {
                            currentIndex--;
                            current = current.Previous;
                        }
                    }
                }
                else if (cyclicType == CyclicType.Backwards)
                {
                    if (goingRight)
                    {
                        if (currentIndex == -1)
                        {
                            currentIndex++;
                            current = collection.First;
                        }
                        else if (currentIndex >= collection.Count - 1)
                        {
                            currentIndex--;
                            current = current.Previous;
                            goingRight = false;
                        }
                        else
                        {
                            currentIndex++;
                            current = current.Next;
                        }
                    }
                    else if (!goingRight)
                    {
                        if (currentIndex <= 0)
                        {
                            currentIndex++;
                            current = current.Next;
                            goingRight = true;
                        }
                        else
                        {
                            currentIndex--;
                            current = current.Previous;
                        }
                    }
                }
                else if (cyclicType == CyclicType.None)
                {
                    if (goingRight)
                    {
                        if (currentIndex == -1)
                        {
                            currentIndex++;
                            current = collection.First;
                        }
                        else if (currentIndex >= collection.Count - 1)
                            return false;
                        else
                        {
                            currentIndex++;
                            current = current.Next;
                            return true;
                        }
                    }
                    else if (!goingRight)
                    {
                        if (currentIndex <= 0)
                            return false;
                        else
                        {
                            currentIndex--;
                            current = current.Previous;
                            return true;
                        }
                    }
                }

                return true;
            }

            public void Reset()
            {
                currentIndex = -1;
                current = collection.First;
            }
        }
        #endregion

        #region Properties
        public int Count => points.Count;
        public bool IsReadOnly { get; }
        public T Last { get => points.Last.Value; }
        public T First { get => points.First.Value; }
        public LinkedListNode<T> FirstNode { get => points.First; }
        public LinkedListNode<T> LastNode { get => points.Last; }
        public T LastSearched { get => lastSearched.Value; }
        public static bool operator true(PatrolPath<T> p) => p != null;
        public static bool operator false(PatrolPath<T> p) => p == null;
        #endregion

        public PatrolPath() { }

        public PatrolPath(CyclicType cyclicType = CyclicType.None, params T[] points)
        {
            IsReadOnly = false;
            this.points = new();

            foreach (var point in points)
            {
                Add(point);
            }

            this.cyclicType = cyclicType;
        }

        //public void Initialize(CyclicType cyclicType = CyclicType.None, params T[] points)
        //{
        //    foreach (var point in points)
        //    {
        //        AddVariable(point);
        //    }

        //    this.cyclicType = cyclicType;
        //}

        #region Public methods
        public IEnumerator<T> GetEnumerator()
        {
            //using (var enumerator = new Enumerator(points, cyclicType))
            //{
            //    while (enumerator.MoveNext())
            //    {
            //        yield return (T)enumerator.Current;
            //    }
            //}
            return new PatrolEnumerator(points, cyclicType);
        }

        //{

        //    return new Enumerator<T>(this);
        //foreach (var hi in points)
        //{
        //    yield return hi;
        //}

        //using(var enumerator = GetEnumerator())
        //{
        //    enumerator.MoveNext();
        //    while (true)
        //    {
        //        yield return enumerator.Current;

        //        if (!enumerator.MoveNext())
        //            enumerator.Reset();
        //    }
        //}

        //return ((IEnumerable<T>)points).GetEnumerator();
        //}

        public void Add(T item)
        {
            points.AddLast(item);
        }

        public bool AddAfter(T node, T newNode)
        {
            var point = points.Find(node);

            if (point == null) return false;

            points.AddAfter(point, newNode);

            return true;
        }

        public bool AddBefore(T node, T item)
        {
            var point = points.Find(node);

            if (point == null) return false;

            points.AddBefore(point, item);

            return true;
        }

        public void Clear()
        {
            points.Clear();
        }

        public bool Contains(T item)
        {
            return points.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)points).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (points.Remove(item))
            {
                return true;
            }

            return false;
        }

        public T Next(T item)
        {
            if (item)
                lastSearched = points.Find(item);

            if (lastSearched == null)
                throw new InvalidOperationException("Last serached was null");

            return lastSearched.Next.Value;
        }

        public T Prev(T item)
        {
            if (item)
                lastSearched = points.Find(item);

            return lastSearched.Previous.Value;
        }
        #endregion

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            points.Clear();
            lastSearched = null;
        }

        ~PatrolPath()
        {
            Dispose();
            //Debug.Log("End Patrol Path");
        }
    }
}
